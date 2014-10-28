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
using OsamesMicroOrm.Logging;

namespace OsamesMicroOrm
{
    /// <summary>
    /// Generic ADO.NET level, multi thread class, that deals with database providers and database query execution.
    /// </summary>
    public sealed class DbManager
    {

        #region DECLARATIONS

        /// <summary>
        /// Current DB provider factory.
        /// </summary>
        internal DbProviderFactory DbProviderFactory;

        /// <summary>
        /// First created connection, to be used when pool is exhausted when pooling is active.
        /// </summary>
        private DbConnection BackupConnection;

        /// <summary>
        /// Connection string that is set/checked by ConnectionString property.
        /// </summary>
        private static string ConnectionStringField;
        /// <summary>
        /// Invariant provider name that is set/checked by ProviderName property.
        /// </summary>
        private static string ProviderInvariantName;
        /// <summary>
        /// Provider specific SQL code for "select last insert id" that is set/checked by SelectLastInsertIdCommandText property.
        /// </summary>
        private static string SelectLastInsertIdCommandTextField;

        /// <summary>
        /// Singleton.
        /// </summary>
        private static DbManager Singleton;
        /// <summary>
        /// Lock object for singleton initialization.
        /// </summary>
        private static readonly object SingletonInitLockObject = new object();

        /// <summary>
        /// Lock object for using backup connection.
        /// </summary>
        private static readonly object BackupConnectionUsageLockObject = new object();

        /// <summary>
        /// Singleton access, with singleton thread-safe initialization using dedicated lock object.
        /// </summary>
        public static DbManager Instance
        {
            get
            {
                lock (SingletonInitLockObject)
                {
                    return Singleton ?? (Singleton = new DbManager());
                }
            }
        }

        /// <summary>
        /// Standard SQL provider specific connection string.
        /// Getter throws an exception if setter hasn't been called with a value.
        /// </summary>
        internal static string ConnectionString
        {
            get
            {
                if (string.IsNullOrWhiteSpace(ConnectionStringField))
                {
                    Logger.Log(TraceEventType.Critical, "Connection string not set!");
                    throw new Exception("ConnectionString column not initialized, please set a value!");
                }
                return ConnectionStringField;
            }
            set { ConnectionStringField = value; }
        }

        /// <summary>
        /// Provider specific SQL query select instruction (= command text), to execute "Select Last Insert Id".
        /// It's necessary to define it because there is no ADO.NET generic way of retrieving last insert ID after a SQL update execution.
        /// </summary>
        internal static string SelectLastInsertIdCommandText
        {
            get
            {
                if (SelectLastInsertIdCommandTextField == null)
                {
                    Logger.Log(TraceEventType.Critical, "Select Last Insert Id Command Text not set!");
                    throw new Exception("SelectLastInsertIdCommandText column not initialized, please set a value!");
                }
                return SelectLastInsertIdCommandTextField;
            }
            set { SelectLastInsertIdCommandTextField = value; }
        }

        /// <summary>
        /// Database provider definition. ADO.NET provider invariant name.
        /// </summary>
        internal static string ProviderName
        {
            get
            {
                if (ProviderInvariantName == null)
                {
                    Logger.Log(TraceEventType.Critical, "Database provider not set!");
                    throw new Exception("ProviderName column not initialized, please set a value!");
                }
                return ProviderInvariantName;
            }
            set { ProviderInvariantName = value; }
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
            DbProviderFactory = DbProviderFactories.GetFactory(ProviderName);
        }

        #endregion

        #region DESTRUCTOR

        /// <summary>
        /// Destructor. Sets internal static variables that could be linked to unmanaged resources to null.
        /// </summary>
        ~DbManager()
        {
            if(BackupConnection !=  null)
                BackupConnection.Close();
            DbProviderFactory = null;
        }

        #endregion

        #region CONNECTIONS

        /// <summary>
        /// Try to get a new connection, usually from pool (may get backup connection in this case) or single connection.
        /// Opens the connection before returning it.
        /// May throw exception only when no connection at all can be opened.	
        /// </summary>
        public DbConnection CreateConnection()
        {
            try
            {
                System.Data.Common.DbConnection adoConnection = DbProviderFactory.CreateConnection();
                // ReSharper disable PossibleNullReferenceException
                adoConnection.ConnectionString = ConnectionString;
                // ReSharper restore PossibleNullReferenceException
                adoConnection.Open();
                // everything OK
                if (BackupConnection == null)
                {
                    // we just opened our first connection!
                    // Keep a reference to it and keep this backup connexion unused for now
                    BackupConnection = new DbConnection(adoConnection, true);
                    // Try to get a second connection and return it
                    return CreateConnection();
                }
                // Not the first connection
                DbConnection pooledConnection = new DbConnection(adoConnection, false);
                return pooledConnection;

            }
            catch (Exception ex)
            {
                // could not get a new connection !
                if (BackupConnection == null)
                {
                    // could not get any connection
                    Logger.Log(TraceEventType.Critical, ex);
                    throw new Exception("DbManager, CreateConnection: Connection could not be created! *** " + ex.Message + " *** . Look at detailed log for details");
                }
                // could not get a second connection
                // use backup connection
                // We may have to reopen it because we waited a long time before using it
                if (BackupConnection.State != ConnectionState.Open)
                {
                    BackupConnection.ConnectionString = ConnectionString;
                    BackupConnection.Open();
                }

                return BackupConnection;
            }

        }
        /// <summary>
        /// Fermeture d'une connexion et dispose/mise à null de l'objet.
        /// </summary>
        /// <param name="connexion_">connexion</param>
        /// <returns>Ne renvoie rien</returns>
        public void DisposeConnection(ref DbConnection connexion_)
        {
            if (connexion_ == null) return;

            connexion_.Close();
            connexion_ = null;
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
                Logger.Log(TraceEventType.Critical, ex.ToString());
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
                Logger.Log(TraceEventType.Critical, ex.ToString());
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
                Logger.Log(TraceEventType.Critical, ex.ToString());
                throw new Exception("@HandleTransaction - " + ex.Message);
            }
        }

        #endregion

        #region COMMANDS

        #region PrepareCommand

        /// <summary>
        /// Initializes a DbCommand object with parameters and returns it ready for execution.
        /// </summary>
        /// <param name="connection_">Current connection</param>
        /// <param name="transaction_">When not null, transaction to assign to _command. OpenTransaction() should have been called first</param>
        /// <param name="cmdType_">Type of command (Text, StoredProcedure, TableDirect)</param>
        /// <param name="cmdText_">SQL command text</param>
        /// <param name="cmdParams_">ADO.NET parameters (name and value) as a two-dimensional array</param>
        private DbCommand PrepareCommand(DbConnection connection_, DbTransaction transaction_, string cmdText_, object[,] cmdParams_, CommandType cmdType_ = CommandType.Text)
        {
            DbCommand command = PrepareCommandWithoutParameter(connection_, transaction_, cmdText_, cmdType_);

            if (cmdParams_ != null)
                CreateDbParameters(command, cmdParams_);

            return command;
        }

        /// <summary>
        /// Initializes a DbCommand object with parameters and returns it ready for execution.
        /// </summary>
        /// <param name="connection_">Current connection</param>
        /// <param name="transaction_">When not null, transaction to assign to _command. OpenTransaction() should have been called first</param>
        /// <param name="cmdType_">Type of command (Text, StoredProcedure, TableDirect)</param>
        /// <param name="cmdText_">SQL command text</param>
        /// <param name="cmdParams_">ADO.NET parameters (name and value) as an array of Parameter structures</param>
        private DbCommand PrepareCommand(DbConnection connection_, DbTransaction transaction_, string cmdText_, IEnumerable<Parameter> cmdParams_, CommandType cmdType_ = CommandType.Text)
        {
            DbCommand command = PrepareCommandWithoutParameter(connection_, transaction_, cmdText_, cmdType_);

            if (cmdParams_ != null)
                CreateDbParameters(command, cmdParams_);

            return command;
        }

        /// <summary>
        /// Initializes a DbCommand object with parameters and returns it ready for execution.
        /// </summary>
        /// <param name="connection_">Current connection</param>
        /// <param name="transaction_">When not null, transaction to assign to _command. OpenTransaction() should have been called first</param>
        /// <param name="cmdType_">Type of command (Text, StoredProcedure, TableDirect)</param>
        /// <param name="cmdText_">SQL command text</param>
        /// <param name="cmdParams_">ADO.NET parameters (name and value) as an a list of string and value key value pairs</param>
        private DbCommand PrepareCommand(DbConnection connection_, DbTransaction transaction_, string cmdText_, IEnumerable<KeyValuePair<string, object>> cmdParams_, CommandType cmdType_ = CommandType.Text)
        {
            DbCommand command = PrepareCommandWithoutParameter(connection_, transaction_, cmdText_, cmdType_);

            if (cmdParams_ != null)
                CreateDbParameters(command, cmdParams_);

            return command;
        }

        /// <summary>
        /// Initializes a DbCommand object without parameters and returns it ready for execution.
        /// </summary>
        /// <param name="connection_">Current connection</param>
        /// <param name="transaction_">When not null, transaction to assign to _command. OpenTransaction() should have been called first</param>
        /// <param name="cmdType_">Type of command (Text, StoredProcedure, TableDirect)</param>
        /// <param name="cmdText_">SQL command text</param>
        private DbCommand PrepareCommandWithoutParameter(DbConnection connection_, DbTransaction transaction_, string cmdText_, CommandType cmdType_ = CommandType.Text)
        {
            System.Data.Common.DbCommand adoCommand = DbProviderFactory.CreateCommand();

            if (adoCommand == null)
            {
                throw new Exception("DbHelper, PrepareCommand: Command could not be created");
            }

            DbCommand command = new DbCommand(adoCommand) { Connection = connection_, CommandText = cmdText_, CommandType = cmdType_ };

            if (transaction_ != null)
                command.Transaction = transaction_;

            return command;
        }

        #endregion
        #region CreateDbParameters

        /// <summary>
        /// Adds ADO.NET parameters to parameter DbCommand.
        /// Parameters are all input parameters.
        /// </summary>
        /// <param name="command_">DbCommand to add parameters to</param>
        /// <param name="adoParams_">ADO.NET parameters (name and value) in multiple array format</param>
        private static void CreateDbParameters(DbCommand command_, object[,] adoParams_)
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

        /// <summary>
        /// Adds ADO.NET parameters to parameter DbCommand.
        /// Parameters can be input or output parameters.
        /// </summary>
        /// <param name="command_">DbCommand to add parameters to</param>
        /// <param name="adoParams_">ADO.NET parameters (name and value) as enumerable Parameter objects format</param>
        private static void CreateDbParameters(DbCommand command_, IEnumerable<Parameter> adoParams_)
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

        /// <summary>
        /// Adds ADO.NET parameters to parameter DbCommand.
        /// Parameters are all input parameters.
        /// </summary>
        /// <param name="command_">DbCommand to add parameters to</param>
        /// <param name="adoParams_">ADO.NET parameters (name and value) as enumerable Parameter objects format</param>
        private static void CreateDbParameters(DbCommand command_, IEnumerable<KeyValuePair<string, object>> adoParams_)
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

        #region avec sortie d'un ID d'enregistrement (INSERT)

        /// <summary>
        /// Exécution de System.Data.Common.DbCommand.ExecuteNonQuery() puis ExecuteScalar() pour exécuter une requête de type INSERT et obtenir l'ID de la ligne insérée.
        /// </summary>
        /// <param name="connection_">Connexion (sans transaction)</param>
        /// <param name="cmdParams_">Une des implémentations retenues pour les paramètres ADO.NET : object[,] (peut être null)</param>
        /// <param name="lastInsertedRowId_">Sortie : ID du dernier enregistrement inséré</param>
        /// <param name="cmdType_">Type de la commande, par défaut CommandType.Text</param>
        /// <param name="cmdText_">Texte de la requête SQL</param>
        /// <returns>Nombre de lignes affectées</returns>
        public int ExecuteNonQuery(DbConnection connection_, CommandType cmdType_, string cmdText_, object[,] cmdParams_, out long lastInsertedRowId_)
        {
            int iNbAffectedRows;

            if (connection_.IsBackup)
            {
                lock (BackupConnectionUsageLockObject)
                {
                    // perform code with locking
                    using (DbCommand command = PrepareCommand(connection_, null, cmdText_, cmdParams_, cmdType_))
                    {
                        iNbAffectedRows = command.ExecuteNonQuery();
                    }

                    using (DbCommand command = PrepareCommand(connection_, null, SelectLastInsertIdCommandText, (object[,])null))
                    {
                        object oValue = command.ExecuteScalar();
                        if (!Int64.TryParse(oValue.ToString(), out lastInsertedRowId_))
                            throw new Exception("Returned last insert ID value '" + oValue + "' could not be parsed to Long number");
                    }
                }
            }
            else
            {
                // no lock
                using (DbCommand command = PrepareCommand(connection_, null, cmdText_, cmdParams_, cmdType_))
                {
                    iNbAffectedRows = command.ExecuteNonQuery();
                }

                using (DbCommand command = PrepareCommand(connection_, null, SelectLastInsertIdCommandText, (object[,])null))
                {
                    object oValue = command.ExecuteScalar();
                    if (!Int64.TryParse(oValue.ToString(), out lastInsertedRowId_))
                        throw new Exception("Returned last insert ID value '" + oValue + "' could not be parsed to Long number");
                }
            }
            return iNbAffectedRows;
        }

        /// <summary>
        /// Exécution de System.Data.Common.DbCommand.ExecuteNonQuery() puis ExecuteScalar() pour exécuter une requête de type INSERT et obtenir l'ID de la ligne insérée.
        /// </summary>
        /// <param name="transaction_">Transaction avec une connexion associée</param>
        /// <param name="cmdParams_">Une des implémentations retenues pour les paramètres ADO.NET : object[,] (peut être null)</param>
        /// <param name="lastInsertedRowId_">Sortie : ID du dernier enregistrement inséré</param>
        /// <param name="cmdType_">Type de la commande, par défaut CommandType.Text</param>
        /// <param name="cmdText_">Texte de la requête SQL</param>
        /// <returns>Nombre de lignes affectées</returns>
        public int ExecuteNonQuery(DbTransaction transaction_, CommandType cmdType_, string cmdText_, object[,] cmdParams_, out long lastInsertedRowId_)
        {
            int iNbAffectedRows;

            if (transaction_.Connection.IsBackup)
            {
                lock (BackupConnectionUsageLockObject)
                {
                    // perform code with locking
                    using (DbCommand command = PrepareCommand(transaction_.Connection, transaction_, cmdText_, cmdParams_, cmdType_))
                    {
                        iNbAffectedRows = command.ExecuteNonQuery();
                    }

                    using (DbCommand command = PrepareCommand(transaction_.Connection, transaction_, SelectLastInsertIdCommandText, (object[,])null))
                    {
                        object oValue = command.ExecuteScalar();
                        if (!Int64.TryParse(oValue.ToString(), out lastInsertedRowId_))
                            throw new Exception("Returned last insert ID value '" + oValue + "' could not be parsed to Long number");
                    }
                }
            }
            else
            {
                // no lock
                using (DbCommand command = PrepareCommand(transaction_.Connection, transaction_, cmdText_, cmdParams_, cmdType_))
                {
                    iNbAffectedRows = command.ExecuteNonQuery();
                }

                using (DbCommand command = PrepareCommand(transaction_.Connection, transaction_, SelectLastInsertIdCommandText, (object[,])null))
                {
                    object oValue = command.ExecuteScalar();
                    if (!Int64.TryParse(oValue.ToString(), out lastInsertedRowId_))
                        throw new Exception("Returned last insert ID value '" + oValue + "' could not be parsed to Long number");
                }
            }

            return iNbAffectedRows;
        }

        /// <summary>
        /// Exécution de System.Data.Common.DbCommand.ExecuteNonQuery() puis ExecuteScalar() pour exécuter une requête de type INSERT et obtenir l'ID de la ligne insérée.
        /// </summary>
        /// <param name="connection_">Connexion (sans transaction)</param>
        /// <param name="cmdParams_">Une des implémentations retenues pour les paramètres ADO.NET : IEnumerable&lt;Parameter&gt; (peut être null)</param>
        /// <param name="lastInsertedRowId_">Sortie : ID du dernier enregistrement inséré</param>
        /// <param name="cmdType_">Type de la commande, par défaut CommandType.Text</param>
        /// <param name="cmdText_">Texte de la requête SQL</param>
        /// <returns>Nombre de lignes affectées</returns>
        public int ExecuteNonQuery(DbConnection connection_, CommandType cmdType_, string cmdText_, IEnumerable<Parameter> cmdParams_, out long lastInsertedRowId_)
        {
            int iNbAffectedRows;

            if (connection_.IsBackup)
            {
                lock (BackupConnectionUsageLockObject)
                {
                    // perform code with locking
                    using (DbCommand command = PrepareCommand(connection_, null, cmdText_, cmdParams_, cmdType_))
                    {
                        iNbAffectedRows = command.ExecuteNonQuery();
                    }

                    using (DbCommand command = PrepareCommand(connection_, null, SelectLastInsertIdCommandText, (IEnumerable<Parameter>)null))
                    {
                        object oValue = command.ExecuteScalar();
                        if (!Int64.TryParse(oValue.ToString(), out lastInsertedRowId_))
                            throw new Exception("Returned last insert ID value '" + oValue + "' could not be parsed to Long number");
                    }
                }
            }
            else
            {
                // no lock
                using (DbCommand command = PrepareCommand(connection_, null, cmdText_, cmdParams_, cmdType_))
                {
                    iNbAffectedRows = command.ExecuteNonQuery();
                }

                using (DbCommand command = PrepareCommand(connection_, null, SelectLastInsertIdCommandText, (IEnumerable<Parameter>)null))
                {
                    object oValue = command.ExecuteScalar();
                    if (!Int64.TryParse(oValue.ToString(), out lastInsertedRowId_))
                        throw new Exception("Returned last insert ID value '" + oValue + "' could not be parsed to Long number");
                }
            }
            return iNbAffectedRows;
        }

        /// <summary>
        /// Exécution de System.Data.Common.DbCommand.ExecuteNonQuery() puis ExecuteScalar() pour exécuter une requête de type INSERT et obtenir l'ID de la ligne insérée.
        /// </summary>
        /// <param name="transaction_">Transaction avec sa connexion associée</param>
        /// <param name="cmdParams_">Une des implémentations retenues pour les paramètres ADO.NET : IEnumerable&lt;Parameter&gt; (peut être null)</param>
        /// <param name="lastInsertedRowId_">Sortie : ID du dernier enregistrement inséré</param>
        /// <param name="cmdType_">Type de la commande, par défaut CommandType.Text</param>
        /// <param name="cmdText_">Texte de la requête SQL</param>
        /// <returns>Nombre de lignes affectées</returns>
        public int ExecuteNonQuery(DbTransaction transaction_, CommandType cmdType_, string cmdText_, IEnumerable<Parameter> cmdParams_, out long lastInsertedRowId_)
        {
            int iNbAffectedRows;

            if (transaction_.Connection.IsBackup)
            {
                lock (BackupConnectionUsageLockObject)
                {
                    // perform code with locking
                    using (DbCommand command = PrepareCommand(transaction_.Connection, transaction_, cmdText_, cmdParams_, cmdType_))
                    {
                        iNbAffectedRows = command.ExecuteNonQuery();
                    }

                    using (DbCommand command = PrepareCommand(transaction_.Connection, transaction_, SelectLastInsertIdCommandText, (IEnumerable<Parameter>)null))
                    {
                        object oValue = command.ExecuteScalar();
                        if (!Int64.TryParse(oValue.ToString(), out lastInsertedRowId_))
                            throw new Exception("Returned last insert ID value '" + oValue + "' could not be parsed to Long number");
                    }
                }
            }
            else
            {
                // no lock
                using (DbCommand command = PrepareCommand(transaction_.Connection, transaction_, cmdText_, cmdParams_, cmdType_))
                {
                    iNbAffectedRows = command.ExecuteNonQuery();
                }

                using (DbCommand command = PrepareCommand(transaction_.Connection, transaction_, SelectLastInsertIdCommandText, (IEnumerable<Parameter>)null))
                {
                    object oValue = command.ExecuteScalar();
                    if (!Int64.TryParse(oValue.ToString(), out lastInsertedRowId_))
                        throw new Exception("Returned last insert ID value '" + oValue + "' could not be parsed to Long number");
                }
            }

            return iNbAffectedRows;
        }

        /// <summary>
        /// Exécution de System.Data.Common.DbCommand.ExecuteNonQuery() puis ExecuteScalar() pour exécuter une requête de type INSERT et obtenir l'ID de la ligne insérée.
        /// </summary>
        /// <param name="connection_">Connexion (sans transaction)</param>
        /// <param name="cmdParams_">Une des implémentations retenues pour les paramètres ADO.NET : IEnumerable&lt;KeyValuePair&lt;string, object&gt;&gt; (peut être null)</param>
        /// <param name="lastInsertedRowId_">Sortie : ID du dernier enregistrement inséré</param>
        /// <param name="cmdType_">Type de la commande, par défaut CommandType.Text</param>
        /// <param name="cmdText_">Texte de la requête SQL</param>
        /// <returns>Nombre de lignes affectées</returns>
        public int ExecuteNonQuery(DbConnection connection_, CommandType cmdType_, string cmdText_, IEnumerable<KeyValuePair<string, object>> cmdParams_, out long lastInsertedRowId_)
        {
            int iNbAffectedRows;

            if (connection_.IsBackup)
            {
                lock (BackupConnectionUsageLockObject)
                {
                    // perform code with locking
                    using (DbCommand command = PrepareCommand(connection_, null, cmdText_, cmdParams_, cmdType_))
                    {
                        iNbAffectedRows = command.ExecuteNonQuery();
                    }

                    using (DbCommand command = PrepareCommand(connection_, null, SelectLastInsertIdCommandText, (IEnumerable<KeyValuePair<string, object>>)null))
                    {
                        object oValue = command.ExecuteScalar();
                        if (!Int64.TryParse(oValue.ToString(), out lastInsertedRowId_))
                            throw new Exception("Returned last insert ID value '" + oValue + "' could not be parsed to Long number");
                    }
                }
            }
            else
            {
                // no lock
                using (DbCommand command = PrepareCommand(connection_, null, cmdText_, cmdParams_, cmdType_))
                {
                    iNbAffectedRows = command.ExecuteNonQuery();
                }

                using (DbCommand command = PrepareCommand(connection_, null, SelectLastInsertIdCommandText, (IEnumerable<KeyValuePair<string, object>>)null))
                {
                    object oValue = command.ExecuteScalar();
                    if (!Int64.TryParse(oValue.ToString(), out lastInsertedRowId_))
                        throw new Exception("Returned last insert ID value '" + oValue + "' could not be parsed to Long number");
                }
            }

            return iNbAffectedRows;
        }

        /// <summary>
        /// Exécution de System.Data.Common.DbCommand.ExecuteNonQuery() puis ExecuteScalar() pour exécuter une requête de type INSERT et obtenir l'ID de la ligne insérée.
        /// </summary>
        /// <param name="transaction_">Transaction avec connexion associée</param>
        /// <param name="cmdParams_">Une des implémentations retenues pour les paramètres ADO.NET : IEnumerable&lt;KeyValuePair&lt;string, object&gt;&gt; (peut être null)</param>
        /// <param name="lastInsertedRowId_">Sortie : ID du dernier enregistrement inséré</param>
        /// <param name="cmdType_">Type de la commande, par défaut CommandType.Text</param>
        /// <param name="cmdText_">Texte de la requête SQL</param>
        /// <returns>Nombre de lignes affectées</returns>
        public int ExecuteNonQuery(DbTransaction transaction_, CommandType cmdType_, string cmdText_, IEnumerable<KeyValuePair<string, object>> cmdParams_, out long lastInsertedRowId_)
        {
            int iNbAffectedRows;

            if (transaction_.Connection.IsBackup)
            {
                lock (BackupConnectionUsageLockObject)
                {
                    // perform code with locking
                    using (DbCommand command = PrepareCommand(transaction_.Connection, transaction_, cmdText_, cmdParams_, cmdType_))
                    {
                        iNbAffectedRows = command.ExecuteNonQuery();
                    }

                    using (DbCommand command = PrepareCommand(transaction_.Connection, transaction_, SelectLastInsertIdCommandText, (IEnumerable<KeyValuePair<string, object>>)null))
                    {
                        object oValue = command.ExecuteScalar();
                        if (!Int64.TryParse(oValue.ToString(), out lastInsertedRowId_))
                            throw new Exception("Returned last insert ID value '" + oValue + "' could not be parsed to Long number");
                    }
                }
            }
            else
            {
                // no lock
                using (DbCommand command = PrepareCommand(transaction_.Connection, transaction_, cmdText_, cmdParams_, cmdType_))
                {
                    iNbAffectedRows = command.ExecuteNonQuery();
                }

                using (DbCommand command = PrepareCommand(transaction_.Connection, transaction_, SelectLastInsertIdCommandText, (IEnumerable<KeyValuePair<string, object>>)null))
                {
                    object oValue = command.ExecuteScalar();
                    if (!Int64.TryParse(oValue.ToString(), out lastInsertedRowId_))
                        throw new Exception("Returned last insert ID value '" + oValue + "' could not be parsed to Long number");
                }
            }

            return iNbAffectedRows;
        }

        #endregion

        #region sans sortie d'ID d'enregistrement (UPDATE)

        /// <summary>
        /// Exécution de System.Data.Common.DbCommand.ExecuteNonQuery() pour exécuter une requête de type UPDATE.
        /// </summary>
        /// <param name="connection_">Connexion (sans transaction)</param>
        /// <param name="cmdParams_">Une des implémentations retenues pour les paramètres ADO.NET : object[,], Parameter[] ou List&lt;KeyValuePair&lt;string, oject&gt;&gt;, ou encore null</param>
        /// <param name="cmdType_">Type de la commande, par défaut CommandType.Text</param>
        /// <param name="cmdText_">Texte de la requête SQL</param>
        /// <returns>Nombre de lignes affectées</returns>
        public int ExecuteNonQuery(DbConnection connection_, CommandType cmdType_, string cmdText_, object[,] cmdParams_)
        {
            int iNbAffectedRows;

            if (connection_.IsBackup)
            {
                lock (BackupConnectionUsageLockObject)
                {
                    // perform code with locking
                    using (DbCommand command = PrepareCommand(connection_, null, cmdText_, cmdParams_, cmdType_))
                    {
                        iNbAffectedRows = command.ExecuteNonQuery();
                    }
                }
            }
            else
            {
                // no lock
                using (DbCommand command = PrepareCommand(connection_, null, cmdText_, cmdParams_, cmdType_))
                {
                    iNbAffectedRows = command.ExecuteNonQuery();
                }
            }

            return iNbAffectedRows;
        }

        /// <summary>
        /// Exécution de System.Data.Common.DbCommand.ExecuteNonQuery() pour exécuter une requête de type UPDATE.
        /// </summary>
        /// <param name="transaction_">Transaction avec sa connexion associée</param>
        /// <param name="cmdParams_">Une des implémentations retenues pour les paramètres ADO.NET : object[,], Parameter[] ou List&lt;KeyValuePair&lt;string, oject&gt;&gt;, ou encore null</param>
        /// <param name="cmdType_">Type de la commande, par défaut CommandType.Text</param>
        /// <param name="cmdText_">Texte de la requête SQL</param>
        /// <returns>Nombre de lignes affectées</returns>
        public int ExecuteNonQuery(DbTransaction transaction_, CommandType cmdType_, string cmdText_, object[,] cmdParams_)
        {
            int iNbAffectedRows;

            if (transaction_.Connection.IsBackup)
            {
                lock (BackupConnectionUsageLockObject)
                {
                    // perform code with locking
                    using (DbCommand command = PrepareCommand(transaction_.Connection, transaction_, cmdText_, cmdParams_, cmdType_))
                    {
                        iNbAffectedRows = command.ExecuteNonQuery();
                    }
                }
            }
            else
            {
                // no lock
                using (DbCommand command = PrepareCommand(transaction_.Connection, transaction_, cmdText_, cmdParams_, cmdType_))
                {
                    iNbAffectedRows = command.ExecuteNonQuery();
                }
            }

            return iNbAffectedRows;
        }

        /// <summary>
        /// Exécution de System.Data.Common.DbCommand.ExecuteNonQuery() pour exécuter une requête de type UPDATE.
        /// </summary>
        /// <param name="connection_">Connexion (sans transaction)</param>
        /// <param name="cmdParams_">Une des implémentations retenues pour les paramètres ADO.NET : IEnumerable&lt;Parameter&gt;, ou encore null</param>
        /// <param name="cmdType_">Type de la commande, par défaut CommandType.Text</param>
        /// <param name="cmdText_">Texte de la requête SQL</param>
        /// <returns>Nombre de lignes affectées</returns>
        public int ExecuteNonQuery(DbConnection connection_, CommandType cmdType_, string cmdText_, IEnumerable<Parameter> cmdParams_)
        {
            int iNbAffectedRows;

            if (connection_.IsBackup)
            {
                lock (BackupConnectionUsageLockObject)
                {
                    // perform code with locking
                    using (DbCommand command = PrepareCommand(connection_, null, cmdText_, cmdParams_, cmdType_))
                    {
                        iNbAffectedRows = command.ExecuteNonQuery();
                    }
                }
            }
            else
            {
                // no lock
                using (DbCommand command = PrepareCommand(connection_, null, cmdText_, cmdParams_, cmdType_))
                {
                    iNbAffectedRows = command.ExecuteNonQuery();
                }
            }

            return iNbAffectedRows;
        }

        /// <summary>
        /// Exécution de System.Data.Common.DbCommand.ExecuteNonQuery() pour exécuter une requête de type UPDATE.
        /// </summary>
        /// <param name="transaction_">Transaction avec sa connexion associée</param>
        /// <param name="cmdParams_">Une des implémentations retenues pour les paramètres ADO.NET : IEnumerable&lt;Parameter&gt;, ou encore null</param>
        /// <param name="cmdType_">Type de la commande, par défaut CommandType.Text</param>
        /// <param name="cmdText_">Texte de la requête SQL</param>
        /// <returns>Nombre de lignes affectées</returns>
        public int ExecuteNonQuery(DbTransaction transaction_, CommandType cmdType_, string cmdText_, IEnumerable<Parameter> cmdParams_)
        {
            int iNbAffectedRows;

            if (transaction_.Connection.IsBackup)
            {
                lock (BackupConnectionUsageLockObject)
                {
                    // perform code with locking
                    using (DbCommand command = PrepareCommand(transaction_.Connection, transaction_, cmdText_, cmdParams_, cmdType_))
                    {
                        iNbAffectedRows = command.ExecuteNonQuery();
                    }
                }
            }
            else
            {
                // no lock
                using (DbCommand command = PrepareCommand(transaction_.Connection, transaction_, cmdText_, cmdParams_, cmdType_))
                {
                    iNbAffectedRows = command.ExecuteNonQuery();
                }
            }

            return iNbAffectedRows;
        }

        /// <summary>
        /// Exécution de System.Data.Common.DbCommand.ExecuteNonQuery() pour exécuter une requête de type UPDATE.
        /// </summary>
        /// <param name="connection_">Connexion (sans transaction)</param>
        /// <param name="cmdParams_">Une des implémentations retenues pour les paramètres ADO.NET : IEnumerable&lt;KeyValuePair&lt;string,object&gt;&gt;, ou encore null</param>
        /// <param name="cmdType_">Type de la commande, par défaut CommandType.Text</param>
        /// <param name="cmdText_">Texte de la requête SQL</param>
        /// <returns>Nombre de lignes affectées</returns>
        public int ExecuteNonQuery(DbConnection connection_, CommandType cmdType_, string cmdText_, IEnumerable<KeyValuePair<string, object>> cmdParams_)
        {
            int iNbAffectedRows;

            if (connection_.IsBackup)
            {
                lock (BackupConnectionUsageLockObject)
                {
                    // perform code with locking
                    using (DbCommand command = PrepareCommand(connection_, null, cmdText_, cmdParams_, cmdType_))
                    {
                        iNbAffectedRows = command.ExecuteNonQuery();
                    }
                }
            }
            else
            {
                // no lock
                using (DbCommand command = PrepareCommand(connection_, null, cmdText_, cmdParams_, cmdType_))
                {
                    iNbAffectedRows = command.ExecuteNonQuery();
                }
            }

            return iNbAffectedRows;
        }

        /// <summary>
        /// Exécution de System.Data.Common.DbCommand.ExecuteNonQuery() pour exécuter une requête de type UPDATE.
        /// </summary>
        /// <param name="transaction_">Transaction avec sa connexion</param>
        /// <param name="cmdParams_">Une des implémentations retenues pour les paramètres ADO.NET : IEnumerable&lt;KeyValuePair&lt;string,object&gt;&gt;, ou encore null</param>
        /// <param name="cmdType_">Type de la commande, par défaut CommandType.Text</param>
        /// <param name="cmdText_">Texte de la requête SQL</param>
        /// <returns>Nombre de lignes affectées</returns>
        public int ExecuteNonQuery(DbTransaction transaction_, CommandType cmdType_, string cmdText_, IEnumerable<KeyValuePair<string, object>> cmdParams_)
        {
            int iNbAffectedRows;

            if (transaction_.Connection.IsBackup)
            {
                lock (BackupConnectionUsageLockObject)
                {
                    // perform code with locking
                    using (DbCommand command = PrepareCommand(transaction_.Connection, transaction_, cmdText_, cmdParams_, cmdType_))
                    {
                        iNbAffectedRows = command.ExecuteNonQuery();
                    }
                }
            }
            else
            {
                // no lock
                using (DbCommand command = PrepareCommand(transaction_.Connection, transaction_, cmdText_, cmdParams_, cmdType_))
                {
                    iNbAffectedRows = command.ExecuteNonQuery();
                }
            }

            return iNbAffectedRows;
        }

        #endregion
        #endregion

        #region READER METHODS

        /// <summary>
        /// Executes a SQL select operation
        /// </summary>
        /// <param name="cmdType_">SQL command type (Text, StoredProcedure, TableDirect)</param>
        /// <param name="connection_">Connexion (sans transaction)</param>
        /// <param name="cmdText_">SQL command text</param>
        /// <param name="cmdParams_">ADO.NET parameters (name and value) in multiple array format</param>
        /// <returns>ADO .NET data reader</returns>
        public DbDataReader ExecuteReader(DbConnection connection_, string cmdText_, object[,] cmdParams_, CommandType cmdType_ = CommandType.Text)
        {
            if (connection_.IsBackup)
            {
                lock (BackupConnectionUsageLockObject)
                {
                    // perform code with locking
                    using (DbCommand command = PrepareCommand(connection_, null, cmdText_, cmdParams_, cmdType_))
                        try
                        {
                            DbDataReader dr = command.ExecuteReader(CommandBehavior.Default);
                            return dr;
                        }
                        catch (Exception ex)
                        {
                            Logger.Log(TraceEventType.Critical, ex + " Command was: " + cmdText_ + ", params count: " + command.Parameters.Count);
                            throw;
                        }
                }
            }
            // no lock
            using (DbCommand command = PrepareCommand(connection_, null, cmdText_, cmdParams_, cmdType_))
                try
                {
                    DbDataReader dr = command.ExecuteReader(CommandBehavior.Default);
                    return dr;
                }
                catch (Exception ex)
                {
                    Logger.Log(TraceEventType.Critical, ex + " Command was: " + cmdText_ + ", params count: " + command.Parameters.Count);
                    throw;
                }
        }

        /// <summary>
        /// Executes a SQL select operation
        /// </summary>
        /// <param name="cmdType_">SQL command type (Text, StoredProcedure, TableDirect)</param>
        /// <param name="transaction_">Transaction avec sa connexion associée</param>
        /// <param name="cmdText_">SQL command text</param>
        /// <param name="cmdParams_">ADO.NET parameters (name and value) in multiple array format</param>
        /// <returns>ADO .NET data reader</returns>
        public DbDataReader ExecuteReader(DbTransaction transaction_, string cmdText_, object[,] cmdParams_, CommandType cmdType_ = CommandType.Text)
        {
            if (transaction_.Connection.IsBackup)
            {
                lock (BackupConnectionUsageLockObject)
                {
                    // perform code with locking
                    using (DbCommand command = PrepareCommand(transaction_.Connection, transaction_, cmdText_, cmdParams_, cmdType_))
                        try
                        {
                            DbDataReader dr = command.ExecuteReader(CommandBehavior.Default);
                            return dr;
                        }
                        catch (Exception ex)
                        {
                            Logger.Log(TraceEventType.Critical, ex + " Command was: " + cmdText_ + ", params count: " + command.Parameters.Count);
                            throw;
                        }
                }
            }
            // no lock
            using (DbCommand command = PrepareCommand(transaction_.Connection, transaction_, cmdText_, cmdParams_, cmdType_))
                try
                {
                    DbDataReader dr = command.ExecuteReader(CommandBehavior.Default);
                    return dr;
                }
                catch (Exception ex)
                {
                    Logger.Log(TraceEventType.Critical, ex + " Command was: " + cmdText_ + ", params count: " + command.Parameters.Count);
                    throw;
                }
        }

        /// <summary>
        /// Executes a SQL select operation
        /// </summary>
        /// <param name="cmdType_">SQL command type (Text, StoredProcedure, TableDirect)</param>
        /// <param name="connection_">Connexion (sans transaction)</param>
        /// <param name="cmdText_">SQL command text</param>
        /// <param name="cmdParams_">ADO.NET parameters (name and value) in array of Parameter objects format</param>
        /// <returns>ADO .NET data reader</returns>
        public DbDataReader ExecuteReader(DbConnection connection_, string cmdText_, IEnumerable<Parameter> cmdParams_, CommandType cmdType_ = CommandType.Text)
        {
            if (connection_.IsBackup)
            {
                lock (BackupConnectionUsageLockObject)
                {
                    // perform code with locking
                    using (DbCommand command = PrepareCommand(connection_, null, cmdText_, cmdParams_, cmdType_))
                    {
                        try
                        {
                            return command.ExecuteReader(CommandBehavior.Default);
                        }
                        catch (Exception ex)
                        {
                            Logger.Log(TraceEventType.Critical, ex + " Command was: " + cmdText_ + ", params count: " + command.Parameters.Count);
                            throw;
                        }
                    }
                }
            }
            // no lock
            using (DbCommand command = PrepareCommand(connection_, null, cmdText_, cmdParams_, cmdType_))
            {
                try
                {
                    return command.ExecuteReader(CommandBehavior.Default);
                }
                catch (Exception ex)
                {
                    Logger.Log(TraceEventType.Critical, ex + " Command was: " + cmdText_ + ", params count: " + command.Parameters.Count);
                    throw;
                }
            }
        }

        /// <summary>
        /// Executes a SQL select operation
        /// </summary>
        /// <param name="cmdType_">SQL command type (Text, StoredProcedure, TableDirect)</param>
        /// <param name="transaction_">Transaction avec sa connexion associée</param>
        /// <param name="cmdText_">SQL command text</param>
        /// <param name="cmdParams_">ADO.NET parameters (name and value) in array of Parameter objects format</param>
        /// <returns>ADO .NET data reader</returns>
        public DbDataReader ExecuteReader(DbTransaction transaction_, string cmdText_, IEnumerable<Parameter> cmdParams_, CommandType cmdType_ = CommandType.Text)
        {
            if (transaction_.Connection.IsBackup)
            {
                lock (BackupConnectionUsageLockObject)
                {
                    // perform code with locking
                    using (DbCommand command = PrepareCommand(transaction_.Connection, transaction_, cmdText_, cmdParams_, cmdType_))
                    {
                        try
                        {
                            return command.ExecuteReader(CommandBehavior.Default);
                        }
                        catch (Exception ex)
                        {
                            Logger.Log(TraceEventType.Critical, ex + " Command was: " + cmdText_ + ", params count: " + command.Parameters.Count);
                            throw;
                        }
                    }
                }
            }
            // no lock
            using (DbCommand command = PrepareCommand(transaction_.Connection, transaction_, cmdText_, cmdParams_, cmdType_))
            {
                try
                {
                    return command.ExecuteReader(CommandBehavior.Default);
                }
                catch (Exception ex)
                {
                    Logger.Log(TraceEventType.Critical, ex + " Command was: " + cmdText_ + ", params count: " + command.Parameters.Count);
                    throw;
                }
            }
        }

        /// <summary>
        /// Executes a SQL select operation
        /// </summary>
        /// <param name="cmdType_">SQL command type (Text, StoredProcedure, TableDirect)</param>
        /// <param name="connection_">Connexion (sans transaction)</param>
        /// <param name="cmdText_">SQL command text</param>
        /// <param name="cmdParams_">ADO.NET parameters (name and value) formatted as a list of key/value</param>
        /// <returns>ADO .NET data reader</returns>
        public DbDataReader ExecuteReader(DbConnection connection_, string cmdText_, IEnumerable<KeyValuePair<string, object>> cmdParams_, CommandType cmdType_ = CommandType.Text)
        {
            if (connection_.IsBackup)
            {
                lock (BackupConnectionUsageLockObject)
                {
                    // perform code with locking
                    using (DbCommand command = PrepareCommand(connection_, null, cmdText_, cmdParams_, cmdType_))
                    {
                        try
                        {
                            return command.ExecuteReader(CommandBehavior.Default);
                        }
                        catch (Exception ex)
                        {
                            Logger.Log(TraceEventType.Critical, ex + " Command was: " + cmdText_ + ", params count: " + command.Parameters.Count);
                            throw;
                        }
                    }
                }
            }
            // no lock
            using (DbCommand command = PrepareCommand(connection_, null, cmdText_, cmdParams_, cmdType_))
            {
                try
                {
                    return command.ExecuteReader(CommandBehavior.Default);
                }
                catch (Exception ex)
                {
                    Logger.Log(TraceEventType.Critical, ex + " Command was: " + cmdText_ + ", params count: " + command.Parameters.Count);
                    throw;
                }
            }
        }

        /// <summary>
        /// Executes a SQL select operation
        /// </summary>
        /// <param name="cmdType_">SQL command type (Text, StoredProcedure, TableDirect)</param>
        /// <param name="transaction_">Transaction avec sa connexion associée</param>
        /// <param name="cmdText_">SQL command text</param>
        /// <param name="cmdParams_">ADO.NET parameters (name and value) formatted as a list of key/value</param>
        /// <returns>ADO .NET data reader</returns>
        public DbDataReader ExecuteReader(DbTransaction transaction_, string cmdText_, IEnumerable<KeyValuePair<string, object>> cmdParams_, CommandType cmdType_ = CommandType.Text)
        {
            if (transaction_.Connection.IsBackup)
            {
                lock (BackupConnectionUsageLockObject)
                {
                    // perform code with locking
                    using (DbCommand command = PrepareCommand(transaction_.Connection, transaction_, cmdText_, cmdParams_, cmdType_))
                    {
                        try
                        {
                            return command.ExecuteReader(CommandBehavior.Default);
                        }
                        catch (Exception ex)
                        {
                            Logger.Log(TraceEventType.Critical, ex + " Command was: " + cmdText_ + ", params count: " + command.Parameters.Count);
                            throw;
                        }
                    }
                }
            }
            // no lock
            using (DbCommand command = PrepareCommand(transaction_.Connection, transaction_, cmdText_, cmdParams_, cmdType_))
            {
                try
                {
                    return command.ExecuteReader(CommandBehavior.Default);
                }
                catch (Exception ex)
                {
                    Logger.Log(TraceEventType.Critical, ex + " Command was: " + cmdText_ + ", params count: " + command.Parameters.Count);
                    throw;
                }
            }
        }
        #endregion

        #region ADAPTER METHODS

        /// <summary>
        /// Executes a SQL select operation
        /// </summary>
        /// <param name="cmdType_">SQL command type (Text, StoredProcedure, TableDirect)</param>
        /// <param name="connection_">Connexion (sans transaction)</param>
        /// <param name="cmdText_">SQL command text</param>
        /// <param name="cmdParams_">ADO.NET parameters (name and value) in multiple object array format</param>
        /// <returns>ADO .NET dataset</returns>
        public DataSet DataAdapter(DbConnection connection_, string cmdText_, object[,] cmdParams_, CommandType cmdType_ = CommandType.Text)
        {

            if (connection_.IsBackup)
            {
                lock (BackupConnectionUsageLockObject)
                {
                    // perform code with locking
                    using (DbCommand command = PrepareCommand(connection_, null, cmdText_, cmdParams_, cmdType_))
                        try
                        {

                            DbDataAdapter dda = DbProviderFactory.CreateDataAdapter();

                            if (dda == null)
                            {
                                throw new Exception("DbHelper, DataAdapter: data adapter could not be created");
                            }
                            dda.SelectCommand = command.AdoDbCommand;
                            DataSet ds = new DataSet();
                            dda.Fill(ds);
                            return ds;
                        }
                        catch (Exception ex)
                        {
                            Logger.Log(TraceEventType.Critical, ex + " Command was: " + cmdText_ + ", params count: " + command.Parameters.Count);
                            throw;
                        }
                }
            }
            // no lock

            using (DbCommand command = PrepareCommand(connection_, null, cmdText_, cmdParams_, cmdType_))
                try
                {

                    DbDataAdapter dda = DbProviderFactory.CreateDataAdapter();

                    if (dda == null)
                    {
                        throw new Exception("DbHelper, DataAdapter: data adapter could not be created");
                    }
                    dda.SelectCommand = command.AdoDbCommand;
                    DataSet ds = new DataSet();
                    dda.Fill(ds);
                    return ds;
                }
                catch (Exception ex)
                {
                    Logger.Log(TraceEventType.Critical, ex + " Command was: " + cmdText_ + ", params count: " + command.Parameters.Count);
                    throw;
                }
        }

        /// <summary>
        /// Executes a SQL select operation
        /// </summary>
        /// <param name="cmdType_">SQL command type (Text, StoredProcedure, TableDirect)</param>
        /// <param name="connection_">Connexion (sans transaction)</param>
        /// <param name="cmdText_">SQL command text</param>
        /// <param name="cmdParams_">ADO.NET parameters (name and value) in array of Parameter objects format</param>
        /// <returns>ADO .NET dataset</returns>
        public DataSet DataAdapter(DbConnection connection_, string cmdText_, IEnumerable<Parameter> cmdParams_, CommandType cmdType_ = CommandType.Text)
        {

            if (connection_.IsBackup)
            {
                lock (BackupConnectionUsageLockObject)
                {
                    // perform code with locking
                    using (DbCommand command = PrepareCommand(connection_, null, cmdText_, cmdParams_, cmdType_))
                        try
                        {

                            DbDataAdapter dda = DbProviderFactory.CreateDataAdapter();

                            if (dda == null)
                            {
                                throw new Exception("DbHelper, DataAdapter: data adapter could not be created");
                            }
                            dda.SelectCommand = command.AdoDbCommand;
                            DataSet ds = new DataSet();
                            dda.Fill(ds);
                            return ds;
                        }
                        catch (Exception ex)
                        {
                            Logger.Log(TraceEventType.Critical, ex + " Command was: " + cmdText_ + ", params count: " + command.Parameters.Count);
                            throw;
                        }
                }

            }

            // no lock

            using (DbCommand command = PrepareCommand(connection_, null, cmdText_, cmdParams_, cmdType_))
                try
                {

                    DbDataAdapter dda = DbProviderFactory.CreateDataAdapter();

                    if (dda == null)
                    {
                        throw new Exception("DbHelper, DataAdapter: data adapter could not be created");
                    }
                    dda.SelectCommand = command.AdoDbCommand;
                    DataSet ds = new DataSet();
                    dda.Fill(ds);
                    return ds;
                }
                catch (Exception ex)
                {
                    Logger.Log(TraceEventType.Critical, ex + " Command was: " + cmdText_ + ", params count: " + command.Parameters.Count);
                    throw;
                }
        }

        /// <summary>
        /// Executes a SQL select operation
        /// </summary>
        /// <param name="cmdType_">SQL command type (Text, StoredProcedure, TableDirect)</param>
        /// <param name="connection_">Connexion (sans transaction)</param>
        /// <param name="cmdText_">SQL command text</param>
        /// <param name="cmdParams_">ADO.NET parameters (name and value) in list of key/value pair format</param>
        /// <returns>ADO .NET dataset</returns>
        public DataSet DataAdapter(DbConnection connection_, string cmdText_, IEnumerable<KeyValuePair<string, object>> cmdParams_, CommandType cmdType_ = CommandType.Text)
        {

            if (connection_.IsBackup)
            {
                lock (BackupConnectionUsageLockObject)
                {
                    // perform code with locking
                    using (DbCommand command = PrepareCommand(connection_, null, cmdText_, cmdParams_, cmdType_))
                        try
                        {

                            DbDataAdapter dda = DbProviderFactory.CreateDataAdapter();

                            if (dda == null)
                            {
                                throw new Exception("DbHelper, DataAdapter: data adapter could not be created");
                            }
                            dda.SelectCommand = command.AdoDbCommand;
                            DataSet ds = new DataSet();
                            dda.Fill(ds);
                            return ds;
                        }
                        catch (Exception ex)
                        {
                            Logger.Log(TraceEventType.Critical, ex + " Command was: " + cmdText_ + ", params count: " + command.Parameters.Count);
                            throw;
                        }
                }

            }

            // no lock
            using (DbCommand command = PrepareCommand(connection_, null, cmdText_, cmdParams_, cmdType_))
                try
                {

                    DbDataAdapter dda = DbProviderFactory.CreateDataAdapter();

                    if (dda == null)
                    {
                        throw new Exception("DbHelper, DataAdapter: data adapter could not be created");
                    }
                    dda.SelectCommand = command.AdoDbCommand;
                    DataSet ds = new DataSet();
                    dda.Fill(ds);
                    return ds;
                }
                catch (Exception ex)
                {
                    Logger.Log(TraceEventType.Critical, ex + " Command was: " + cmdText_ + ", params count: " + command.Parameters.Count);
                    throw;
                }
        }

        // TODO ORM-94 : les mêmes méthodes que ci-dessus, qui prennent en entrée un DbTransaction

        /// <summary>
        /// Executes a SQL select operation
        /// </summary>
        /// <param name="cmdType_">SQL command type (Text, StoredProcedure, TableDirect)</param>
        /// <param name="transaction_">Transaction avec sa connexion associée</param>
        /// <param name="cmdText_">SQL command text</param>
        /// <param name="cmdParams_">ADO.NET parameters (name and value) in multiple object array format</param>
        /// <returns>ADO .NET dataset</returns>
        public DataSet DataAdapter(DbTransaction transaction_, string cmdText_, object[,] cmdParams_, CommandType cmdType_ = CommandType.Text)
        {

            if (transaction_.Connection.IsBackup)
            {
                lock (BackupConnectionUsageLockObject)
                {
                    // perform code with locking
                    using (DbCommand command = PrepareCommand(transaction_.Connection, transaction_, cmdText_, cmdParams_, cmdType_))
                        try
                        {

                            DbDataAdapter dda = DbProviderFactory.CreateDataAdapter();

                            if (dda == null)
                            {
                                throw new Exception("DbHelper, DataAdapter: data adapter could not be created");
                            }
                            dda.SelectCommand = command.AdoDbCommand;
                            DataSet ds = new DataSet();
                            dda.Fill(ds);
                            return ds;
                        }
                        catch (Exception ex)
                        {
                            Logger.Log(TraceEventType.Critical, ex + " Command was: " + cmdText_ + ", params count: " + command.Parameters.Count);
                            throw;
                        }
                }
            }
            // no lock

            using (DbCommand command = PrepareCommand(transaction_.Connection, transaction_, cmdText_, cmdParams_, cmdType_))
                try
                {

                    DbDataAdapter dda = DbProviderFactory.CreateDataAdapter();

                    if (dda == null)
                    {
                        throw new Exception("DbHelper, DataAdapter: data adapter could not be created");
                    }
                    dda.SelectCommand = command.AdoDbCommand;
                    DataSet ds = new DataSet();
                    dda.Fill(ds);
                    return ds;
                }
                catch (Exception ex)
                {
                    Logger.Log(TraceEventType.Critical, ex + " Command was: " + cmdText_ + ", params count: " + command.Parameters.Count);
                    throw;
                }
        }

        /// <summary>
        /// Executes a SQL select operation
        /// </summary>
        /// <param name="cmdType_">SQL command type (Text, StoredProcedure, TableDirect)</param>
        /// <param name="transaction_">Transaction, avec sa connexion associée</param>
        /// <param name="cmdText_">SQL command text</param>
        /// <param name="cmdParams_">ADO.NET parameters (name and value) in array of Parameter objects format</param>
        /// <returns>ADO .NET dataset</returns>
        public DataSet DataAdapter(DbTransaction transaction_, string cmdText_, IEnumerable<Parameter> cmdParams_, CommandType cmdType_ = CommandType.Text)
        {

            if (transaction_.Connection.IsBackup)
            {
                lock (BackupConnectionUsageLockObject)
                {
                    // perform code with locking
                    using (DbCommand command = PrepareCommand(transaction_.Connection, transaction_, cmdText_, cmdParams_, cmdType_))
                        try
                        {

                            DbDataAdapter dda = DbProviderFactory.CreateDataAdapter();

                            if (dda == null)
                            {
                                throw new Exception("DbHelper, DataAdapter: data adapter could not be created");
                            }
                            dda.SelectCommand = command.AdoDbCommand;
                            DataSet ds = new DataSet();
                            dda.Fill(ds);
                            return ds;
                        }
                        catch (Exception ex)
                        {
                            Logger.Log(TraceEventType.Critical, ex + " Command was: " + cmdText_ + ", params count: " + command.Parameters.Count);
                            throw;
                        }
                }

            }

            // no lock

            using (DbCommand command = PrepareCommand(transaction_.Connection, transaction_, cmdText_, cmdParams_, cmdType_))
                try
                {

                    DbDataAdapter dda = DbProviderFactory.CreateDataAdapter();

                    if (dda == null)
                    {
                        throw new Exception("DbHelper, DataAdapter: data adapter could not be created");
                    }
                    dda.SelectCommand = command.AdoDbCommand;
                    DataSet ds = new DataSet();
                    dda.Fill(ds);
                    return ds;
                }
                catch (Exception ex)
                {
                    Logger.Log(TraceEventType.Critical, ex + " Command was: " + cmdText_ + ", params count: " + command.Parameters.Count);
                    throw;
                }
        }

        /// <summary>
        /// Executes a SQL select operation
        /// </summary>
        /// <param name="cmdType_">SQL command type (Text, StoredProcedure, TableDirect)</param>
        /// <param name="transaction_">Transaction, avec sa connexion associée</param>
        /// <param name="cmdText_">SQL command text</param>
        /// <param name="cmdParams_">ADO.NET parameters (name and value) in list of key/value pair format</param>
        /// <returns>ADO .NET dataset</returns>
        public DataSet DataAdapter(DbTransaction transaction_, string cmdText_, IEnumerable<KeyValuePair<string, object>> cmdParams_, CommandType cmdType_ = CommandType.Text)
        {

            if (transaction_.Connection.IsBackup)
            {
                lock (BackupConnectionUsageLockObject)
                {
                    // perform code with locking
                    using (DbCommand command = PrepareCommand(transaction_.Connection, transaction_, cmdText_, cmdParams_, cmdType_))
                        try
                        {

                            DbDataAdapter dda = DbProviderFactory.CreateDataAdapter();

                            if (dda == null)
                            {
                                throw new Exception("DbHelper, DataAdapter: data adapter could not be created");
                            }
                            dda.SelectCommand = command.AdoDbCommand;
                            DataSet ds = new DataSet();
                            dda.Fill(ds);
                            return ds;
                        }
                        catch (Exception ex)
                        {
                            Logger.Log(TraceEventType.Critical, ex + " Command was: " + cmdText_ + ", params count: " + command.Parameters.Count);
                            throw;
                        }
                }

            }

            // no lock
            using (DbCommand command = PrepareCommand(transaction_.Connection, transaction_, cmdText_, cmdParams_, cmdType_))
                try
                {

                    DbDataAdapter dda = DbProviderFactory.CreateDataAdapter();

                    if (dda == null)
                    {
                        throw new Exception("DbHelper, DataAdapter: data adapter could not be created");
                    }
                    dda.SelectCommand = command.AdoDbCommand;
                    DataSet ds = new DataSet();
                    dda.Fill(ds);
                    return ds;
                }
                catch (Exception ex)
                {
                    Logger.Log(TraceEventType.Critical, ex + " Command was: " + cmdText_ + ", params count: " + command.Parameters.Count);
                    throw;
                }
        }


        #endregion

        #region SCALAR METHODS

        /// <summary>
        /// Executes a SQL operation and returns value of first column and first line of data table result.
        /// Generally used for a query such as "count()".
        /// </summary>
        /// <param name="cmdType_">SQL command type (Text, StoredProcedure, TableDirect)</param>
        /// <param name="connection_">Connexion (sans transaction)</param>
        /// <param name="cmdText_">SQL command text</param>
        /// <param name="cmdParams_">ADO.NET parameters (name and value) in multiple object array format. Can be null</param>
        /// <returns>data value</returns>
        public object ExecuteScalar(DbConnection connection_, string cmdText_, object[,] cmdParams_, CommandType cmdType_ = CommandType.Text)
        {
            if (connection_.IsBackup)
            {
                lock (BackupConnectionUsageLockObject)
                {
                    // perform code with locking
                    using (DbCommand command = PrepareCommand(connection_, null, cmdText_, cmdParams_, cmdType_))
                    {
                        try
                        {
                            return command.ExecuteScalar();

                        }
                        catch (Exception ex)
                        {
                            Logger.Log(TraceEventType.Critical, ex + " Command was: " + cmdText_ + ", params count: " + command.Parameters.Count);
                            throw;
                        }
                    }
                }
            }

            // no lock
            using (DbCommand command = PrepareCommand(connection_, null, cmdText_, cmdParams_, cmdType_))
            {
                try
                {
                    return command.ExecuteScalar();

                }
                catch (Exception ex)
                {
                    Logger.Log(TraceEventType.Critical, ex + " Command was: " + cmdText_ + ", params count: " + command.Parameters.Count);
                    throw;
                }
            }
        }

        /// <summary>
        /// Executes a SQL operation and returns value of first column and first line of data table result.
        /// Generally used for a query such as "count()".
        /// </summary>
        /// <param name="cmdType_">SQL command type (Text, StoredProcedure, TableDirect)</param>
        /// <param name="transaction_">Transaction avec sa connexion associée</param>
        /// <param name="cmdText_">SQL command text</param>
        /// <param name="cmdParams_">ADO.NET parameters (name and value) in multiple object array format. Can be null</param>
        /// <returns>data value</returns>
        public object ExecuteScalar(DbTransaction transaction_, string cmdText_, object[,] cmdParams_, CommandType cmdType_ = CommandType.Text)
        {
            if (transaction_.Connection.IsBackup)
            {
                lock (BackupConnectionUsageLockObject)
                {
                    // perform code with locking
                    using (DbCommand command = PrepareCommand(transaction_.Connection, transaction_, cmdText_, cmdParams_, cmdType_))
                    {
                        try
                        {
                            return command.ExecuteScalar();

                        }
                        catch (Exception ex)
                        {
                            Logger.Log(TraceEventType.Critical, ex + " Command was: " + cmdText_ + ", params count: " + command.Parameters.Count);
                            throw;
                        }
                    }
                }
            }

            // no lock
            using (DbCommand command = PrepareCommand(transaction_.Connection, transaction_, cmdText_, cmdParams_, cmdType_))
            {
                try
                {
                    return command.ExecuteScalar();

                }
                catch (Exception ex)
                {
                    Logger.Log(TraceEventType.Critical, ex + " Command was: " + cmdText_ + ", params count: " + command.Parameters.Count);
                    throw;
                }
            }
        }


        /// <summary>
        /// Executes a SQL operation and returns value of first column and first line of data table result.
        /// Generally used for a query such as "count()".
        /// </summary>
        /// <param name="cmdType_">SQL command type (Text, StoredProcedure, TableDirect)</param>
        /// <param name="connection_">Connexion (sans transaction)</param>
        /// <param name="cmdText_">SQL command text</param>
        /// <param name="cmdParams_">ADO.NET parameters (name and value) in array of Parameter objects format. Can be null</param>
        /// <returns>data value</returns>
        public object ExecuteScalar(DbConnection connection_, string cmdText_, IEnumerable<Parameter> cmdParams_, CommandType cmdType_ = CommandType.Text)
        {

            if (connection_.IsBackup)
            {
                lock (BackupConnectionUsageLockObject)
                {
                    // perform code with locking
                    using (DbCommand command = PrepareCommand(connection_, null, cmdText_, cmdParams_, cmdType_))
                        try
                        {
                            return command.ExecuteScalar();

                        }
                        catch (Exception ex)
                        {
                            Logger.Log(TraceEventType.Critical, ex + " Command was: " + cmdText_ + ", params count: " + command.Parameters.Count);
                            throw;
                        }
                }
            }
            // no lock
            using (DbCommand command = PrepareCommand(connection_, null, cmdText_, cmdParams_, cmdType_))
                try
                {
                    return command.ExecuteScalar();

                }
                catch (Exception ex)
                {
                    Logger.Log(TraceEventType.Critical, ex + " Command was: " + cmdText_ + ", params count: " + command.Parameters.Count);
                    throw;
                }

        }

        /// <summary>
        /// Executes a SQL operation and returns value of first column and first line of data table result.
        /// Generally used for a query such as "count()".
        /// </summary>
        /// <param name="cmdType_">SQL command type (Text, StoredProcedure, TableDirect)</param>
        /// <param name="transaction_">Transaction avec sa connexion associée</param>
        /// <param name="cmdText_">SQL command text</param>
        /// <param name="cmdParams_">ADO.NET parameters (name and value) in array of Parameter objects format. Can be null</param>
        /// <returns>data value</returns>
        public object ExecuteScalar(DbTransaction transaction_, string cmdText_, IEnumerable<Parameter> cmdParams_, CommandType cmdType_ = CommandType.Text)
        {

            if (transaction_.Connection.IsBackup)
            {
                lock (BackupConnectionUsageLockObject)
                {
                    // perform code with locking
                    using (DbCommand command = PrepareCommand(transaction_.Connection, transaction_, cmdText_, cmdParams_, cmdType_))
                        try
                        {
                            return command.ExecuteScalar();

                        }
                        catch (Exception ex)
                        {
                            Logger.Log(TraceEventType.Critical, ex + " Command was: " + cmdText_ + ", params count: " + command.Parameters.Count);
                            throw;
                        }
                }
            }
            // no lock
            using (DbCommand command = PrepareCommand(transaction_.Connection, transaction_, cmdText_, cmdParams_, cmdType_))
                try
                {
                    return command.ExecuteScalar();

                }
                catch (Exception ex)
                {
                    Logger.Log(TraceEventType.Critical, ex + " Command was: " + cmdText_ + ", params count: " + command.Parameters.Count);
                    throw;
                }

        }
        /// <summary>
        /// Executes a SQL operation and returns value of first column and first line of data table result.
        /// Generally used for a query such as "count()".
        /// </summary>
        /// <param name="cmdType_">SQL command type (Text, StoredProcedure, TableDirect)</param>
        /// <param name="connection_">Connexion (sans transaction)</param>
        /// <param name="cmdText_">SQL command text</param>
        /// <param name="cmdParams_">ADO.NET parameters (name and value) in array of Parameter objects format. Can be null</param>
        /// <returns>data value</returns>
        public object ExecuteScalar(DbConnection connection_, string cmdText_, IEnumerable<KeyValuePair<string, object>> cmdParams_, CommandType cmdType_ = CommandType.Text)
        {

            if (connection_.IsBackup)
            {
                lock (BackupConnectionUsageLockObject)
                {
                    // perform code with locking
                    using (DbCommand command = PrepareCommand(connection_, null, cmdText_, cmdParams_, cmdType_))
                        try
                        {
                            return command.ExecuteScalar();

                        }
                        catch (Exception ex)
                        {
                            Logger.Log(TraceEventType.Critical, ex + " Command was: " + cmdText_ + ", params count: " + command.Parameters.Count);
                            throw;
                        }
                }
            }
            // no lock
            using (DbCommand command = PrepareCommand(connection_, null, cmdText_, cmdParams_, cmdType_))
                try
                {
                    return command.ExecuteScalar();

                }
                catch (Exception ex)
                {
                    Logger.Log(TraceEventType.Critical, ex + " Command was: " + cmdText_ + ", params count: " + command.Parameters.Count);
                    throw;
                }

        }

        /// <summary>
        /// Executes a SQL operation and returns value of first column and first line of data table result.
        /// Generally used for a query such as "count()".
        /// </summary>
        /// <param name="cmdType_">SQL command type (Text, StoredProcedure, TableDirect)</param>
        /// <param name="transaction_">Transaction avec sa connexion associée</param>
        /// <param name="cmdText_">SQL command text</param>
        /// <param name="cmdParams_">ADO.NET parameters (name and value) in array of Parameter objects format. Can be null</param>
        /// <returns>data value</returns>
        public object ExecuteScalar(DbTransaction transaction_, string cmdText_, IEnumerable<KeyValuePair<string, object>> cmdParams_, CommandType cmdType_ = CommandType.Text)
        {

            if (transaction_.Connection.IsBackup)
            {
                lock (BackupConnectionUsageLockObject)
                {
                    // perform code with locking
                    using (DbCommand command = PrepareCommand(transaction_.Connection, transaction_, cmdText_, cmdParams_, cmdType_))
                        try
                        {
                            return command.ExecuteScalar();

                        }
                        catch (Exception ex)
                        {
                            Logger.Log(TraceEventType.Critical, ex + " Command was: " + cmdText_ + ", params count: " + command.Parameters.Count);
                            throw;
                        }
                }
            }
            // no lock
            using (DbCommand command = PrepareCommand(transaction_.Connection, transaction_, cmdText_, cmdParams_, cmdType_))
                try
                {
                    return command.ExecuteScalar();

                }
                catch (Exception ex)
                {
                    Logger.Log(TraceEventType.Critical, ex + " Command was: " + cmdText_ + ", params count: " + command.Parameters.Count);
                    throw;
                }

        }

        #endregion

    }
}


