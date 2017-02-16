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
    /// 
    /// </summary>
    public static class DbToolsInserts
    {
        /// <summary>
        /// Crée le texte de la commande SQL paramétrée ainsi que les paramètres ADO.NET, dans le cas d'un insert sur un seul objet.
        /// Utilise le template du style de "BaseInsert" : <c>"INSERT INTO {0} ({1}) values ({2})"</c> ainsi que les éléments suivants :
        /// <list type="bullet">
        /// <item><description>clé du dictionnaire de mapping</description></item>
        /// <item><description>un objet de données databaseEntityObject_</description></item>
        /// <item><description>liste de noms de propriétés de databaseEntityObject_ à utiliser pour les champs à enregistrer en base de données</description></item>
        /// </list>
        /// </summary>
        /// <typeparam name="T">Type C#</typeparam>
        /// <param name="databaseEntityObject_">Objet de données</param>
        /// <param name="sqlTemplateName_">Nom du template SQL</param>
        /// <param name="mappingDictionariesContainerKey_">Clé pour le dictionnaire de mapping. Toujours {0} dans le template sql</param>
        /// <param name="lstDataObjectColumnNames_">Noms des propriétés de l'objet databaseEntityObject_ à utiliser pour les champs à enregistrer en base de données. Utilisé pour la partie {1} du template SQL</param>
        /// <returns>Sortie : structure contenant : texte de la commande SQL paramétrée, clé/valeur des paramètres ADO.NET pour la commande SQL paramétrée</returns>
        /// <exception cref="OOrmHandledException">Toute sorte d'erreur</exception>
        internal static InternalPreparedStatement FormatSqlForInsert<T>(T databaseEntityObject_, string sqlTemplateName_, string mappingDictionariesContainerKey_, List<string> lstDataObjectColumnNames_)
        where T : IDatabaseEntityObject
        {
            StringBuilder sbFieldsToInsert = new StringBuilder();
            StringBuilder sbParamToInsert = new StringBuilder();

            string sqlCommand;
            List<string> lstDbColumnNames;
            List<KeyValuePair<string, object>> lstAdoParameters; // Paramètres ADO.NET, à construire

            // 1. détermine les champs à mettre à jour et remplit la stringbuilder sbFieldsToInsert
            DbToolsCommon.DetermineDatabaseColumnNamesAndAdoParameters(databaseEntityObject_, mappingDictionariesContainerKey_, lstDataObjectColumnNames_, out lstDbColumnNames, out lstAdoParameters);

            int iCountMinusOne = lstDbColumnNames.Count - 1;
            for (int i = 0; i < iCountMinusOne; i++)
            {
                sbFieldsToInsert.Append(ConfigurationLoader.StartFieldEncloser).Append(lstDbColumnNames[i]).Append(ConfigurationLoader.EndFieldEncloser).Append(", ");
                sbParamToInsert.Append(lstAdoParameters[i].Key).Append(", ");
            }

            sbFieldsToInsert.Append(ConfigurationLoader.StartFieldEncloser).Append(lstDbColumnNames[iCountMinusOne]).Append(ConfigurationLoader.EndFieldEncloser);
            sbParamToInsert.Append(lstAdoParameters[iCountMinusOne].Key);

            // 2. Positionne les deux premiers placeholders : nom de la table, chaîne pour les champs à mettre à jour
            List<string> sqlPlaceholders = new List<string> { string.Concat(ConfigurationLoader.StartFieldEncloser, mappingDictionariesContainerKey_, ConfigurationLoader.EndFieldEncloser), sbFieldsToInsert.ToString(), sbParamToInsert.ToString() };

            string templateText;
            if (!ConfigurationLoader.DicInsertSql.TryGetValue(sqlTemplateName_, out templateText))
                throw new OOrmHandledException(HResultEnum.E_NOTEMPLATE, null, "Template: " + sqlTemplateName_);

            DbToolsCommon.TryFormatTemplate(ConfigurationLoader.DicInsertSql, sqlTemplateName_, out sqlCommand, sqlPlaceholders.ToArray());

            return new InternalPreparedStatement(new PreparedStatement(sqlCommand, lstAdoParameters.Count), lstAdoParameters);
        }

        /// <summary>
        /// Exécution d'un enregistrement d'un objet de données vers la base de données.
        /// <list type="bullet">
        /// <item><description>formatage des éléments nécessaires par appel à <c>FormatSqlForInsert &lt;T&gt;()</c></description></item>
        /// <item><description>appel de bas niveau ADO.NET</description></item>
        /// <item><description>sortie : nombre de lignes mises à jour</description></item>
        /// </list>
        /// </summary>
        /// <typeparam name="T">Type C#</typeparam>
        /// <param name="databaseEntityObject_">Instance d'un objet de la classe T</param>
        /// <param name="sqlTemplateName_">Nom du template SQL</param>
        /// <param name="lstPropertiesNames_">Noms des propriétés de l'objet databaseEntityObject_ à utiliser pour les champs à enregistrer en base de données</param>
        /// <param name="transaction_">Transaction optionnelle (obtenue par appel à DbManager)</param>
        /// <returns>Retourne la valeur de la clé primaire de l'enregistrement inséré dans la base de données.</returns>
        /// <exception cref="OOrmHandledException">Toute sorte d'erreur</exception>
        public static long Insert<T>(T databaseEntityObject_, string sqlTemplateName_, List<string> lstPropertiesNames_, OOrmDbTransactionWrapper transaction_ = null)
        where T : IDatabaseEntityObject, new()
        {
            long newRecordId;
            string mappingDictionariesContainerKey = MappingTools.GetTableNameFromMappingDictionary(typeof(T));

            InternalPreparedStatement statement = FormatSqlForInsert(databaseEntityObject_, sqlTemplateName_, mappingDictionariesContainerKey, lstPropertiesNames_);

            if (transaction_ != null)
            {
                // Présence d'une transaction
                if (DbManager.Instance.ExecuteNonQuery(transaction_, CommandType.Text, statement.PreparedStatement.PreparedSqlCommand, statement.AdoParameters, out newRecordId) == 0)
                    Logger.Log(TraceEventType.Warning, "Query didn't insert any row: '" + statement.PreparedStatement.PreparedSqlCommand + "'");
                return newRecordId;
            }

            // Pas de transaction
            OOrmDbConnectionWrapper conn = null;
            try
            {
                conn = DbManager.Instance.CreateConnection();

                if (DbManager.Instance.ExecuteNonQuery(conn, CommandType.Text, statement.PreparedStatement.PreparedSqlCommand, statement.AdoParameters, out newRecordId) == 0)
                    Logger.Log(TraceEventType.Warning, "Query didn't insert any row: '" + statement.PreparedStatement.PreparedSqlCommand + "'");
                return newRecordId;
            }
            finally
            {
                // Si c'est la connexion de backup alors on ne la dipose pas pour usage ultérieur.
                if (!conn.IsBackup)
                    conn.Dispose();
            }

        }
    }
}
