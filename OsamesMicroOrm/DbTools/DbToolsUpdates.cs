/*
This file is part of OSAMES Micro ORM.
Copyright 2014 OSAMES

OSAMES Micro ORM is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

OSAMES Micro ORM is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with OSAMES Micro ORM.  If not, see <http://www.gnu.org/licenses/>.
*/

using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Text;
using OsamesMicroOrm.Configuration;
using OsamesMicroOrm.Logging;
using OsamesMicroOrm.Utilities;

namespace OsamesMicroOrm.DbTools
{
    /// <summary>
    /// Classe servant à formater et exécuter des requêtes SQL de type UPDATE, en proposant une abstraction au dessus de ADO.NET.
    /// </summary>
    public static class DbToolsUpdates
    {
        /// <summary>
        /// Crée le texte de la commande SQL paramétrée ainsi que les paramètres ADO.NET, dans le cas d'un update sur un seul objet.
        /// Utilise le template du style de "BaseUpdate" : <c>"UPDATE {0} SET {1} WHERE ..."</c> ainsi que les éléments suivants :
        /// <list type="bullet">
        /// <item><description>clé du dictionnaire de mapping</description></item>
        /// <item><description>un objet de données databaseEntityObject_</description></item>
        /// <item><description>liste de noms de propriétés de databaseEntityObject_ à utiliser pour les champs à mettre à jour</description></item>
        /// <item><description>nom de la propriété de databaseEntityObject_ correspondant au champ clé primaire</description></item>
        /// </list>
        /// </summary>
        /// <typeparam name="T">Type C#</typeparam>
        /// <param name="databaseEntityObject_">Instance d'un objet de la classe T</param>
        /// <param name="mappingDictionariesContainerKey_">Clé pour le dictionnaire de mapping. Toujours {0} dans le template sql</param>
        /// <param name="sqlTemplateName_">Nom du template SQL</param>
        /// <param name="lstDataObjectPropertiesNames_">Noms des propriétés de l'objet databaseEntityObject_ à utiliser pour les champs à mettre à jour. Utilisé pour la partie {1} du template SQL</param>
        /// <param name="lstWhereMetaNames_">Pour les colonnes de la clause where : valeur dont la syntaxe indique qu'il s'agit d'une propriété de classe C#/un paramètre dynamique/un littéral. 
        /// Pour formater à partir de {2} dans le template SQL. Peut être null</param>
        /// <param name="lstWhereValues_">Valeurs pour les paramètres ADO.NET. Peut être null</param>
        /// <returns>Sortie : structure contenant : texte de la commande SQL paramétrée, clé/valeur des paramètres ADO.NET pour la commande SQL paramétrée</returns>
        /// <exception cref="OOrmHandledException">Toute sorte d'erreur</exception>
        internal static InternalPreparedStatement FormatSqlForUpdate<T>(T databaseEntityObject_, string sqlTemplateName_, string mappingDictionariesContainerKey_, List<string> lstDataObjectPropertiesNames_, List<string> lstWhereMetaNames_, List<object> lstWhereValues_)
        where T : IDatabaseEntityObject
        {
            StringBuilder sbFieldsToUpdate = new StringBuilder();
            string sqlCommand;

            List<string> lstDbColumnNames;
            List<KeyValuePair<string, object>> lstAdoParameters; // Paramètres ADO.NET, à construire

            // 1. détermine les champs à mettre à jour et remplit la stringbuilder sbFieldsToUpdate
            DbToolsCommon.DetermineDatabaseColumnNamesAndAdoParameters(databaseEntityObject_, mappingDictionariesContainerKey_, lstDataObjectPropertiesNames_, out lstDbColumnNames, out lstAdoParameters);

            int iCountMinusOne = lstDbColumnNames.Count - 1;
            for (int i = 0; i < iCountMinusOne; i++)
            {
                sbFieldsToUpdate.Append(ConfigurationLoader.StartFieldEncloser).Append(lstDbColumnNames[i]).Append(ConfigurationLoader.EndFieldEncloser).Append(" = ").Append(lstAdoParameters[i].Key).Append(", ");
            }
            sbFieldsToUpdate.Append(ConfigurationLoader.StartFieldEncloser).Append(lstDbColumnNames[iCountMinusOne]).Append(ConfigurationLoader.EndFieldEncloser).Append(" = ").Append(lstAdoParameters[iCountMinusOne].Key);

            // 2. Positionne les deux premiers placeholders : nom de la table, chaîne pour les champs à mettre à jour
            List<string> sqlPlaceholders = new List<string> { string.Concat(ConfigurationLoader.StartFieldEncloser, mappingDictionariesContainerKey_, ConfigurationLoader.EndFieldEncloser), sbFieldsToUpdate.ToString() };

            // 3. Détermine les noms des paramètres pour le where
            DbToolsCommon.FillPlaceHoldersAndAdoParametersNamesAndValues(mappingDictionariesContainerKey_, lstWhereMetaNames_, lstWhereValues_, sqlPlaceholders, lstAdoParameters);

            DbToolsCommon.TryFormatTemplate(ConfigurationLoader.DicUpdateSql, sqlTemplateName_, out sqlCommand, sqlPlaceholders.ToArray());

            return new InternalPreparedStatement(new PreparedStatement(sqlCommand, lstAdoParameters.Count), lstAdoParameters);

        }

        /// <summary>
        /// Exécution d'une mise à jour d'un objet de données vers la base de données
        /// <list type="bullet">
        /// <item><description>formatage des éléments nécessaires par appel à <c>FormatSqlForUpdate &lt;T&gt;()</c></description></item>
        /// <item><description>appel de bas niveau ADO.NET</description></item>
        /// <item><description>sortie : nombre de lignes mises à jour</description></item>
        /// </list>
        /// </summary>
        /// <typeparam name="T">Type C#</typeparam>
        /// <param name="databaseEntityObject_">Instance d'un objet de la classe T</param>
        /// <param name="sqlTemplateName_">Nom du template SQL</param>
        /// <param name="lstPropertiesNames_">Noms des propriétés de l'objet databaseEntityObject_ à utiliser pour les champs à mettre à jour</param>
        /// <param name="lstWhereMetaNames_">Pour les colonnes de la clause where : indication d'une propriété de databaseEntityObject_ ou un paramètre dynamique. 
        /// Pour formater à partir de {2} dans le template SQL. Peut être null</param>
        /// <param name="lstWhereValues_">Valeurs pour les paramètres ADO.NET. Peut être null</param>
        /// <param name="transaction_">Transaction optionnelle (obtenue par appel à DbManager)</param>
        /// <returns>Retourne le nombre d'enregistrements modifiés dans la base de données.</returns>
        /// <exception cref="OOrmHandledException">any error</exception>
        public static uint Update<T>(T databaseEntityObject_, string sqlTemplateName_, List<string> lstPropertiesNames_, List<string> lstWhereMetaNames_, List<object> lstWhereValues_, OOrmDbTransactionWrapper transaction_ = null)
       where T : IDatabaseEntityObject
        {
            if (lstPropertiesNames_ == null || lstPropertiesNames_.Count == 0)
            {
                Logger.Log(TraceEventType.Warning, Utilities.OOrmErrorsHandler.FindHResultAndDescriptionByCode(HResultEnum.W_UPDATEFIELDSLISTEMPTY).Value);
                return 0;
            }

            string sqlCommand;
            List<KeyValuePair<string, object>> adoParameters;
            uint nbRowsAffected = 0;
            string mappingDictionariesContainerKey = MappingTools.GetTableNameFromMappingDictionary(typeof(T));

            InternalPreparedStatement statement = FormatSqlForUpdate(databaseEntityObject_, sqlTemplateName_, mappingDictionariesContainerKey, lstPropertiesNames_, lstWhereMetaNames_, lstWhereValues_);

            if (transaction_ != null)
            {
                // Présence d'une transaction
                if (DbManager.Instance.ExecuteNonQuery(transaction_, CommandType.Text, statement.PreparedStatement.PreparedSqlCommand, statement.AdoParameters) != 0)
                    nbRowsAffected++;
                else
                    Logger.Log(TraceEventType.Warning, Utilities.OOrmErrorsHandler.FindHResultAndDescriptionByCode(HResultEnum.W_NOROWUPDATED).Value + " : '" + statement.PreparedStatement.PreparedSqlCommand + "'");

                return nbRowsAffected;
            }

            // Pas de transaction
            OOrmDbConnectionWrapper conn = null;
            try
            {
                conn = DbManager.Instance.CreateConnection();
                if (DbManager.Instance.ExecuteNonQuery(conn, CommandType.Text, statement.PreparedStatement.PreparedSqlCommand, statement.AdoParameters) != 0)
                    nbRowsAffected++;
                else
                    Logger.Log(TraceEventType.Warning, Utilities.OOrmErrorsHandler.FindHResultAndDescriptionByCode(HResultEnum.W_NOROWUPDATED).Value + " : '" + statement.PreparedStatement.PreparedSqlCommand + "'");

                return nbRowsAffected;
            }
            finally
            {
                // Si c'est la connexion de backup alors on ne la dipose pas pour usage ultérieur.
                if (!conn.IsBackup)
                    conn.Dispose();
            }
        }

        /// <summary>
        ///  Exécution d'une mise à jour d'objets de données vers la base de données.
        /// </summary>
        /// <typeparam name="T">Type C#</typeparam>
        /// <param name="databaseEntityObjects_">Instance liste d'objets de la classe T</param>
        /// <param name="sqlTemplateName_">Nom du template SQL</param>
        /// <param name="lstPropertiesNames_">Noms des propriétés de l'objet databaseEntityObject_ à utiliser pour les champs à mettre à jour</param>
        /// <param name="lstWhereMetaNames_">Pour les colonnes de la clause where : valeur dont la syntaxe indique qu'il s'agit d'une propriété de classe C#/un paramètre dynamique/un littéral. 
        /// Pour formater à partir de {2} dans le template SQL. Peut être null</param>
        /// <param name="lstWhereValues_">Valeurs pour les paramètres ADO.NET, une liste pour chaque objet de databaseEntityObjects_. Peut être null</param>
        /// <param name="transaction_">Transaction optionnelle (obtenue par appel à DbManager)</param>
        /// <returns>Retourne le nombre d'enregistrements modifiés dans la base de données.</returns>
        /// <exception cref="OOrmHandledException">any error</exception>
        public static uint Update<T>(List<T> databaseEntityObjects_, string sqlTemplateName_, List<string> lstPropertiesNames_, List<string> lstWhereMetaNames_, List<List<object>> lstWhereValues_, OOrmDbTransactionWrapper transaction_ = null)
        where T : IDatabaseEntityObject
        {
            if (lstPropertiesNames_ == null || lstPropertiesNames_.Count == 0)
            {
                Logger.Log(TraceEventType.Warning, OOrmErrorsHandler.FindHResultAndDescriptionByCode(HResultEnum.W_UPDATEFIELDSLISTEMPTY).Value);
                return 0;
            }

            string sqlCommand = null;

            uint nbRowsAffected = 0;
            string mappingDictionariesContainerKey = MappingTools.GetTableNameFromMappingDictionary(typeof(T));


            for (int i = 0; i < databaseEntityObjects_.Count; i++)
            {
                T dataObject = databaseEntityObjects_[i];
                InternalPreparedStatement statement = FormatSqlForUpdate(dataObject, sqlTemplateName_, mappingDictionariesContainerKey, lstPropertiesNames_, lstWhereMetaNames_, lstWhereValues_[i]);
               
                if (transaction_ != null)
                {
                    // Présence d'une transaction
                    if (DbManager.Instance.ExecuteNonQuery(transaction_, CommandType.Text, statement.PreparedStatement.PreparedSqlCommand, statement.AdoParameters) != 0)
                        nbRowsAffected++;
                    else
                        Logger.Log(TraceEventType.Warning, OOrmErrorsHandler.FindHResultAndDescriptionByCode(HResultEnum.W_NOROWUPDATED).Value + " : '" + statement.PreparedStatement.PreparedSqlCommand + "'");

                    continue;
                }

                // Pas de transaction
                OOrmDbConnectionWrapper conn = null;
                try
                {
                    conn = DbManager.Instance.CreateConnection();
                    if (DbManager.Instance.ExecuteNonQuery(conn, CommandType.Text, statement.PreparedStatement.PreparedSqlCommand, statement.AdoParameters) != 0)
                        nbRowsAffected++;
                    else
                        Logger.Log(TraceEventType.Warning, OOrmErrorsHandler.FindHResultAndDescriptionByCode(HResultEnum.W_NOROWUPDATED).Value + " : '" + statement.PreparedStatement.PreparedSqlCommand + "'");
                }
                finally
                {
                    // Si c'est la connexion de backup alors on ne la dipose pas pour usage ultérieur.
                    if (!conn.IsBackup)
                        conn.Dispose();
                }
            }

            return nbRowsAffected;
        }
    }
}
