using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OsamesMicroOrm;

namespace OsamesMicroOrm
{
    /// <summary>
    /// Child of DbManager
    /// </summary>
    internal class DbManagerHelper : IDisposable
    {
        private static OOrmDbConnectionWrapper connection;
        private static OOrmDbTransactionWrapper transaction;
        private static CommandType cmdType;
        private static string cmdText;

        internal DbManagerHelper(OOrmDbConnectionWrapper connection_, CommandType cmdType_, string cmdText_)
        {
            connection = connection_;
            cmdType = cmdType_;
            cmdText = cmdText_;
        }

        internal DbManagerHelper(OOrmDbTransactionWrapper transaction_, CommandType cmdType_, string cmdText_)
        {
            transaction = transaction_;
            cmdType = cmdType_;
            cmdText = cmdText_;
        }

        /// <summary>
        /// Execute with cmdParams as object array
        /// </summary>
        /// <param name="connection_"></param>
        /// <param name="cmdType_"></param>
        /// <param name="cmdText_"></param>
        /// <param name="cmdParams_"></param>
        /// <param name="lastInsertedRowId_"></param>
        internal long Execute(object[,] cmdParams_)
        {
            long commandResult;

            using (OOrmDbCommandWrapper command = new OOrmDbCommandWrapper(connection, null, cmdText + ";" + DbManager.SelectLastInsertIdCommandText, cmdParams_, cmdType))
            {
                object oValue;
                
                try { oValue = command.AdoDbCommand.ExecuteScalar(); }
                catch (Exception ex) { throw new OOrmHandledException(HResultEnum.E_EXECUTENONQUERYFAILED, ex, cmdText); }

                if (!Int64.TryParse(oValue.ToString(),out commandResult))
                    throw new OOrmHandledException(HResultEnum.E_LASTINSERTIDNOTNUMBER, null, "value: '" + oValue + "'");
            }
            return commandResult;
        }

        /// <summary>
        /// Execute with cmdParams as IEnumerable of OOrmDbParameter
        /// </summary>
        /// <param name="connection_"></param>
        /// <param name="cmdType_"></param>
        /// <param name="cmdText_"></param>
        /// <param name="cmdParams_"></param>
        /// <returns></returns>
        internal long Execute(IEnumerable<OOrmDbParameter> cmdParams_)
        {
            long commandResult;

            using (OOrmDbCommandWrapper command = new OOrmDbCommandWrapper(connection, null, cmdText + ";" + DbManager.SelectLastInsertIdCommandText, cmdParams_, cmdType))
            {
                object oValue;

                try { oValue = command.AdoDbCommand.ExecuteScalar(); }
                catch (Exception ex) { throw new OOrmHandledException(HResultEnum.E_EXECUTENONQUERYFAILED, ex, cmdText); }

                if (!Int64.TryParse(oValue.ToString(), out commandResult))
                    throw new OOrmHandledException(HResultEnum.E_LASTINSERTIDNOTNUMBER, null, "value: '" + oValue + "'");
            }
            return commandResult;
        }

        /// <summary>
        /// Execute with cmdParams as IEnumerable of  KeyValuePair of <string, object>
        /// </summary>
        /// <param name="connection_"></param>
        /// <param name="cmdType_"></param>
        /// <param name="cmdText_"></param>
        /// <param name="cmdParams_"></param>
        /// <returns></returns>
        internal long Execute(IEnumerable<KeyValuePair<string, object>> cmdParams_)
        {
            long commandResult;

            using (OOrmDbCommandWrapper command = new OOrmDbCommandWrapper(connection, null, cmdText + ";" + DbManager.SelectLastInsertIdCommandText, cmdParams_, cmdType))
            {
                object oValue;

                try { oValue = command.AdoDbCommand.ExecuteScalar(); }
                catch (Exception ex) { throw new OOrmHandledException(HResultEnum.E_EXECUTENONQUERYFAILED, ex, cmdText); }

                if (!Int64.TryParse(oValue.ToString(), out commandResult))
                    throw new OOrmHandledException(HResultEnum.E_LASTINSERTIDNOTNUMBER, null, "value: '" + oValue + "'");
            }
            return commandResult;
        }

        ~DbManagerHelper()
        {
            Dispose();
        }

        public void Dispose()
        {
            
        }
    }
}
