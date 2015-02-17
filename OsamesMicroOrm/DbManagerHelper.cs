﻿using System;
using System.Collections.Generic;
using System.Data;

namespace OsamesMicroOrm
{
    /// <summary>
    /// Child of DbManager
    /// </summary>
    internal class DbManagerHelper : IDisposable
    {
        private OOrmDbConnectionWrapper connection;
        private OOrmDbTransactionWrapper transaction;
        private CommandType cmdType;
        private string cmdText;
        private sqlCommandType sqlCommandType;

        #region CONSTRUCTOR
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection_"></param>
        /// <param name="cmdType_"></param>
        /// <param name="cmdText_"></param>
        /// <param name="commandType_"></param>
        internal DbManagerHelper(OOrmDbConnectionWrapper connection_, CommandType cmdType_, string cmdText_, sqlCommandType commandType_)
        {
            connection = connection_;
            cmdType = cmdType_;
            cmdText = cmdText_;
            sqlCommandType = commandType_;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection_"></param>
        /// <param name="transaction_"></param>
        /// <param name="cmdType_"></param>
        /// <param name="cmdText_"></param>
        /// <param name="commandType_"></param>
        internal DbManagerHelper(OOrmDbConnectionWrapper connection_, OOrmDbTransactionWrapper transaction_, CommandType cmdType_, string cmdText_, sqlCommandType commandType_)
            : this(connection_, cmdType_, cmdText_, commandType_)
        {
            transaction = transaction_;
        }
        #endregion

        #region EXECUTE

        /// <summary>
        /// Execute with cmdParams as object array
        /// </summary>
        /// <param name="cmdParams_"></param>
        internal T Execute<T>(object[,] cmdParams_)
        {
            long commandResult; //used to return insert or update value

            using (OOrmDbCommandWrapper command = new OOrmDbCommandWrapper(connection, transaction, cmdText, cmdParams_, cmdType))
            {
                if (sqlCommandType == sqlCommandType.Insert) //if is insert
                {
                    object oValue;
                    try
                    {
                        oValue = command.AdoDbCommand.ExecuteScalar();
                    }
                    catch (Exception ex)
                    {
                        throw new OOrmHandledException(HResultEnum.E_EXECUTENONQUERYFAILED, ex, cmdText);
                    }
                    if (!Int64.TryParse(oValue.ToString(), out commandResult))
                        throw new OOrmHandledException(HResultEnum.E_LASTINSERTIDNOTNUMBER, null, "value: '" + oValue + "'");
                    return (T)Convert.ChangeType(commandResult, typeof(T));
                }

                //if is update
                try
                {
                    commandResult = command.AdoDbCommand.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    throw new OOrmHandledException(HResultEnum.E_EXECUTENONQUERYFAILED, ex, cmdText);
                }
            }
            return (T)Convert.ChangeType(commandResult, typeof(T));
        }

        /// <summary>
        /// Execute with cmdParams as IEnumerable of OOrmDbParameter
        /// </summary>
        /// <param name="cmdParams_"></param>
        /// <returns></returns>
        internal T Execute<T>(IEnumerable<OOrmDbParameter> cmdParams_)
        {
            long commandResult;   //used to return insert or update value

            using (OOrmDbCommandWrapper command = new OOrmDbCommandWrapper(connection, transaction, cmdText, cmdParams_, cmdType))
            {
                if (sqlCommandType == sqlCommandType.Insert) //if is insert
                {
                    object oValue;

                    try
                    {
                        oValue = command.AdoDbCommand.ExecuteScalar();
                    }
                    catch (Exception ex)
                    {
                        throw new OOrmHandledException(HResultEnum.E_EXECUTENONQUERYFAILED, ex, cmdText);
                    }

                    if (!Int64.TryParse(oValue.ToString(), out commandResult))
                        throw new OOrmHandledException(HResultEnum.E_LASTINSERTIDNOTNUMBER, null, "value: '" + oValue + "'");
                    return (T)Convert.ChangeType(commandResult, typeof(T));
                }

                //if is update
                try
                {
                    commandResult = command.AdoDbCommand.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    throw new OOrmHandledException(HResultEnum.E_EXECUTENONQUERYFAILED, ex, cmdText);
                }
            }
            return (T)Convert.ChangeType(commandResult, typeof(T));
        }

        /// <summary>
        /// Execute with cmdParams as IEnumerable of  KeyValuePair of <string, object>
        /// </summary>
        /// <param name="cmdParams_"></param>
        /// <returns></returns>
        internal T Execute<T>(IEnumerable<KeyValuePair<string, object>> cmdParams_)
        {
            long commandResult;   //used to return insert or update value

            using (OOrmDbCommandWrapper command = new OOrmDbCommandWrapper(connection, transaction, cmdText, cmdParams_, cmdType))
            {
                if (sqlCommandType == sqlCommandType.Insert) //if is insert
                {
                    object oValue;

                    try
                    {
                        oValue = command.AdoDbCommand.ExecuteScalar();
                    }
                    catch (Exception ex)
                    {
                        throw new OOrmHandledException(HResultEnum.E_EXECUTENONQUERYFAILED, ex, cmdText);
                    }

                    if (!Int64.TryParse(oValue.ToString(), out commandResult))
                        throw new OOrmHandledException(HResultEnum.E_LASTINSERTIDNOTNUMBER, null, "value: '" + oValue + "'");
                    return (T)Convert.ChangeType(commandResult, typeof(T));
                }

                //if is update
                try
                {
                    commandResult = command.AdoDbCommand.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    throw new OOrmHandledException(HResultEnum.E_EXECUTENONQUERYFAILED, ex, cmdText);
                }
            }
            return (T)Convert.ChangeType(commandResult, typeof(T));
        }

        #endregion


        ~DbManagerHelper()
        {
            Dispose();
        }

        public void Dispose()
        {

        }
    }

    internal enum sqlCommandType
    {
        Insert,
        Update
    }
}