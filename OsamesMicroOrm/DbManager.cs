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
using System.Data.Common;
using System.Diagnostics;
using OsamesMicroOrm.Configuration;

namespace OsamesMicroOrm
{
    /// <summary>
    /// Generic ADO.NET level, multi thread class, that deals with database providers and database query execution.
    /// </summary>
    public class DbManager
    {

        #region DECLARATIONS

        /// <summary>
        /// Current DB provider factory.
        /// </summary>
        private DbProviderFactory _dbProviderFactory;

        /// <summary>
        /// Connection string that is set/checked by corresponding column.
        /// </summary>
        private static string _connectionString;
        /// <summary>
        /// Invariant provider name that is set/checked by corresponding column.
        /// </summary>
        private static string _providerDefinition;
        /// <summary>
        /// Provider specific SQL code for "select last insert id" that is set/checked by corresponding column.
        /// </summary>
        private static string _selectLastInsertIdCommandText;

        /// <summary>
        /// Singleton.
        /// </summary>
        private static DbManager _singleton;
        /// <summary>
        /// Lock object for singleton initialization.
        /// </summary>
        private static readonly object _oSingletonInit = new object();

        /// <summary>
        /// Singleton acess, with singleton thread-safe initialization using dedicated lock object.
        /// </summary>
        public static DbManager Instance
        {
            get
            {
                lock (_oSingletonInit)
                {
                    return _singleton ?? (_singleton = new DbManager());
                }
            }
        }

        /// <summary>
        /// Standard SQL provider specific connection string.
        /// Getter thrws an exception if setter hasn't been called with a value.
        /// </summary>
        internal static string ConnectionString
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_connectionString))
                {
                    ConfigurationLoader._loggerTraceSource.TraceEvent(TraceEventType.Critical, 0, "Connection string not set!");
                    throw new Exception("ConnectionString column not initialized, please set a value!");
                }
                return _connectionString;
            }
            set { _connectionString = value; }
        }

        /// <summary>
        /// Provider specific SQL query select instruction (= command text), to execute "Select Last Insert Id".
        /// It's necessary to define it because there is no ADO.NET generic way of retrieving last insert ID after a SQL update execution.
        /// </summary>
        internal static string SelectLastInsertIdCommandText
        {
            get
            {
                if (_selectLastInsertIdCommandText == null)
                {
                    ConfigurationLoader._loggerTraceSource.TraceEvent(TraceEventType.Critical, 0, "Select Last Insert Id Command Text not set!");
                    throw new Exception("SelectLastInsertIdCommandText column not initialized, please set a value!");
                }
                return _selectLastInsertIdCommandText;
            }
            set { _selectLastInsertIdCommandText = value; }
        }

        /// <summary>
        /// Database provider definition. ADO.NET provider invariant name.
        /// </summary>
        internal static string ProviderName
        {
            get
            {
                if (_providerDefinition == null)
                {
                    ConfigurationLoader._loggerTraceSource.TraceEvent(TraceEventType.Critical, 0, "Database provider not set!");
                    throw new Exception("ProviderName column not initialized, please set a value!");
                }
                return _providerDefinition;
            }
            set { _providerDefinition = value; }
        }

        #endregion

        #region STRUCTURES

        /// <summary>
        /// Representation of an ADO.NET parameter. Used same way as an ADO.NET parameter but without depending on System.Data namespace in user code.
        /// It means more code overhead but is fine to deal with list of complex objects rather than list of values.
        /// </summary>
        public struct Parameter
        {
            /// <summary>
            /// 
            /// </summary>
            public string ParamName;

            /// <summary>
            /// 
            /// </summary>
            public object ParamValue;

            /// <summary>
            /// 
            /// </summary>
            public ParameterDirection ParamDirection;

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="name_">Name</param>
            /// <param name="value_">Value</param>
            /// <param name="direction_">ADO.NET parameter direction</param>
            public Parameter(string name_, object value_, ParameterDirection direction_)
            {
                ParamName = name_;
                ParamValue = value_;
                ParamDirection = direction_;
            }
            /// <summary>
            /// Constructor with default "in" direction.
            /// </summary>
            /// <param name="name_">Name</param>
            /// <param name="value_">Value</param>
            public Parameter(string name_, object value_)
            {
                ParamName = name_;
                ParamValue = value_;
                ParamDirection = ParameterDirection.Input;
            }
        }

        #endregion

        #region CONSTRUCTOR

        /// <summary>
        /// Constructor called by singleton initialization. Tries to instantiate provider from its name.
        /// </summary>
        private DbManager()
        {
            _dbProviderFactory = DbProviderFactories.GetFactory(ProviderName);
        }

        #endregion

        #region DESTRUCTOR

        /// <summary>
        /// Destructor. Sets internal static variables that could be linked to unmanaged resources to null.
        /// </summary>
        ~DbManager()
        {
            _dbProviderFactory = null;
        }

        #endregion

        #region CONNECTIONS

        /// <summary>
        /// If a connection doesn't exist, try to create it.
        /// If it exists but is closed, reopen it.
        /// May throw exceptions.	
        /// </summary>
        public DbConnection CreateConnection()
        {
            DbConnection dbConnection = _dbProviderFactory.CreateConnection();

            if (dbConnection == null)
                throw new Exception("DbHelper, CreateConnection: Connection could not be created");

            dbConnection.ConnectionString = ConnectionString;
            dbConnection.Open();
            return dbConnection;
        }

        #endregion

        #region TRANSACTION

        /// <summary>
        /// Opens a transaction and returns it.
        /// </summary>
        public DbTransaction OpenTransaction(DbConnection connection_)
        {
            try
            {
                return connection_.BeginTransaction();

            }
            catch (InvalidOperationException ex)
            {
                ConfigurationLoader._loggerTraceSource.TraceEvent(TraceEventType.Critical, 1, ex.ToString());
                throw new Exception("OpenTransaction - " + ex.Message);
            }
        }

        /// <summary>
        /// Commits and closes a transaction.
        /// </summary>
        /// <param name="transaction_">Transaction to manage</param>
        public void CommitTransaction(DbTransaction transaction_)
        {
            try
            {
                if (transaction_ == null) return;

                transaction_.Commit();
            }
            catch (InvalidOperationException ex)
            {
                ConfigurationLoader._loggerTraceSource.TraceEvent(TraceEventType.Critical, 1, ex.ToString());
                throw new Exception("CommitTransaction - " + ex.Message);
            }
        }

        /// <summary>
        /// Rollbacks and closes a transaction.
        /// </summary>
        /// <param name="transaction_">Transaction to manage</param>
        public void RollbackTransaction(DbTransaction transaction_)
        {
            try
            {
                if (transaction_ == null) return;

                transaction_.Rollback();
            }
            catch (InvalidOperationException ex)
            {
                ConfigurationLoader._loggerTraceSource.TraceEvent(TraceEventType.Critical, 1, ex.ToString());
                throw new Exception("@HandleTransaction - " + ex.Message);
            }
        }

        #endregion

        #region COMMANDS

        #region PARAMETERLESS METHODS

        /// <summary>
        /// Initializes a DbCommand object with parameters and returns it ready for execution.
        /// </summary>
        /// <param name="connection_">Current connection</param>
        /// <param name="transaction_">When not null, transaction to assign to _command. OpenTransaction() should have been called first</param>
        /// <param name="cmdType_">Type of command (Text, StoredProcedure, TableDirect)</param>
        /// <param name="cmdText_">SQL command text</param>
        private DbCommand PrepareCommand(DbConnection connection_, DbTransaction transaction_, string cmdText_, CommandType cmdType_ = CommandType.Text)
        {
            DbCommand command = _dbProviderFactory.CreateCommand();

            if (command == null)
            {
                throw new Exception("DbHelper, PrepareCommand: Command could not be created");
            }

            command.Connection = connection_;
            command.CommandText = cmdText_;
            command.CommandType = cmdType_;

            if (transaction_ != null)
                command.Transaction = transaction_;

            return command;
        }

        #endregion

        #region OBJECT BASED PARAMETER ARRAY

        /// <summary>
        /// Initializes a DbCommand object with parameters and returns it ready for execution.
        /// </summary>
        /// <param name="connection_">Current connection</param>
        /// <param name="transaction_">When not null, transaction to assign to _command. OpenTransaction() should have been called first</param>
        /// <param name="cmdType_">Type of command (Text, StoredProcedure, TableDirect)</param>
        /// <param name="cmdText_">SQL command text</param>
        /// <param name="cmdParms_">ADO.NET parameters (name and value) in multiple array format</param>
        private DbCommand PrepareCommand(DbConnection connection_, DbTransaction transaction_, string cmdText_, object[,] cmdParms_, CommandType cmdType_ = CommandType.Text)
        {
            DbCommand command = PrepareCommand(connection_, transaction_, cmdText_, cmdType_);

            if (cmdParms_ != null)
                CreateDbParameters(command, cmdParms_);

            return command;
        }

        #endregion

        #region STRUCTURE BASED PARAMETER ARRAY

        /// <summary>
        /// Initializes a DbCommand object with parameters and returns it ready for execution.
        /// </summary>
        /// <param name="connection_">Current connection</param>
        /// <param name="transaction_">When not null, transaction to assign to _command. OpenTransaction() should have been called first</param>
        /// <param name="cmdType_">Type of command (Text, StoredProcedure, TableDirect)</param>
        /// <param name="cmdText_">SQL command text</param>
        /// <param name="cmdParms_">ADO.NET parameters (name and value) as enumerable Parameter objects format</param>
        private DbCommand PrepareCommand(DbConnection connection_, DbTransaction transaction_, string cmdText_, IEnumerable<Parameter> cmdParms_, CommandType cmdType_ = CommandType.Text)
        {
            DbCommand command = PrepareCommand(connection_, transaction_, cmdText_, cmdType_);

            if (cmdParms_ != null)
                CreateDbParameters(command, cmdParms_);
            return command;
        }

        #endregion

        #region KEY VALUE PAIR BASED PARAMETER ARRAY

        /// <summary>
        /// Initializes a DbCommand object with parameters and returns it ready for execution.
        /// </summary>
        /// <param name="connection_">Current connection</param>
        /// <param name="transaction_">When not null, transaction to assign to _command. OpenTransaction() should have been called first</param>
        /// <param name="cmdType_">Type of command (Text, StoredProcedure, TableDirect)</param>
        /// <param name="cmdText_">SQL command text</param>
        /// <param name="cmdParms_">ADO.NET parameters (name and value) in list of key/value pair format</param>
        private DbCommand PrepareCommand(DbConnection connection_, DbTransaction transaction_, string cmdText_, List<KeyValuePair<string, object>> cmdParms_, CommandType cmdType_ = CommandType.Text)
        {
            DbCommand command = PrepareCommand(connection_, transaction_, cmdText_, cmdType_);

            if (cmdParms_ != null)
                CreateDbParameters(command, cmdParms_);
            return command;
        }

        #endregion

        #endregion

        #region PARAMETER METHODS

        #region OBJECT BASED

        /// <summary>
        /// Adds ADO.NET parameters to parameter DbCommand.
        /// Parameters are all input parameters.
        /// </summary>
        /// <param name="command_">DbCommand to add parameters to</param>
        /// <param name="adoParams_">ADO.NET parameters (name and value) in multiple array format</param>
        private void CreateDbParameters(DbCommand command_, object[,] adoParams_)
        {
            for (int i = 0; i < adoParams_.Length / 2; i++)
            {
                DbParameter dbParameter = command_.CreateParameter();
                dbParameter.ParameterName = adoParams_[i, 0].ToString();
                dbParameter.Value = adoParams_[i, 1];
                dbParameter.Direction = ParameterDirection.Input;
                command_.Parameters.Add(dbParameter);
            }
        }

        #endregion

        #region STRUCTURE BASED

        /// <summary>
        /// Adds ADO.NET parameters to parameter DbCommand.
        /// Parameters can be input or output parameters.
        /// </summary>
        /// <param name="command_">DbCommand to add parameters to</param>
        /// <param name="adoParams_">ADO.NET parameters (name and value) as enumerable Parameter objects format</param>
        private void CreateDbParameters(DbCommand command_, IEnumerable<Parameter> adoParams_)
        {
            foreach (Parameter oParam in adoParams_)
            {
                DbParameter dbParameter = command_.CreateParameter();
                dbParameter.ParameterName = oParam.ParamName;
                dbParameter.Value = oParam.ParamValue;
                dbParameter.Direction = oParam.ParamDirection;
                command_.Parameters.Add(dbParameter);
            }
        }

        #endregion

        #region KeyValuePair based

        /// <summary>
        /// Adds ADO.NET parameters to parameter DbCommand.
        /// Parameters are all input parameters.
        /// </summary>
        /// <param name="command_">DbCommand to add parameters to</param>
        /// <param name="adoParams_">ADO.NET parameters (name and value) as enumerable Parameter objects format</param>
        private void CreateDbParameters(DbCommand command_, List<KeyValuePair<string, object>> adoParams_)
        {
            foreach (KeyValuePair<string, object> oParam in adoParams_)
            {
                DbParameter dbParameter = command_.CreateParameter();
                dbParameter.ParameterName = oParam.Key;
                dbParameter.Value = oParam.Value;
                dbParameter.Direction = ParameterDirection.Input;
                command_.Parameters.Add(dbParameter);
            }
        }

        #endregion

        #endregion

        #region EXECUTE METHODS

        #region PARAMETERLESS METHODS

        /// <summary>
        /// Executes an SQL statement which returns number of affected rows("non query command").
        /// </summary>
        /// <param name="lastInsertedRowId_">Last inserted row ID (long number)</param>
        /// <param name="cmdType_">Command type (Text, StoredProcedure, TableDirect)</param>
        /// <param name="cmdText_">SQL command text</param>
        /// <param name="transaction_">When not null, transaction to use</param>
        /// <returns>Number of affected rows</returns>
        public int ExecuteNonQuery(string cmdText_, out long lastInsertedRowId_, CommandType cmdType_ = CommandType.Text, DbTransaction transaction_ = null)
        {
            DbConnection dbConnection = null;
            try
            {
                // Utiliser la connexion de la transaction ou une nouvelle connexion
                dbConnection = transaction_ != null ? transaction_.Connection : CreateConnection();

                int iNbAffectedRows;
                using (DbCommand command = PrepareCommand(dbConnection, transaction_, cmdText_, cmdType_))
                {
                     iNbAffectedRows = command.ExecuteNonQuery();
                }
                using (DbCommand command = PrepareCommand(dbConnection, transaction_, SelectLastInsertIdCommandText))
                {
                        
                    object oValue = command.ExecuteScalar();
                    if(!Int64.TryParse(oValue.ToString(), out lastInsertedRowId_))
                        throw new Exception("Returned last insert ID value '" + oValue + "' could not be parsed to Long number");
                }
                return iNbAffectedRows;
            }
            catch (Exception ex)
            {
                ConfigurationLoader._loggerTraceSource.TraceEvent(TraceEventType.Critical, 1, ex.ToString());
                throw;
            }
            finally
            {
                if (transaction_ == null && dbConnection != null)
                {
                    // La connexion n'était pas celle de la transaction
                    dbConnection.Close();
                    dbConnection.Dispose();
                }

            }

        }


        #endregion

        #region OBJECT BASED PARAMETER ARRAY

        /// <summary>
        /// Executes an SQL statement which returns number of  affected rows("non query command").
        /// </summary>
        /// <param name="lastInsertedRowId_">Last inserted row ID (long number)</param>
        /// <param name="cmdType_">Command type (Text, StoredProcedure, TableDirect)</param>
        /// <param name="cmdText_">SQL command text</param>
        /// <param name="cmdParms_">ADO.NET parameters (name and value) in multiple array format</param>
        /// <param name="transaction_">When not null, transaction to use</param>
        /// <returns>Number of affected rows</returns>
        public int ExecuteNonQuery(string cmdText_, object[,] cmdParms_, out long lastInsertedRowId_, CommandType cmdType_ = CommandType.Text, DbTransaction transaction_ = null)
        {
            DbConnection dbConnection = null;
            try
            {
                // Utiliser la connexion de la transaction ou une nouvelle connexion
                dbConnection = transaction_ != null ? transaction_.Connection : CreateConnection();

                int iNbAffectedRows;
                using (DbCommand command = PrepareCommand(dbConnection, transaction_, cmdText_, cmdParms_, cmdType_))
                {
                    iNbAffectedRows = command.ExecuteNonQuery();
                }
                using (DbCommand command = PrepareCommand(dbConnection, transaction_, SelectLastInsertIdCommandText))
                {

                    object oValue = command.ExecuteScalar();
                    if (!Int64.TryParse(oValue.ToString(), out lastInsertedRowId_))
                        throw new Exception("Returned last insert ID value '" + oValue + "' could not be parsed to Long number");
                }
                return iNbAffectedRows;
            }
            catch (Exception ex)
            {
                ConfigurationLoader._loggerTraceSource.TraceEvent(TraceEventType.Critical, 1, ex.ToString());
                throw;
            }
            finally
            {
                if (transaction_ == null && dbConnection != null)
                {
                    // La connexion n'était pas celle de la transaction
                    dbConnection.Close();
                    dbConnection.Dispose();
                }

            }

        }


        #endregion

        #region STRUCTURE BASED PARAMETER ARRAY

        /// <summary>
        /// Executes an SQL statement which returns number of  affected rows("non query command").
        /// </summary>
        /// <param name="lastInsertedRowId_">Last inserted row ID (long number)</param>
        /// <param name="cmdType_">Command type (Text, StoredProcedure, TableDirect)</param>
        /// <param name="cmdText_">SQL command text</param>
        /// <param name="cmdParms_">ADO.NET parameters (name and value) in array of Parameter objects format</param>
        /// <param name="transaction_">When not null, transaction to use</param>
        /// <returns>Number of affected rows</returns>
        public int ExecuteNonQuery(string cmdText_, Parameter[] cmdParms_, out long lastInsertedRowId_, CommandType cmdType_ = CommandType.Text, DbTransaction transaction_ = null)
        {
            DbConnection dbConnection = null;
            try
            {
                // Utiliser la connexion de la transaction ou une nouvelle connexion
                dbConnection = transaction_ != null ? transaction_.Connection : CreateConnection();

                int iNbAffectedRows;
                using (DbCommand command = PrepareCommand(dbConnection, transaction_, cmdText_, cmdParms_, cmdType_))
                {
                    iNbAffectedRows = command.ExecuteNonQuery();
                }
                using (DbCommand command = PrepareCommand(dbConnection, transaction_, SelectLastInsertIdCommandText))
                {

                    object oValue = command.ExecuteScalar();
                    if (!Int64.TryParse(oValue.ToString(), out lastInsertedRowId_))
                        throw new Exception("Returned last insert ID value '" + oValue + "' could not be parsed to Long number");
                }
                return iNbAffectedRows;
            }
            catch (Exception ex)
            {
                ConfigurationLoader._loggerTraceSource.TraceEvent(TraceEventType.Critical, 1, ex.ToString());
                throw;
            }
            finally
            {
                if (transaction_ == null && dbConnection != null)
                {
                    // La connexion n'était pas celle de la transaction
                    dbConnection.Close();
                    dbConnection.Dispose();
                }

            }
        }

        #endregion

        #region KEY VALUE PAIR BASED PARAMETER ARRAY

        /// <summary>
        /// Executes an SQL statement which returns number of  affected rows("non query command").
        /// </summary>
        /// <param name="lastInsertedRowId_">Last inserted row ID (long number)</param>
        /// <param name="cmdType_">Command type (Text, StoredProcedure, TableDirect)</param>
        /// <param name="cmdText_">SQL command text</param>
        /// <param name="cmdParms_">ADO.NET parameters (name and value) in list of key/value pair format</param>
        /// <param name="transaction_">When not null, transaction to use</param>
        /// <returns>Number of affected rows</returns>
        public int ExecuteNonQuery(string cmdText_, List<KeyValuePair<string, object>> cmdParms_, out long lastInsertedRowId_, CommandType cmdType_ = CommandType.Text, DbTransaction transaction_ = null)
        {
            using (DbConnection dbConnection = CreateConnection())
            {
                int iNbAffectedRows;
                using (DbCommand command = PrepareCommand(dbConnection, transaction_, cmdText_, cmdParms_, cmdType_))
                {
                    iNbAffectedRows = command.ExecuteNonQuery();
                }
                using (DbCommand command = PrepareCommand(dbConnection, transaction_, SelectLastInsertIdCommandText))
                {

                    object oValue = command.ExecuteScalar();
                    if (!Int64.TryParse(oValue.ToString(), out lastInsertedRowId_))
                        throw new Exception("Returned last insert ID value '" + oValue + "' could not be parsed to Long number");
                }
                return iNbAffectedRows;
            }
        }

        #endregion


        #endregion

        #region READER METHODS

        #region PARAMETERLESS METHODS

        /// <summary>
        /// Executes a SQL select operation
        /// </summary>
        /// <param name="cmdType_">SQL command type (Text, StoredProcedure, TableDirect)</param>
        /// <param name="cmdText_">SQL command text</param>
        /// <returns>ADO .NET data reader</returns>
        public DbDataReader ExecuteReader(string cmdText_, CommandType cmdType_ = CommandType.Text)
        {
            // Ne pas mettre dans un using la connexion sinon elle sera diposée avant d'avoir lu le data reader
            DbConnection dbConnection = CreateConnection();
            using (DbCommand command = PrepareCommand(dbConnection, null, cmdText_, cmdType_))
                try
                {
                    DbDataReader dr = command.ExecuteReader(CommandBehavior.CloseConnection);
                    return dr;
                }
                catch (Exception ex)
                {
                    ConfigurationLoader._loggerTraceSource.TraceEvent(TraceEventType.Critical, 1, ex + " Command was: " + cmdText_);
                    throw;
                }
        }

        #endregion

        #region OBJECT BASED PARAMETER ARRAY

        /// <summary>
        /// Executes a SQL select operation
        /// </summary>
        /// <param name="cmdType_">SQL command type (Text, StoredProcedure, TableDirect)</param>
        /// <param name="cmdText_">SQL command text</param>
        /// <param name="cmdParms_">ADO.NET parameters (name and value) in multiple array format</param>
        /// <returns>ADO .NET data reader</returns>
        public DbDataReader ExecuteReader(string cmdText_, object[,] cmdParms_, CommandType cmdType_ = CommandType.Text)
        {
            // Ne pas mettre dans un using la connexion sinon elle sera diposée avant d'avoir lu le data reader
            DbConnection dbConnection = CreateConnection();
            using (DbCommand command = PrepareCommand(dbConnection, null, cmdText_, cmdParms_, cmdType_))
                try
                {
                    DbDataReader dr = command.ExecuteReader(CommandBehavior.CloseConnection);
                    return dr;
                }
                catch (Exception ex)
                {
                    ConfigurationLoader._loggerTraceSource.TraceEvent(TraceEventType.Critical, 1, ex + " Command was: " + cmdText_ + ", params count: " + command.Parameters.Count);
                    throw;
                }
        }

        #endregion

        #region STRUCTURE BASED PARAMETER ARRAY

        /// <summary>
        /// Executes a SQL select operation
        /// </summary>
        /// <param name="cmdType_">SQL command type (Text, StoredProcedure, TableDirect)</param>
        /// <param name="cmdText_">SQL command text</param>
        /// <param name="cmdParms_">ADO.NET parameters (name and value) in array of Parameter objects format</param>
        /// <returns>ADO .NET data reader</returns>
        public DbDataReader ExecuteReader(string cmdText_, Parameter[] cmdParms_, CommandType cmdType_ = CommandType.Text)
        {
            // Ne pas mettre dans un using la connexion sinon elle sera diposée avant d'avoir lu le data reader
            DbConnection dbConnection = CreateConnection();
            using (DbCommand command = PrepareCommand(dbConnection, null, cmdText_, cmdParms_, cmdType_))
            {
                try
                {
                    return command.ExecuteReader(CommandBehavior.CloseConnection);
                }
                catch (Exception ex)
                {
                    ConfigurationLoader._loggerTraceSource.TraceEvent(TraceEventType.Critical, 1, ex + " Command was: " + cmdText_ + ", params count: " + command.Parameters.Count);
                    throw;
                }
            }
        }

        /// <summary>
        /// Executes a SQL select operation
        /// </summary>
        /// <param name="cmdType_">SQL command type (Text, StoredProcedure, TableDirect)</param>
        /// <param name="cmdText_">SQL command text</param>
        /// <param name="cmdParms_">ADO.NET parameters (name and value) formatted as a list of key/value</param>
        /// <returns>ADO .NET data reader</returns>
        public DbDataReader ExecuteReader(string cmdText_, List<KeyValuePair<string, object>> cmdParms_, CommandType cmdType_ = CommandType.Text)
        {
            // Ne pas mettre dans un using la connexion sinon elle sera diposée avant d'avoir lu le data reader
            DbConnection dbConnection = CreateConnection();
            using (DbCommand command = PrepareCommand(dbConnection, null, cmdText_, cmdParms_, cmdType_))
                try
                {
                    return command.ExecuteReader(CommandBehavior.CloseConnection);
                }
                catch (Exception ex)
                {
                    ConfigurationLoader._loggerTraceSource.TraceEvent(TraceEventType.Critical, 1, ex + " Command was: " + cmdText_ + ", params count: " + command.Parameters.Count);
                    throw;
                }
        }

        #endregion

        #endregion

        #region ADAPTER METHODS

        #region PARAMETERLESS METHODS

        /// <summary>
        /// Executes a SQL select operation
        /// </summary>
        /// <param name="cmdType_">SQL command type (Text, StoredProcedure, TableDirect)</param>
        /// <param name="cmdText_">SQL command text</param>
        /// <returns>ADO .NET dataset</returns>
        public DataSet DataAdapter(string cmdText_, CommandType cmdType_ = CommandType.Text)
        {

            using (DbConnection dbConnection = CreateConnection())
            {
                using (DbCommand command = PrepareCommand(dbConnection, null, cmdText_, cmdType_))
                    try
                    {

                        DbDataAdapter dda = _dbProviderFactory.CreateDataAdapter();

                        if (dda == null)
                        {
                            throw new Exception("DbHelper, DataAdapter: data adapter could not be created");
                        }

                        dda.SelectCommand = command;
                        DataSet ds = new DataSet();
                        dda.Fill(ds);
                        return ds;
                    }
                    catch (Exception ex)
                    {
                        ConfigurationLoader._loggerTraceSource.TraceEvent(TraceEventType.Critical, 1, ex + " Command was: " + cmdText_);
                        throw;
                    }

            }
        }

        #endregion

        #region OBJECT BASED PARAMETER ARRAY

        /// <summary>
        /// Executes a SQL select operation
        /// </summary>
        /// <param name="cmdType_">SQL command type (Text, StoredProcedure, TableDirect)</param>
        /// <param name="cmdText_">SQL command text</param>
        /// <param name="cmdParms_">ADO.NET parameters (name and value) in multiple object array format</param>
        /// <returns>ADO .NET dataset</returns>
        public DataSet DataAdapter(string cmdText_, object[,] cmdParms_, CommandType cmdType_ = CommandType.Text)
        {

            using (DbConnection dbConnection = CreateConnection())
            {
                using (DbCommand command = PrepareCommand(dbConnection, null, cmdText_, cmdParms_, cmdType_))
                    try
                    {

                        DbDataAdapter dda = _dbProviderFactory.CreateDataAdapter();

                        if (dda == null)
                        {
                            throw new Exception("DbHelper, DataAdapter: data adapter could not be created");
                        }
                        dda.SelectCommand = command;
                        DataSet ds = new DataSet();
                        dda.Fill(ds);
                        return ds;
                    }
                    catch (Exception ex)
                    {
                        ConfigurationLoader._loggerTraceSource.TraceEvent(TraceEventType.Critical, 1, ex + " Command was: " + cmdText_ + ", params count: " + command.Parameters.Count);
                        throw;
                    }
            }
        }

        #endregion

        #region STRUCTURE BASED PARAMETER ARRAY

        /// <summary>
        /// Executes a SQL select operation
        /// </summary>
        /// <param name="cmdType_">SQL command type (Text, StoredProcedure, TableDirect)</param>
        /// <param name="cmdText_">SQL command text</param>
        /// <param name="cmdParms_">ADO.NET parameters (name and value) in array of Parameter objects format</param>
        /// <returns>ADO .NET dataset</returns>
        public DataSet DataAdapter(string cmdText_, Parameter[] cmdParms_, CommandType cmdType_ = CommandType.Text)
        {

            using (DbConnection dbConnection = CreateConnection())
            {
                using (DbCommand command = PrepareCommand(dbConnection, null, cmdText_, cmdParms_, cmdType_))
                    try
                    {

                        DbDataAdapter dda = _dbProviderFactory.CreateDataAdapter();

                        if (dda == null)
                        {
                            throw new Exception("DbHelper, DataAdapter: data adapter could not be created");
                        }
                        dda.SelectCommand = command;
                        DataSet ds = new DataSet();
                        dda.Fill(ds);
                        return ds;
                    }
                    catch (Exception ex)
                    {
                        ConfigurationLoader._loggerTraceSource.TraceEvent(TraceEventType.Critical, 1, ex + " Command was: " + cmdText_ + ", params count: " + command.Parameters.Count);
                        throw;
                    }
            }
        }

        #endregion

        #endregion

        #region SCALAR METHODS

        #region PARAMETERLESS METHODS

        /// <summary>
        /// Executes a SQL operation and returns value of first column and first line of data table result.
        /// Generally used for a query such as "count()".
        /// </summary>
        /// <param name="cmdType_">SQL command type (Text, StoredProcedure, TableDirect)</param>
        /// <param name="cmdText_">SQL command text</param>
        /// <returns>data value</returns>
        public object ExecuteScalar(string cmdText_, CommandType cmdType_ = CommandType.Text)
        {
            using (DbConnection dbConnection = CreateConnection())
            {
                using (DbCommand command = PrepareCommand(dbConnection, null, cmdText_, cmdType_))
                    try
                    {
                        return command.ExecuteScalar();
                    }
                    catch (Exception ex)
                    {
                        ConfigurationLoader._loggerTraceSource.TraceEvent(TraceEventType.Critical, 1, ex+ " Command was: " + cmdText_);
                        throw;
                    }
            }
        }

        #endregion

        #region OBJECT BASED PARAMETER ARRAY

        /// <summary>
        /// Executes a SQL operation and returns value of first column and first line of data table result.
        /// Generally used for a query such as "count()".
        /// </summary>
        /// <param name="cmdType_">SQL command type (Text, StoredProcedure, TableDirect)</param>
        /// <param name="cmdText_">SQL command text</param>
        /// <param name="cmdParms_">ADO.NET parameters (name and value) in multiple object array format</param>
        /// <returns>data value</returns>
        public object ExecuteScalar(string cmdText_, object[,] cmdParms_, CommandType cmdType_ = CommandType.Text)
        {
            using (DbConnection dbConnection = CreateConnection())
            {
                using (DbCommand command = PrepareCommand(dbConnection, null, cmdText_, cmdParms_, cmdType_))
                    try
                    {
                        return command.ExecuteScalar();

                    }
                    catch (Exception ex)
                    {
                        ConfigurationLoader._loggerTraceSource.TraceEvent(TraceEventType.Critical, 1, ex + " Command was: " + cmdText_ + ", params count: " + command.Parameters.Count);
                        throw;
                    }
            }
        }

        /// <summary>
        /// Executes a SQL operation and returns value of first column and first line of data table result.
        /// Generally used for a query such as "count()".
        /// </summary>
        /// <param name="cmdType_">SQL command type (Text, StoredProcedure, TableDirect)</param>
        /// <param name="cmdText_">SQL command text</param>
        /// <param name="cmdParms_">ADO.NET parameters (name and value) in multiple object array format</param>
        /// <param name="blTransaction_">When true, query will be executed using current transaction. OpenTransaction() should have been called first</param>
        /// <returns>data value</returns>
        public object ExecuteScalar(bool blTransaction_, string cmdText_, object[,] cmdParms_, CommandType cmdType_ = CommandType.Text)
        {
            using (DbConnection dbConnection = CreateConnection())
            {
                using (DbCommand command = PrepareCommand(dbConnection, null, cmdText_, cmdParms_, cmdType_))
                    try
                    {
                        return command.ExecuteScalar();

                    }
                    catch (Exception ex)
                    {
                        ConfigurationLoader._loggerTraceSource.TraceEvent(TraceEventType.Critical, 1, ex + " Command was: " + cmdText_ + ", params count: " + command.Parameters.Count);
                        throw;
                    }
            }
        }


        #endregion

        #region STRUCTURE BASED PARAMETER ARRAY

        /// <summary>
        /// Executes a SQL operation and returns value of first column and first line of data table result.
        /// Generally used for a query such as "count()".
        /// </summary>
        /// <param name="cmdType_">SQL command type (Text, StoredProcedure, TableDirect)</param>
        /// <param name="cmdText_">SQL command text</param>
        /// <param name="cmdParms_">ADO.NET parameters (name and value) in array of Parameter objects format</param>
        /// <returns>data value</returns>
        public object ExecuteScalar(string cmdText_, Parameter[] cmdParms_, CommandType cmdType_ = CommandType.Text)
        {
            using (DbConnection dbConnection = CreateConnection())
            {
                using (DbCommand command = PrepareCommand(dbConnection, null, cmdText_, cmdParms_, cmdType_))
                    try
                    {
                        return command.ExecuteScalar();

                    }
                    catch (Exception ex)
                    {
                        ConfigurationLoader._loggerTraceSource.TraceEvent(TraceEventType.Critical, 1, ex + " Command was: " + cmdText_ + ", params count: " + command.Parameters.Count);
                        throw;
                    }
            }
        }

        /// <summary>
        /// Executes a SQL operation and returns value of first column and first line of data table result.
        /// Generally used for a query such as "count()".
        /// </summary>
        /// <param name="cmdType_">SQL command type (Text, StoredProcedure, TableDirect)</param>
        /// <param name="cmdText_">SQL command text</param>
        /// <param name="cmdParms_">ADO.NET parameters (name and value) in array of Parameter objects format</param>
        /// <param name="blTransaction_">When true, query will be executed using current transaction. OpenTransaction() should have been called first</param>
        /// <returns>data value</returns>
        public object ExecuteScalar(bool blTransaction_, string cmdText_, Parameter[] cmdParms_, CommandType cmdType_ = CommandType.Text)
        {
            using (DbConnection dbConnection = CreateConnection())
            {
                using (DbCommand command = PrepareCommand(dbConnection, null, cmdText_, cmdParms_, cmdType_))
                    try
                    {
                        return command.ExecuteScalar();

                    }
                    catch (Exception ex)
                    {
                        ConfigurationLoader._loggerTraceSource.TraceEvent(TraceEventType.Critical, 1, ex + " Command was: " + cmdText_ + ", params count: " + command.Parameters.Count);
                        throw;
                    }
            }
        }

        #endregion

        #endregion

    }
}


