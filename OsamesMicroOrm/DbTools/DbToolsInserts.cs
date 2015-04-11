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
        /// <item><description>un objet de données dataObject_</description></item>
        /// <item><description>liste de noms de propriétés de dataObject_ à utiliser pour les champs à enregistrer en base de données</description></item>
        /// </list>
        /// </summary>
        /// <typeparam name="T">Type C#</typeparam>
        /// <param name="dataObject_">Objet de données</param>
        /// <param name="sqlTemplateName_">Nom du template SQL</param>
        /// <param name="mappingDictionariesContainerKey_">Clé pour le dictionnaire de mapping. Toujours {0} dans le template sql</param>
        /// <param name="lstDataObjectColumnNames_">Noms des propriétés de l'objet dataObject_ à utiliser pour les champs à enregistrer en base de données. Utilisé pour la partie {1} du template SQL</param>
        /// <param name="sqlCommand_">Sortie : texte de la commande SQL paramétrée</param>
        /// <param name="lstAdoParameters_">Sortie : clé/valeur des paramètres ADO.NET pour la commande SQL paramétrée</param>
        /// <param name="tryFormat">Si a vrai, on fait un try format sur le sqlcommand.
        /// A faux quand on appelle cette méthode pour une liste d'objets à enregistrer : on ne fait try format que pour le premier objet, puis la sqlcommand est réutilisée</param>
        /// <returns>Ne renvoie rien</returns>
        /// <exception cref="OOrmHandledException">Toute sorte d'erreur</exception>
        internal static void FormatSqlForInsert<T>(T dataObject_, string sqlTemplateName_, string mappingDictionariesContainerKey_, List<string> lstDataObjectColumnNames_, out string sqlCommand_, out List<KeyValuePair<string, object>> lstAdoParameters_, bool tryFormat = true)
        where T : IDatabaseEntityObject
        {
            StringBuilder sbFieldsToInsert = new StringBuilder();
            StringBuilder sbParamToInsert = new StringBuilder();
            sqlCommand_ = null;

            List<string> lstDbColumnNames;

            // 1. détermine les champs à mettre à jour et remplit la stringbuilder sbFieldsToInsert
            DbToolsCommon.DetermineDatabaseColumnNamesAndAdoParameters(dataObject_, mappingDictionariesContainerKey_, lstDataObjectColumnNames_, out lstDbColumnNames, out lstAdoParameters_);

            int iCountMinusOne = lstDbColumnNames.Count - 1;
            for (int i = 0; i < iCountMinusOne; i++)
            {
                sbFieldsToInsert.Append(ConfigurationLoader.StartFieldEncloser).Append(lstDbColumnNames[i]).Append(ConfigurationLoader.EndFieldEncloser).Append(", ");
                sbParamToInsert.Append(lstAdoParameters_[i].Key).Append(", ");
            }

            sbFieldsToInsert.Append(ConfigurationLoader.StartFieldEncloser).Append(lstDbColumnNames[iCountMinusOne]).Append(ConfigurationLoader.EndFieldEncloser);
            sbParamToInsert.Append(lstAdoParameters_[iCountMinusOne].Key);

            // 2. Positionne les deux premiers placeholders : nom de la table, chaîne pour les champs à mettre à jour
            List<string> sqlPlaceholders = new List<string> { string.Concat(ConfigurationLoader.StartFieldEncloser, mappingDictionariesContainerKey_, ConfigurationLoader.EndFieldEncloser), sbFieldsToInsert.ToString(), sbParamToInsert.ToString() };

            if (tryFormat)
            {
                DbToolsCommon.TryFormatTemplate(ConfigurationLoader.DicInsertSql, sqlTemplateName_, out sqlCommand_, sqlPlaceholders.ToArray());
            }
        }

        /// <summary>
        /// Exécution d'un enregistrement d'un objet de données vers la base de données
        /// <list type="bullet">
        /// <item><description>formatage des éléments nécessaires par appel à <c>FormatSqlForInsert &lt;T&gt;()</c></description></item>
        /// <item><description>appel de bas niveau ADO.NET</description></item>
        /// <item><description>sortie : nombre de lignes mises à jour</description></item>
        /// </list>
        /// </summary>
        /// <typeparam name="T">Type C#</typeparam>
        /// <param name="dataObject_">Instance d'un objet de la classe T</param>
        /// <param name="mappingDictionariesContainerKey_">Clé pour le dictionnaire de mapping</param>
        /// <param name="sqlTemplateName_">Contient le nom du template sql update à utiliser</param>
        /// <param name="lstPropertiesNames_">Noms des propriétés de l'objet dataObject_ à utiliser pour les champs à enregistrer en base de données</param>
        /// <param name="transaction_">Transaction optionnelle (obtenue par appel à DbManager)</param>
        /// <returns>Retourne la valeur de la clé primaire de l'enregistrement inséré dans la base de données.</returns>
        /// <exception cref="OOrmHandledException">Toute sorte d'erreur</exception>
        public static long Insert<T>(T dataObject_, string sqlTemplateName_, string mappingDictionariesContainerKey_, List<string> lstPropertiesNames_, OOrmDbTransactionWrapper transaction_ = null)
        where T : IDatabaseEntityObject
        {
            string sqlCommand;
            List<KeyValuePair<string, object>> adoParameters;
            long newRecordId;

            FormatSqlForInsert(dataObject_, sqlTemplateName_, mappingDictionariesContainerKey_, lstPropertiesNames_, out sqlCommand, out adoParameters);

            if (transaction_ != null)
            {
                // Présence d'une transaction
                if (DbManager.Instance.ExecuteNonQuery(transaction_, CommandType.Text, sqlCommand, adoParameters, out newRecordId) == 0)
                    Logger.Log(TraceEventType.Warning, "Query didn't insert any row: '" + sqlCommand + "'");
                return newRecordId;
            }

            // Pas de transaction
            OOrmDbConnectionWrapper conn = null;
            try
            {
                conn = DbManager.Instance.CreateConnection();

                if (DbManager.Instance.ExecuteNonQuery(conn, CommandType.Text, sqlCommand, adoParameters, out newRecordId) == 0)
                    Logger.Log(TraceEventType.Warning, "Query didn't insert any row: '" + sqlCommand + "'");
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
