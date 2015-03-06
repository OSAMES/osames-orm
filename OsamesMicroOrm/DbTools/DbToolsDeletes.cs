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
using System.Text;
using OsamesMicroOrm.Configuration;

namespace OsamesMicroOrm.DbTools
{
    /// <summary>
    /// TODO
    /// </summary>
    public class DbToolsDeletes
    {
        /// <summary>
        /// Exécute une requête de type "DELETE FROM {0} WHERE ...".
        /// TODO
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dataObject_"></param>
        /// <param name="refSqlTemplate_"></param>
        /// <param name="mappingDictionariesContainerKey_"></param>
        /// <param name="lstWhereMetaNames_"></param>
        /// <param name="lstWhereValues_"></param>
        /// <param name="transaction_"></param>
        public static void Delete<T>(T dataObject_, string refSqlTemplate_, string mappingDictionariesContainerKey_, List<string> lstWhereMetaNames_, List<object> lstWhereValues_,
            OOrmDbTransactionWrapper transaction_ = null)
        {
            string sqlCommand;
            List<KeyValuePair<string, object>> adoParameters;
            List<string> lstDbColumnNames;
            List<string> lstPropertiesNames;

            FormatSqlForSelectAutoDetermineSelectedFields(refSqlTemplate_, mappingDictionariesContainerKey_, lstWhereMetaNames_, lstWhereValues_, out sqlCommand,
                out adoParameters, out lstPropertiesNames, out lstDbColumnNames, true);

            // Transaction
            if (transaction_ != null)
            {
                long.TryParse(DbManager.Instance.ExecuteScalar(transaction_, sqlCommand, adoParameters).ToString(), out count);
                return;

            }
            // Pas de transaction
            OOrmDbConnectionWrapper conn = null;
            try
            {
                conn = DbManager.Instance.CreateConnection();
                long.TryParse(DbManager.Instance.ExecuteScalar(conn, sqlCommand, adoParameters).ToString(), out count);
                return;
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
