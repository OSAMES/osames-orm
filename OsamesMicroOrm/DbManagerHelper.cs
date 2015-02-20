using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace OsamesMicroOrm
{
    /// <summary>
    /// Child of DbManager
    /// </summary>
    internal class DbManagerHelper<T> : IDisposable
    {
        private readonly OOrmDbConnectionWrapper connection;
        private readonly OOrmDbTransactionWrapper transaction;
        private readonly CommandType cmdType;
        private readonly string cmdText;
        private readonly SqlCommandType sqlCommandType;

        #region CONSTRUCTOR
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection_"></param>
        /// <param name="cmdType_"></param>
        /// <param name="cmdText_"></param>
        /// <param name="commandType_"></param>
        internal DbManagerHelper(OOrmDbConnectionWrapper connection_, CommandType cmdType_, string cmdText_, SqlCommandType commandType_)
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
        internal DbManagerHelper(OOrmDbConnectionWrapper connection_, OOrmDbTransactionWrapper transaction_, CommandType cmdType_, string cmdText_, SqlCommandType commandType_)
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
        internal T Execute(object[,] cmdParams_)
        {
			long commandResult; //used to return insert or update value
            using (PrepareCommandHelper<T> command = new PrepareCommandHelper<T>(connection, transaction, cmdText, (dynamic)cmdParams_, cmdType))
            {
                if (sqlCommandType == SqlCommandType.Update)
                {
                    // cas du UPDATE (le plus fréquent)
                    try
                    {
                        commandResult = command.AdoDbCommand.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        throw new OOrmHandledException(HResultEnum.E_EXECUTENONQUERYFAILED, ex, cmdText);
                    }
                }
                else
                {
                    // case du INSERT
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
                }
            }

            return (T)Convert.ChangeType(commandResult, typeof(T));
        }

        /// <summary>
        /// Execute with cmdParams as IEnumerable of OOrmDbParameter
        /// </summary>
        /// <param name="cmdParams_"></param>
        /// <returns></returns>
        internal T Execute(IEnumerable<OOrmDbParameter> cmdParams_)
        {
            long commandResult; //used to return insert or update value
            using (PrepareCommandHelper<T> command = new PrepareCommandHelper<T>(connection, transaction, cmdText, (dynamic)cmdParams_, cmdType))
            {
                if (sqlCommandType == SqlCommandType.Update)
                {
                    // cas du UPDATE (le plus fréquent)
                    try
                    {
                        commandResult = command.AdoDbCommand.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        throw new OOrmHandledException(HResultEnum.E_EXECUTENONQUERYFAILED, ex, cmdText);
                    }
                }
                else
                {
                    // case du INSERT
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
                }
            }

            return (T)Convert.ChangeType(commandResult, typeof(T));
        }

        /// <summary>
        /// Execute with cmdParams as IEnumerable of  KeyValuePair of &lt;string, object&gt;
        /// </summary>
        /// <param name="cmdParams_"></param>
        /// <returns></returns>
        internal T Execute(IEnumerable<KeyValuePair<string, object>> cmdParams_)
        {
            long commandResult; //used to return insert or update value
            using (PrepareCommandHelper<T> command = new PrepareCommandHelper<T>(connection, transaction, cmdText, (dynamic)cmdParams_, cmdType))
            {
                switch (sqlCommandType)
                {
                    // cas du INSERT
                    case SqlCommandType.Insert:
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
                        break;
                    default:
                        // cas du UPDATE
                        try
                        {
                            commandResult = command.AdoDbCommand.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            throw new OOrmHandledException(HResultEnum.E_EXECUTENONQUERYFAILED, ex, cmdText);
                        }
                        break;
                }
            }
            return (T)Convert.ChangeType(commandResult, typeof(T));
        }

        #endregion

        #region READER
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cmdParams_"></param>
        /// <returns></returns>
        internal DbDataReader ExecuteReader(object[,] cmdParams_)
        {
            using (PrepareCommandHelper<T> command = new PrepareCommandHelper<T>(connection, transaction, cmdText, (dynamic)cmdParams_, cmdType))
                try
                {
                    DbDataReader dr = command.AdoDbCommand.ExecuteReader(CommandBehavior.Default);
                    return dr;
                }
                catch (Exception ex)
                {
                    throw new OOrmHandledException(HResultEnum.E_EXECUTEREADERFAILED, ex, cmdText);
                }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cmdParams_"></param>
        /// <returns></returns>
        internal DbDataReader ExecuteReader(IEnumerable<OOrmDbParameter> cmdParams_)
        {
            using (PrepareCommandHelper<T> command = new PrepareCommandHelper<T>(connection, transaction, cmdText, (dynamic)cmdParams_, cmdType))
                try
                {
                    DbDataReader dr = command.AdoDbCommand.ExecuteReader(CommandBehavior.Default);
                    return dr;
                }
                catch (Exception ex)
                {
                    throw new OOrmHandledException(HResultEnum.E_EXECUTEREADERFAILED, ex, cmdText);
                }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cmdParams_"></param>
        /// <returns></returns>
        internal DbDataReader ExecuteReader(IEnumerable<KeyValuePair<string, object>> cmdParams_)
        {
            using (PrepareCommandHelper<T> command = new PrepareCommandHelper<T>(connection, transaction, cmdText, (dynamic)cmdParams_, cmdType))
                try
                {
                    DbDataReader dr = command.AdoDbCommand.ExecuteReader(CommandBehavior.Default);
                    return dr;
                }
                catch (Exception ex)
                {
                    throw new OOrmHandledException(HResultEnum.E_EXECUTEREADERFAILED, ex, cmdText);
                }
        }

        #endregion

        #region ADAPTER

        /// <summary>
        /// Datas the adapter.
        /// </summary>
        /// <returns>The adapter.</returns>
        /// <param name="cmdParams_">Cmd parameters.</param>
        internal DataSet DataAdapter(object[,] cmdParams_)
        {
            using (PrepareCommandHelper<T> command = new PrepareCommandHelper<T>(connection, transaction, cmdText, (dynamic)cmdParams_, cmdType))
                try
                {

                    DbDataAdapter dda = DbManager.Instance.DbProviderFactory.CreateDataAdapter();

                    if (dda == null)
                    {
                        throw new OOrmHandledException(HResultEnum.E_CREATEDATAADAPTERFAILED, null, null);
                    }
                    dda.SelectCommand = command.AdoDbCommand;
                    DataSet ds = new DataSet();
                    dda.Fill(ds);
                    return ds;
                }
                catch (Exception ex)
                {
                    if (ex is OOrmHandledException)
                        throw;
                    throw new OOrmHandledException(HResultEnum.E_FILLDATASETFAILED, ex, cmdText);
                }
        }

        /// <summary>
        /// Datas the adapter.
        /// </summary>
        /// <returns>The adapter.</returns>
        /// <param name="cmdParams_">Cmd parameters.</param>
        internal DataSet DataAdapter(IEnumerable<OOrmDbParameter> cmdParams_)
        {
            using (PrepareCommandHelper<T> command = new PrepareCommandHelper<T>(connection, transaction, cmdText, (dynamic)cmdParams_, cmdType))
                try
                {

                    DbDataAdapter dda = DbManager.Instance.DbProviderFactory.CreateDataAdapter();

                    if (dda == null)
                    {
                        throw new OOrmHandledException(HResultEnum.E_CREATEDATAADAPTERFAILED, null, null);
                    }
                    dda.SelectCommand = command.AdoDbCommand;
                    DataSet ds = new DataSet();
                    dda.Fill(ds);
                    return ds;
                }
                catch (Exception ex)
                {
                    if (ex is OOrmHandledException)
                        throw;
                    throw new OOrmHandledException(HResultEnum.E_FILLDATASETFAILED, ex, cmdText);
                }
        }

        /// <summary>
        /// Datas the adapter.
        /// </summary>
        /// <returns>The adapter.</returns>
        /// <param name="cmdParams_">Cmd parameters.</param>
        internal DataSet DataAdapter(IEnumerable<KeyValuePair<string, object>> cmdParams_)
        {
            using (PrepareCommandHelper<T> command = new PrepareCommandHelper<T>(connection, transaction, cmdText, (dynamic)cmdParams_, cmdType))
                try
                {

                    DbDataAdapter dda = DbManager.Instance.DbProviderFactory.CreateDataAdapter();

                    if (dda == null)
                    {
                        throw new OOrmHandledException(HResultEnum.E_CREATEDATAADAPTERFAILED, null, null);
                    }
                    dda.SelectCommand = command.AdoDbCommand;
                    DataSet ds = new DataSet();
                    dda.Fill(ds);
                    return ds;
                }
                catch (Exception ex)
                {
                    if (ex is OOrmHandledException)
                        throw;
                    throw new OOrmHandledException(HResultEnum.E_FILLDATASETFAILED, ex, cmdText);
                }
        }

        #endregion

        #region EXECUTE SCALAR
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cmdParams_"></param>
        /// <returns></returns>
        internal object ExecuteScalar(object[,] cmdParams_)
        {
            using (PrepareCommandHelper<T> command = new PrepareCommandHelper<T>(connection, transaction, cmdText, (dynamic)cmdParams_, cmdType))
            {
                try
                {
                    return command.AdoDbCommand.ExecuteScalar();

                }
                catch (Exception ex)
                {
                    throw new OOrmHandledException(HResultEnum.E_EXECUTESCALARFAILED, ex, cmdText);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cmdParams_"></param>
        /// <returns></returns>
        internal object ExecuteScalar(IEnumerable<OOrmDbParameter> cmdParams_)
        {
            using (PrepareCommandHelper<T> command = new PrepareCommandHelper<T>(connection, transaction, cmdText, (dynamic)cmdParams_, cmdType))
            {
                try
                {
                    return command.AdoDbCommand.ExecuteScalar();

                }
                catch (Exception ex)
                {
                    throw new OOrmHandledException(HResultEnum.E_EXECUTESCALARFAILED, ex, cmdText);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cmdParams_"></param>
        /// <returns></returns>
        internal object ExecuteScalar(IEnumerable<KeyValuePair<string, object>> cmdParams_)
        {
            using (PrepareCommandHelper<T> command = new PrepareCommandHelper<T>(connection, transaction, cmdText, (dynamic)cmdParams_, cmdType))
            {
                try
                {
                    return command.AdoDbCommand.ExecuteScalar();

                }
                catch (Exception ex)
                {
                    throw new OOrmHandledException(HResultEnum.E_EXECUTESCALARFAILED, ex, cmdText);
                }
            }
        }
        #endregion

        #region DESTRUCTOR
        ~DbManagerHelper()
        {
            Dispose();
        }

        public void Dispose()
        {

        }
        #endregion
    }

    internal enum SqlCommandType
    {
        Insert,
        Update,
        Adapter
    }
}
