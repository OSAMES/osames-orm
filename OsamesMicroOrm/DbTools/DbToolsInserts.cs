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

using System;
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
    public class DbToolsInserts
    {
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dataObject_"></param>
        /// <param name="sqlTemplate_"></param>
        /// <param name="mappingDictionariesContainerKey_"></param>
        /// <param name="lstDataObjectColumnNames_"></param>
        /// <param name="sqlCommand_"></param>
        /// <param name="lstAdoParameters_"></param>
        /// <param name="strErrorMsg_"></param>
        /// <param name="tryFormat"></param>
        internal static void FormatSqlForInsert<T>(T dataObject_, string sqlTemplate_, string mappingDictionariesContainerKey_, List<string> lstDataObjectColumnNames_, out string sqlCommand_, out List<KeyValuePair<string, object>> lstAdoParameters_, out string strErrorMsg_, bool tryFormat = true)
        {
            StringBuilder sbFieldsToInsert = new StringBuilder();
            StringBuilder sbParamToInsert = new StringBuilder();
            strErrorMsg_ = sqlCommand_ = null;

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

            // 2. Positionne les deux premiers placeholders
            List<string> sqlPlaceholders = new List<string> { string.Concat(ConfigurationLoader.StartFieldEncloser, mappingDictionariesContainerKey_, ConfigurationLoader.EndFieldEncloser), sbFieldsToInsert.ToString(), sbParamToInsert.ToString() };

            if (tryFormat)
                DbToolsCommon.TryFormat(ConfigurationLoader.DicInsertSql[sqlTemplate_], out sqlCommand_, out strErrorMsg_, sqlPlaceholders.ToArray());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dataObject_"></param>
        /// <param name="sqlTemplate_"></param>
        /// <param name="mappingDictionariesContainerKey_"></param>
        /// <param name="lstPropertiesNames_"></param>
        /// <param name="strErrorMsg_"></param>
        /// <param name="transaction_"></param>
        /// <returns></returns>
        /// <exception cref="OOrmHandledException">any error</exception>
        public static long Insert<T>(T dataObject_, string sqlTemplate_, string mappingDictionariesContainerKey_, List<string> lstPropertiesNames_, out string strErrorMsg_, OOrmDbTransactionWrapper transaction_ = null)
        {
            string sqlCommand;
            List<KeyValuePair<string, object>> adoParameters;
            long newRecordId_ = 0;

            FormatSqlForInsert(dataObject_, sqlTemplate_, mappingDictionariesContainerKey_, lstPropertiesNames_, out sqlCommand, out adoParameters, out strErrorMsg_);

            if (transaction_ != null)
            {
                // Présence d'une transaction
                if (DbManager.Instance.ExecuteNonQuery(transaction_, CommandType.Text, sqlCommand, adoParameters, out newRecordId_) == 0)
                    Logger.Log(TraceEventType.Warning, "Query didn't insert any row: " + sqlCommand);
                return newRecordId_;
            }

            // Pas de transaction
            OOrmDbConnectionWrapper conn = null;
            try
            {
                conn = DbManager.Instance.CreateConnection();

                if (DbManager.Instance.ExecuteNonQuery(conn, CommandType.Text, sqlCommand, adoParameters, out newRecordId_) == 0)
                    Logger.Log(TraceEventType.Warning, "Query didn't insert any row: " + sqlCommand);
                return newRecordId_;
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
