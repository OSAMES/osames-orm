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
using System.Diagnostics.Eventing.Reader;
using System.Text;
using OsamesMicroOrm.Configuration;
using OsamesMicroOrm.Logging;

namespace OsamesMicroOrm.DbTools
{
    /// <summary>
    /// Classe servant à formater et exécuter des requêtes SQL de type UPDATE, en proposant une abstraction au dessus de ADO.NET.
    /// </summary>
    public class DbToolsUpdates
    {
        /// <summary>
        /// Crée le texte de la commande SQL paramétrée ainsi que les paramètres ADO.NET, dans le cas d'un update sur un seul objet.
        /// Utilise le template "BaseUpdate" : <c>"UPDATE {0} SET {1} WHERE {2}"</c> ainsi que les éléments suivants :
        /// <list type="bullet">
        /// <item><description>clé du dictionnaire de mapping</description></item>
        /// <item><description>un objet de données dataObject_</description></item>
        /// <item><description>liste de noms de propriétés de dataObject_ à utiliser pour les champs à mettre à jour</description></item>
        /// <item><description>nom de la propriété de dataObject_ correspondant au champ clé primaire</description></item>
        /// </list>
        /// </summary>
        /// <typeparam name="T">Type C#</typeparam>
        /// <param name="dataObject_">Instance d'un objet de la classe T</param>
        /// <param name="mappingDictionariesContainerKey_">Clé pour le dictionnaire de mapping</param>
        /// <param name="sqlTemplate_">Contient le nom du template sql update à utiliser</param>
        /// <param name="lstDataObjectColumnNames_">Noms des propriétés de l'objet dataObject_ à utiliser pour les champs à mettre à jour</param>
        /// <param name="lstWhereMetaNames_">Pour les colonnes de la clause where : indication d'une propriété de dataObject_ ou un paramètre dynamique. 
        /// Pour formater à partir de {2} dans le template SQL. Peut être null</param>
        /// <param name="lstWhereValues_">Valeurs pour les paramètres ADO.NET. Peut être null</param>
        /// <param name="sqlCommand_">Sortie : texte de la commande SQL paramétrée</param>
        /// <param name="lstAdoParameters_">Sortie : clé/valeur des paramètres ADO.NET pour la commande SQL paramétrée</param>
        /// <param name="tryFormat">Si a vrai, on fait un try format sur le sqlcommand</param>
        /// <returns>Ne renvoie rien</returns>
        /// <exception cref="OOrmHandledException">Toute sorte d'erreur</exception>
        internal static void FormatSqlForUpdate<T>(T dataObject_, string sqlTemplate_, string mappingDictionariesContainerKey_, List<string> lstDataObjectColumnNames_, List<string> lstWhereMetaNames_, List<object> lstWhereValues_, out string sqlCommand_, out List<KeyValuePair<string, object>> lstAdoParameters_, bool tryFormat = true)
        {
            StringBuilder sbFieldsToUpdate = new StringBuilder();
            sqlCommand_ = null;

            List<string> lstDbColumnNames;

            // 1. détermine les champs à mettre à jour et remplit la stringbuilder sbFieldsToUpdate
            DbToolsCommon.DetermineDatabaseColumnNamesAndAdoParameters(dataObject_, mappingDictionariesContainerKey_, lstDataObjectColumnNames_, out lstDbColumnNames, out lstAdoParameters_);

            int iCountMinusOne = lstDbColumnNames.Count - 1;
            for (int i = 0; i < iCountMinusOne; i++)
            {
                sbFieldsToUpdate.Append(ConfigurationLoader.StartFieldEncloser).Append(lstDbColumnNames[i]).Append(ConfigurationLoader.EndFieldEncloser).Append(" = ").Append(lstAdoParameters_[i].Key).Append(", ");
            }
            sbFieldsToUpdate.Append(ConfigurationLoader.StartFieldEncloser).Append(lstDbColumnNames[iCountMinusOne]).Append(ConfigurationLoader.EndFieldEncloser).Append(" = ").Append(lstAdoParameters_[iCountMinusOne].Key);

            // 2. Positionne les deux premiers placeholders
            List<string> sqlPlaceholders = new List<string> { string.Concat(ConfigurationLoader.StartFieldEncloser, mappingDictionariesContainerKey_, ConfigurationLoader.EndFieldEncloser), sbFieldsToUpdate.ToString() };

            // 3. Détermine les noms des paramètres pour le where
            DbToolsCommon.FillPlaceHoldersAndAdoParametersNamesAndValues(mappingDictionariesContainerKey_, lstWhereMetaNames_, lstWhereValues_, sqlPlaceholders, lstAdoParameters_);

            if (tryFormat)
            {
                string templateName;
                if (!ConfigurationLoader.DicUpdateSql.TryGetValue(sqlTemplate_, out templateName))
                    throw new OOrmHandledException(HResultEnum.E_NOTEMPLATE, null, "Template: " + sqlTemplate_);

                DbToolsCommon.TryFormat(templateName, out sqlCommand_, sqlPlaceholders.ToArray());
            }

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
        /// <param name="dataObject_">Instance d'un objet de la classe T</param>
        /// <param name="mappingDictionariesContainerKey_">Clé pour le dictionnaire de mapping</param>
        /// <param name="sqlTemplate_">Contient le nom du template sql update à utiliser</param>
        /// <param name="lstPropertiesNames_">Noms des propriétés de l'objet dataObject_ à utiliser pour les champs à mettre à jour</param>
        /// <param name="lstWhereColumnNames_">Pour les colonnes de la clause where : indication d'une propriété de dataObject_ ou un paramètre dynamique. 
        /// Pour formater à partir de {2} dans le template SQL. Peut être null</param>
        /// <param name="lstWhereValues_">Valeurs pour les paramètres ADO.NET. Peut être null</param>
        /// <param name="strErrorMsg_">Retourne un message d'erreur en cas d'échec</param>
        /// <param name="transaction_">Transaction optionnelle (obtenue par appel à DbManager)</param>
        /// <returns>Retourne le nombre d'enregistrements modifiés dans la base de données.</returns>
        /// <exception cref="OOrmHandledException">any error</exception>
        public static int Update<T>(T dataObject_, string sqlTemplate_, string mappingDictionariesContainerKey_, List<string> lstPropertiesNames_, List<string> lstWhereColumnNames_, List<object> lstWhereValues_, out string strErrorMsg_, OOrmDbTransactionWrapper transaction_ = null)
        {
            string sqlCommand;
            List<KeyValuePair<string, object>> adoParameters;
            int nbRowsAffected = 0;

            strErrorMsg_ = null;
            FormatSqlForUpdate(dataObject_, sqlTemplate_, mappingDictionariesContainerKey_, lstPropertiesNames_, lstWhereColumnNames_, lstWhereValues_, out sqlCommand, out adoParameters);

            if (transaction_ != null)
            {
                // Présence d'une transaction
                if (DbManager.Instance.ExecuteNonQuery(transaction_, CommandType.Text, sqlCommand, adoParameters) == 0)
                    Logger.Log(TraceEventType.Warning, "Query didn't update any row: " + sqlCommand);
                else
                    nbRowsAffected++;

                return nbRowsAffected;
            }

            // Pas de transaction
            OOrmDbConnectionWrapper conn = null;
            try
            {
                conn = DbManager.Instance.CreateConnection();
                if (DbManager.Instance.ExecuteNonQuery(conn, CommandType.Text, sqlCommand, adoParameters) == 0)
                    Logger.Log(TraceEventType.Warning, "Query didn't update any row: " + sqlCommand);
                else
                    nbRowsAffected++;

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
        ///  Exécution d'une mise à jour d'objets de données vers la base de données
        /// </summary>
        /// <typeparam name="T">Type C#</typeparam>
        /// <param name="dataObjects_">Instance liste d'objets de la classe T</param>
        /// <param name="mappingDictionariesContainerKey_">Clé pour le dictionnaire de mapping</param>
        /// <param name="sqlTemplate_">Contient le nom du template sql update à utiliser</param>
        /// <param name="lstPropertiesNames_">Noms des propriétés de l'objet dataObject_ à utiliser pour les champs à mettre à jour</param>
        /// <param name="lstWhereColumnNames_">Pour les colonnes de la clause where : indication d'une propriété de dataObject_ ou un paramètre dynamique. 
        /// Pour formater à partir de {2} dans le template SQL. Peut être null</param>
        /// <param name="lstWhereValues_">Valeurs pour les paramètres ADO.NET. Peut être null</param>
        /// <param name="strErrorMsg_">Retourne un message d'erreur en cas d'échec</param>
        /// <param name="transaction_">Transaction optionnelle (obtenue par appel à DbManager)</param>
        /// <returns>Retourne le nombre d'enregistrements modifiés dans la base de données.</returns>
        /// <exception cref="OOrmHandledException">any error</exception>
        public static int Update<T>(List<T> dataObjects_, string sqlTemplate_, string mappingDictionariesContainerKey_, List<string> lstPropertiesNames_, List<string> lstWhereColumnNames_, List<object> lstWhereValues_, out string strErrorMsg_, OOrmDbTransactionWrapper transaction_ = null)
        {
            string sqlCommand = null;
            string tmpSqlCommand;
            string tmpStrErrorMsg;

            List<KeyValuePair<string, object>> adoParameters;
            int nbRowsAffected = 0;
            strErrorMsg_ = "";

            for (int i = 0; i < dataObjects_.Count; i++)
            {
                T dataObject = dataObjects_[i];
                if (i == 0)
                    //on tryformat le sqlcommand
                    FormatSqlForUpdate(dataObject, sqlTemplate_, mappingDictionariesContainerKey_, lstPropertiesNames_, lstWhereColumnNames_, lstWhereValues_, out sqlCommand, out adoParameters);
                else
                    // ici le slqcommand rendu est null
                    FormatSqlForUpdate(dataObject, sqlTemplate_, mappingDictionariesContainerKey_, lstPropertiesNames_, lstWhereColumnNames_, lstWhereValues_, out tmpSqlCommand, out adoParameters, false);

                if (transaction_ != null)
                {
                    // Présence d'une transaction
                    if (DbManager.Instance.ExecuteNonQuery(transaction_, CommandType.Text, sqlCommand, adoParameters) == 0)
                        Logger.Log(TraceEventType.Warning, "Query didn't update any row: " + sqlCommand);
                    else
                        nbRowsAffected++;

                    continue;
                }

                // Pas de transaction
                OOrmDbConnectionWrapper conn = null;
                try
                {
                    conn = DbManager.Instance.CreateConnection();
                    if (DbManager.Instance.ExecuteNonQuery(conn, CommandType.Text, sqlCommand, adoParameters) == 0)
                        Logger.Log(TraceEventType.Warning, "Query didn't update any row: " + sqlCommand);
                    else
                        nbRowsAffected++;
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
