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
    internal class DbManagerFactored : IDisposable
    {

        internal DbManagerFactored()
        {
            
        }

        /// <summary>
        /// Execute with cmdParams as object array
        /// </summary>
        /// <param name="connection_"></param>
        /// <param name="cmdType_"></param>
        /// <param name="cmdText_"></param>
        /// <param name="cmdParams_"></param>
        /// <param name="lastInsertedRowId_"></param>
        internal static long Execute(OOrmDbConnectionWrapper connection_, CommandType cmdType_, string cmdText_, object[,] cmdParams_)
        {
            long commandResult;

            using (OOrmDbCommandWrapper command = new OOrmDbCommandWrapper(connection_, null, cmdText_ + ";" + DbManager.SelectLastInsertIdCommandText, cmdParams_, cmdType_))
            {
                object oValue;
                
                try { oValue = command.AdoDbCommand.ExecuteScalar(); }
                catch (Exception ex) { throw new OOrmHandledException(HResultEnum.E_EXECUTENONQUERYFAILED, ex, cmdText_); }

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
        internal static long Execute(OOrmDbConnectionWrapper connection_, CommandType cmdType_, string cmdText_, IEnumerable<OOrmDbParameter> cmdParams_)
        {
            long commandResult;

            using (OOrmDbCommandWrapper command = new OOrmDbCommandWrapper(connection_, null, cmdText_ + ";" + DbManager.SelectLastInsertIdCommandText, cmdParams_, cmdType_))
            {
                object oValue;

                try { oValue = command.AdoDbCommand.ExecuteScalar(); }
                catch (Exception ex) { throw new OOrmHandledException(HResultEnum.E_EXECUTENONQUERYFAILED, ex, cmdText_); }

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
        internal static long Execute(OOrmDbConnectionWrapper connection_, CommandType cmdType_, string cmdText_, IEnumerable<KeyValuePair<string, object>> cmdParams_)
        {
            long commandResult;

            using (OOrmDbCommandWrapper command = new OOrmDbCommandWrapper(connection_, null, cmdText_ + ";" + DbManager.SelectLastInsertIdCommandText, cmdParams_, cmdType_))
            {
                object oValue;

                try { oValue = command.AdoDbCommand.ExecuteScalar(); }
                catch (Exception ex) { throw new OOrmHandledException(HResultEnum.E_EXECUTENONQUERYFAILED, ex, cmdText_); }

                if (!Int64.TryParse(oValue.ToString(), out commandResult))
                    throw new OOrmHandledException(HResultEnum.E_LASTINSERTIDNOTNUMBER, null, "value: '" + oValue + "'");
            }
            return commandResult;
        }

        ~DbManagerFactored()
        {
            Dispose();
        }

        public void Dispose()
        {
            
        }
    }
}
