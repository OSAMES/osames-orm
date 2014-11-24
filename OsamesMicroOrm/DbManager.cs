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
        private DbConnectionWrapper BackupConnection;

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
            try
            {
                if (BackupConnection != null && BackupConnection.State == ConnectionState.Open)
                    BackupConnection.Close();
            }
            catch (ObjectDisposedException)
            {
            }
            DbProviderFactory = null;
        }

        #endregion

        #region CONNECTIONS

        /// <summary>
        /// Try to get a new connection, usually from pool (may get backup connection in this case) or single connection.
        /// Opens the connection before returning it.
        /// May throw exception only when no connection at all can be opened.	
        /// </summary>
        public DbConnectionWrapper CreateConnection()
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
                    BackupConnection = new DbConnectionWrapper(adoConnection, true);
                    // Try to get a second connection and return it
                    return CreateConnection();
                }
                // Not the first connection
                DbConnectionWrapper pooledConnection = new DbConnectionWrapper(adoConnection, false);
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
        public void DisposeConnection(DbConnectionWrapper connexion_)
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
        internal DbTransactionWrapper OpenTransaction(DbConnectionWrapper connection_)
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
        /// <param name="closeConnexion_">Si true ferme la connexion</param>
        public void CommitTransaction(DbTransactionWrapper transaction_, bool closeConnexion_=  true)
        {
            try
            {
                if (transaction_ == null) return;

                transaction_.Commit();
                if (closeConnexion_)
                    transaction_.Connection.Close();
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
        /// <param name="closeConnexion_">Si true ferme la connexion</param>
        public void RollbackTransaction(DbTransactionWrapper transaction_, bool closeConnexion_ = true)
        {
            try
            {
                if (transaction_ == null) return;

                transaction_.Rollback();
                if (closeConnexion_)
                    transaction_.Connection.Close();
            }
            catch (InvalidOperationException ex)
            {
                Logger.Log(TraceEventType.Critical, ex.ToString());
                throw new Exception("@HandleTransaction - " + ex.Message);
            }
        }

        /// <summary>
        /// Opens a transaction with connexion and returns it.
        /// </summary>
        /// <returns>void</returns>
        public DbTransactionWrapper BeginTransaction()
        {
            try
            {
                DbConnectionWrapper connection = this.CreateConnection();
                return connection.BeginTransaction();
            }
            catch (InvalidOperationException ex)
            {
                Logger.Log(TraceEventType.Critical, ex.ToString());
                throw new Exception("beginTransaction - " + ex.Message);
            }
        }

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
        internal int ExecuteNonQuery(DbConnectionWrapper connection_, CommandType cmdType_, string cmdText_, object[,] cmdParams_, out long lastInsertedRowId_)
        {
            int iNbAffectedRows;

            if (connection_.IsBackup)
            {
                lock (BackupConnectionUsageLockObject)
                {
                    // perform code with locking
                    using (DbCommandWrapper command = new DbCommandWrapper(connection_, null, cmdText_, cmdParams_, cmdType_))
                    {
                        iNbAffectedRows = command.ExecuteNonQuery();
                    }

                    using (DbCommandWrapper command = new DbCommandWrapper(connection_, null, SelectLastInsertIdCommandText, (object[,])null))
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
                using (DbCommandWrapper command = new DbCommandWrapper(connection_, null, cmdText_, cmdParams_, cmdType_))
                {
                    iNbAffectedRows = command.ExecuteNonQuery();
                }

                using (DbCommandWrapper command = new DbCommandWrapper(connection_, null, SelectLastInsertIdCommandText, (object[,])null))
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
        internal int ExecuteNonQuery(DbConnectionWrapper connection_, CommandType cmdType_, string cmdText_, IEnumerable<DbCommandWrapper.Parameter> cmdParams_, out long lastInsertedRowId_)
        {
            int iNbAffectedRows;

            if (connection_.IsBackup)
            {
                lock (BackupConnectionUsageLockObject)
                {
                    // perform code with locking
                    using (DbCommandWrapper command = new DbCommandWrapper(connection_, null, cmdText_, cmdParams_, cmdType_))
                    {
                        iNbAffectedRows = command.ExecuteNonQuery();
                    }

                    using (DbCommandWrapper command = new DbCommandWrapper(connection_, null, SelectLastInsertIdCommandText, (IEnumerable<DbCommandWrapper.Parameter>)null))
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
                using (DbCommandWrapper command = new DbCommandWrapper(connection_, null, cmdText_, cmdParams_, cmdType_))
                {
                    iNbAffectedRows = command.ExecuteNonQuery();
                }

                using (DbCommandWrapper command = new DbCommandWrapper(connection_, null, SelectLastInsertIdCommandText, (IEnumerable<DbCommandWrapper.Parameter>)null))
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
        internal int ExecuteNonQuery(DbConnectionWrapper connection_, CommandType cmdType_, string cmdText_, IEnumerable<KeyValuePair<string, object>> cmdParams_, out long lastInsertedRowId_)
        {
            int iNbAffectedRows;

            if (connection_.IsBackup)
            {
                lock (BackupConnectionUsageLockObject)
                {
                    // perform code with locking
                    using (DbCommandWrapper command = new DbCommandWrapper(connection_, null, cmdText_, cmdParams_, cmdType_))
                    {
                        iNbAffectedRows = command.ExecuteNonQuery();
                    }

                    using (DbCommandWrapper command = new DbCommandWrapper(connection_, null, SelectLastInsertIdCommandText, (IEnumerable<KeyValuePair<string, object>>)null))
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
                using (DbCommandWrapper command = new DbCommandWrapper(connection_, null, cmdText_, cmdParams_, cmdType_))
                {
                    iNbAffectedRows = command.ExecuteNonQuery();
                }

                using (DbCommandWrapper command = new DbCommandWrapper(connection_, null, SelectLastInsertIdCommandText, (IEnumerable<KeyValuePair<string, object>>)null))
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
        public int ExecuteNonQuery(DbTransactionWrapper transaction_, CommandType cmdType_, string cmdText_, object[,] cmdParams_, out long lastInsertedRowId_)
        {
            int iNbAffectedRows;

            if (transaction_.Connection.IsBackup)
            {
                lock (BackupConnectionUsageLockObject)
                {
                    // perform code with locking
                    using (DbCommandWrapper command = new DbCommandWrapper(transaction_.Connection, transaction_, cmdText_, cmdParams_, cmdType_))
                    {
                        iNbAffectedRows = command.ExecuteNonQuery();
                    }

                    using (DbCommandWrapper command = new DbCommandWrapper(transaction_.Connection, transaction_, SelectLastInsertIdCommandText, (object[,])null))
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
                using (DbCommandWrapper command = new DbCommandWrapper(transaction_.Connection, transaction_, cmdText_, cmdParams_, cmdType_))
                {
                    iNbAffectedRows = command.ExecuteNonQuery();
                }

                using (DbCommandWrapper command = new DbCommandWrapper(transaction_.Connection, transaction_, SelectLastInsertIdCommandText, (object[,])null))
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
        public int ExecuteNonQuery(DbTransactionWrapper transaction_, CommandType cmdType_, string cmdText_, IEnumerable<DbCommandWrapper.Parameter> cmdParams_, out long lastInsertedRowId_)
        {
            int iNbAffectedRows;

            if (transaction_.Connection.IsBackup)
            {
                lock (BackupConnectionUsageLockObject)
                {
                    // perform code with locking
                    using (DbCommandWrapper command = new DbCommandWrapper(transaction_.Connection, transaction_, cmdText_, cmdParams_, cmdType_))
                    {
                        iNbAffectedRows = command.ExecuteNonQuery();
                    }

                    using (DbCommandWrapper command = new DbCommandWrapper(transaction_.Connection, transaction_, SelectLastInsertIdCommandText, (IEnumerable<DbCommandWrapper.Parameter>)null))
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
                using (DbCommandWrapper command = new DbCommandWrapper(transaction_.Connection, transaction_, cmdText_, cmdParams_, cmdType_))
                {
                    iNbAffectedRows = command.ExecuteNonQuery();
                }

                using (DbCommandWrapper command = new DbCommandWrapper(transaction_.Connection, transaction_, SelectLastInsertIdCommandText, (IEnumerable<DbCommandWrapper.Parameter>)null))
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
        public int ExecuteNonQuery(DbTransactionWrapper transaction_, CommandType cmdType_, string cmdText_, IEnumerable<KeyValuePair<string, object>> cmdParams_, out long lastInsertedRowId_)
        {
            int iNbAffectedRows;

            if (transaction_.Connection.IsBackup)
            {
                lock (BackupConnectionUsageLockObject)
                {
                    // perform code with locking
                    using (DbCommandWrapper command = new DbCommandWrapper(transaction_.Connection, transaction_, cmdText_, cmdParams_, cmdType_))
                    {
                        iNbAffectedRows = command.ExecuteNonQuery();
                    }

                    using (DbCommandWrapper command = new DbCommandWrapper(transaction_.Connection, transaction_, SelectLastInsertIdCommandText, (IEnumerable<KeyValuePair<string, object>>)null))
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
                using (DbCommandWrapper command = new DbCommandWrapper(transaction_.Connection, transaction_, cmdText_, cmdParams_, cmdType_))
                {
                    iNbAffectedRows = command.ExecuteNonQuery();
                }

                using (DbCommandWrapper command = new DbCommandWrapper(transaction_.Connection, transaction_, SelectLastInsertIdCommandText, (IEnumerable<KeyValuePair<string, object>>)null))
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
        internal int ExecuteNonQuery(DbConnectionWrapper connection_, CommandType cmdType_, string cmdText_, object[,] cmdParams_)
        {
            int iNbAffectedRows;

            if (connection_.IsBackup)
            {
                lock (BackupConnectionUsageLockObject)
                {
                    // perform code with locking
                    using (DbCommandWrapper command = new DbCommandWrapper(connection_, null, cmdText_, cmdParams_, cmdType_))
                    {

                        iNbAffectedRows = command.ExecuteNonQuery();
                    }
                }
            }
            else
            {
                // no lock
                using (DbCommandWrapper command = new DbCommandWrapper(connection_, null, cmdText_, cmdParams_, cmdType_))
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
        internal int ExecuteNonQuery(DbConnectionWrapper connection_, CommandType cmdType_, string cmdText_, IEnumerable<DbCommandWrapper.Parameter> cmdParams_)
        {
            int iNbAffectedRows;

            if (connection_.IsBackup)
            {
                lock (BackupConnectionUsageLockObject)
                {
                    // perform code with locking
                    using (DbCommandWrapper command = new DbCommandWrapper(connection_, null, cmdText_, cmdParams_, cmdType_))
                    {
                        iNbAffectedRows = command.ExecuteNonQuery();
                    }
                }
            }
            else
            {
                // no lock
                using (DbCommandWrapper command = new DbCommandWrapper(connection_, null, cmdText_, cmdParams_, cmdType_))
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
        internal int ExecuteNonQuery(DbConnectionWrapper connection_, CommandType cmdType_, string cmdText_, IEnumerable<KeyValuePair<string, object>> cmdParams_)
        {
            int iNbAffectedRows;

            if (connection_.IsBackup)
            {
                lock (BackupConnectionUsageLockObject)
                {
                    // perform code with locking
                    using (DbCommandWrapper command = new DbCommandWrapper(connection_, null, cmdText_, cmdParams_, cmdType_))
                    {
                        iNbAffectedRows = command.ExecuteNonQuery();
                    }
                }
            }
            else
            {
                // no lock
                using (DbCommandWrapper command = new DbCommandWrapper(connection_, null, cmdText_, cmdParams_, cmdType_))
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
        public int ExecuteNonQuery(DbTransactionWrapper transaction_, CommandType cmdType_, string cmdText_, object[,] cmdParams_)
        {
            int iNbAffectedRows;

            if (transaction_.Connection.IsBackup)
            {
                lock (BackupConnectionUsageLockObject)
                {
                    // perform code with locking
                    using (DbCommandWrapper command = new DbCommandWrapper(transaction_.Connection, transaction_, cmdText_, cmdParams_, cmdType_))
                    {
                        iNbAffectedRows = command.ExecuteNonQuery();
                    }
                }
            }
            else
            {
                // no lock
                using (DbCommandWrapper command = new DbCommandWrapper(transaction_.Connection, transaction_, cmdText_, cmdParams_, cmdType_))
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
        public int ExecuteNonQuery(DbTransactionWrapper transaction_, CommandType cmdType_, string cmdText_, IEnumerable<DbCommandWrapper.Parameter> cmdParams_)
        {
            int iNbAffectedRows;

            if (transaction_.Connection.IsBackup)
            {
                lock (BackupConnectionUsageLockObject)
                {
                    // perform code with locking
                    using (DbCommandWrapper command = new DbCommandWrapper(transaction_.Connection, transaction_, cmdText_, cmdParams_, cmdType_))
                    {
                        iNbAffectedRows = command.ExecuteNonQuery();
                    }
                }
            }
            else
            {
                // no lock
                using (DbCommandWrapper command = new DbCommandWrapper(transaction_.Connection, transaction_, cmdText_, cmdParams_, cmdType_))
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
        public int ExecuteNonQuery(DbTransactionWrapper transaction_, CommandType cmdType_, string cmdText_, IEnumerable<KeyValuePair<string, object>> cmdParams_)
        {
            int iNbAffectedRows;

            if (transaction_.Connection.IsBackup)
            {
                lock (BackupConnectionUsageLockObject)
                {
                    // perform code with locking
                    using (DbCommandWrapper command = new DbCommandWrapper(transaction_.Connection, transaction_, cmdText_, cmdParams_, cmdType_))
                    {
                        iNbAffectedRows = command.ExecuteNonQuery();
                    }
                }
            }
            else
            {
                // no lock
                using (DbCommandWrapper command = new DbCommandWrapper(transaction_.Connection, transaction_, cmdText_, cmdParams_, cmdType_))
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
        internal DbDataReader ExecuteReader(DbConnectionWrapper connection_, string cmdText_, object[,] cmdParams_, CommandType cmdType_ = CommandType.Text)
        {
            if (connection_.IsBackup)
            {
                lock (BackupConnectionUsageLockObject)
                {
                    // perform code with locking
                    using (DbCommandWrapper command = new DbCommandWrapper(connection_, null, cmdText_, cmdParams_, cmdType_))
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
            using (DbCommandWrapper command = new DbCommandWrapper(connection_, null, cmdText_, cmdParams_, cmdType_))
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
        internal DbDataReader ExecuteReader(DbConnectionWrapper connection_, string cmdText_, IEnumerable<DbCommandWrapper.Parameter> cmdParams_, CommandType cmdType_ = CommandType.Text)
        {
            if (connection_.IsBackup)
            {
                lock (BackupConnectionUsageLockObject)
                {
                    // perform code with locking
                    using (DbCommandWrapper command = new DbCommandWrapper(connection_, null, cmdText_, cmdParams_, cmdType_))
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
            using (DbCommandWrapper command = new DbCommandWrapper(connection_, null, cmdText_, cmdParams_, cmdType_))
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
        internal DbDataReader ExecuteReader(DbConnectionWrapper connection_, string cmdText_, IEnumerable<KeyValuePair<string, object>> cmdParams_, CommandType cmdType_ = CommandType.Text)
        {
            if (connection_.IsBackup)
            {
                lock (BackupConnectionUsageLockObject)
                {
                    // perform code with locking
                    using (DbCommandWrapper command = new DbCommandWrapper(connection_, null, cmdText_, cmdParams_, cmdType_))
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
            using (DbCommandWrapper command = new DbCommandWrapper(connection_, null, cmdText_, cmdParams_, cmdType_))
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
        /// <param name="cmdParams_">ADO.NET parameters (name and value) in multiple array format</param>
        /// <returns>ADO .NET data reader</returns>
        public DbDataReader ExecuteReader(DbTransactionWrapper transaction_, string cmdText_, object[,] cmdParams_, CommandType cmdType_ = CommandType.Text)
        {
            if (transaction_.Connection.IsBackup)
            {
                lock (BackupConnectionUsageLockObject)
                {
                    // perform code with locking
                    using (DbCommandWrapper command = new DbCommandWrapper(transaction_.Connection, transaction_, cmdText_, cmdParams_, cmdType_))
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
            using (DbCommandWrapper command = new DbCommandWrapper(transaction_.Connection, transaction_, cmdText_, cmdParams_, cmdType_))
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
        /// <param name="cmdParams_">ADO.NET parameters (name and value) in array of Parameter objects format</param>
        /// <returns>ADO .NET data reader</returns>
        public DbDataReader ExecuteReader(DbTransactionWrapper transaction_, string cmdText_, IEnumerable<DbCommandWrapper.Parameter> cmdParams_, CommandType cmdType_ = CommandType.Text)
        {
            if (transaction_.Connection.IsBackup)
            {
                lock (BackupConnectionUsageLockObject)
                {
                    // perform code with locking
                    using (DbCommandWrapper command = new DbCommandWrapper(transaction_.Connection, transaction_, cmdText_, cmdParams_, cmdType_))
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
            using (DbCommandWrapper command = new DbCommandWrapper(transaction_.Connection, transaction_, cmdText_, cmdParams_, cmdType_))
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
        public DbDataReader ExecuteReader(DbTransactionWrapper transaction_, string cmdText_, IEnumerable<KeyValuePair<string, object>> cmdParams_, CommandType cmdType_ = CommandType.Text)
        {
            if (transaction_.Connection.IsBackup)
            {
                lock (BackupConnectionUsageLockObject)
                {
                    // perform code with locking
                    using (DbCommandWrapper command = new DbCommandWrapper(transaction_.Connection, transaction_, cmdText_, cmdParams_, cmdType_))
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
            using (DbCommandWrapper command = new DbCommandWrapper(transaction_.Connection, transaction_, cmdText_, cmdParams_, cmdType_))
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
        internal DataSet DataAdapter(DbConnectionWrapper connection_, string cmdText_, object[,] cmdParams_, CommandType cmdType_ = CommandType.Text)
        {

            if (connection_.IsBackup)
            {
                lock (BackupConnectionUsageLockObject)
                {
                    // perform code with locking
                    using (DbCommandWrapper command = new DbCommandWrapper(connection_, null, cmdText_, cmdParams_, cmdType_))
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

            using (DbCommandWrapper command = new DbCommandWrapper(connection_, null, cmdText_, cmdParams_, cmdType_))
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
        internal DataSet DataAdapter(DbConnectionWrapper connection_, string cmdText_, IEnumerable<DbCommandWrapper.Parameter> cmdParams_, CommandType cmdType_ = CommandType.Text)
        {

            if (connection_.IsBackup)
            {
                lock (BackupConnectionUsageLockObject)
                {
                    // perform code with locking
                    using (DbCommandWrapper command = new DbCommandWrapper(connection_, null, cmdText_, cmdParams_, cmdType_))
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

            using (DbCommandWrapper command = new DbCommandWrapper(connection_, null, cmdText_, cmdParams_, cmdType_))
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
        internal DataSet DataAdapter(DbConnectionWrapper connection_, string cmdText_, IEnumerable<KeyValuePair<string, object>> cmdParams_, CommandType cmdType_ = CommandType.Text)
        {

            if (connection_.IsBackup)
            {
                lock (BackupConnectionUsageLockObject)
                {
                    // perform code with locking
                    using (DbCommandWrapper command = new DbCommandWrapper(connection_, null, cmdText_, cmdParams_, cmdType_))
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
            using (DbCommandWrapper command = new DbCommandWrapper(connection_, null, cmdText_, cmdParams_, cmdType_))
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
        /// <param name="transaction_">Transaction avec sa connexion associée</param>
        /// <param name="cmdText_">SQL command text</param>
        /// <param name="cmdParams_">ADO.NET parameters (name and value) in multiple object array format</param>
        /// <returns>ADO .NET dataset</returns>
        public DataSet DataAdapter(DbTransactionWrapper transaction_, string cmdText_, object[,] cmdParams_, CommandType cmdType_ = CommandType.Text)
        {

            if (transaction_.Connection.IsBackup)
            {
                lock (BackupConnectionUsageLockObject)
                {
                    // perform code with locking
                    using (DbCommandWrapper command = new DbCommandWrapper(transaction_.Connection, transaction_, cmdText_, cmdParams_, cmdType_))
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

            using (DbCommandWrapper command = new DbCommandWrapper(transaction_.Connection, transaction_, cmdText_, cmdParams_, cmdType_))
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
        public DataSet DataAdapter(DbTransactionWrapper transaction_, string cmdText_, IEnumerable<DbCommandWrapper.Parameter> cmdParams_, CommandType cmdType_ = CommandType.Text)
        {

            if (transaction_.Connection.IsBackup)
            {
                lock (BackupConnectionUsageLockObject)
                {
                    // perform code with locking
                    using (DbCommandWrapper command = new DbCommandWrapper(transaction_.Connection, transaction_, cmdText_, cmdParams_, cmdType_))
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

            using (DbCommandWrapper command = new DbCommandWrapper(transaction_.Connection, transaction_, cmdText_, cmdParams_, cmdType_))
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
        public DataSet DataAdapter(DbTransactionWrapper transaction_, string cmdText_, IEnumerable<KeyValuePair<string, object>> cmdParams_, CommandType cmdType_ = CommandType.Text)
        {

            if (transaction_.Connection.IsBackup)
            {
                lock (BackupConnectionUsageLockObject)
                {
                    // perform code with locking
                    using (DbCommandWrapper command = new DbCommandWrapper(transaction_.Connection, transaction_, cmdText_, cmdParams_, cmdType_))
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
            using (DbCommandWrapper command = new DbCommandWrapper(transaction_.Connection, transaction_, cmdText_, cmdParams_, cmdType_))
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
        internal object ExecuteScalar(DbConnectionWrapper connection_, string cmdText_, object[,] cmdParams_, CommandType cmdType_ = CommandType.Text)
        {
            if (connection_.IsBackup)
            {
                lock (BackupConnectionUsageLockObject)
                {
                    // perform code with locking
                    using (DbCommandWrapper command = new DbCommandWrapper(connection_, null, cmdText_, cmdParams_, cmdType_))
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
            using (DbCommandWrapper command = new DbCommandWrapper(connection_, null, cmdText_, cmdParams_, cmdType_))
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
        internal object ExecuteScalar(DbConnectionWrapper connection_, string cmdText_, IEnumerable<DbCommandWrapper.Parameter> cmdParams_, CommandType cmdType_ = CommandType.Text)
        {

            if (connection_.IsBackup)
            {
                lock (BackupConnectionUsageLockObject)
                {
                    // perform code with locking
                    using (DbCommandWrapper command = new DbCommandWrapper(connection_, null, cmdText_, cmdParams_, cmdType_))
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
            using (DbCommandWrapper command = new DbCommandWrapper(connection_, null, cmdText_, cmdParams_, cmdType_))
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
        internal object ExecuteScalar(DbConnectionWrapper connection_, string cmdText_, IEnumerable<KeyValuePair<string, object>> cmdParams_, CommandType cmdType_ = CommandType.Text)
        {

            if (connection_.IsBackup)
            {
                lock (BackupConnectionUsageLockObject)
                {
                    // perform code with locking
                    using (DbCommandWrapper command = new DbCommandWrapper(connection_, null, cmdText_, cmdParams_, cmdType_))
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
            using (DbCommandWrapper command = new DbCommandWrapper(connection_, null, cmdText_, cmdParams_, cmdType_))
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
        /// <param name="cmdParams_">ADO.NET parameters (name and value) in multiple object array format. Can be null</param>
        /// <returns>data value</returns>
        public object ExecuteScalar(DbTransactionWrapper transaction_, string cmdText_, object[,] cmdParams_, CommandType cmdType_ = CommandType.Text)
        {
            if (transaction_.Connection.IsBackup)
            {
                lock (BackupConnectionUsageLockObject)
                {
                    // perform code with locking
                    using (DbCommandWrapper command = new DbCommandWrapper(transaction_.Connection, transaction_, cmdText_, cmdParams_, cmdType_))
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
            using (DbCommandWrapper command = new DbCommandWrapper(transaction_.Connection, transaction_, cmdText_, cmdParams_, cmdType_))
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
        /// <param name="cmdParams_">ADO.NET parameters (name and value) in array of Parameter objects format. Can be null</param>
        /// <returns>data value</returns>
        public object ExecuteScalar(DbTransactionWrapper transaction_, string cmdText_, IEnumerable<DbCommandWrapper.Parameter> cmdParams_, CommandType cmdType_ = CommandType.Text)
        {

            if (transaction_.Connection.IsBackup)
            {
                lock (BackupConnectionUsageLockObject)
                {
                    // perform code with locking
                    using (DbCommandWrapper command = new DbCommandWrapper(transaction_.Connection, transaction_, cmdText_, cmdParams_, cmdType_))
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
            using (DbCommandWrapper command = new DbCommandWrapper(transaction_.Connection, transaction_, cmdText_, cmdParams_, cmdType_))
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
        public object ExecuteScalar(DbTransactionWrapper transaction_, string cmdText_, IEnumerable<KeyValuePair<string, object>> cmdParams_, CommandType cmdType_ = CommandType.Text)
        {

            if (transaction_.Connection.IsBackup)
            {
                lock (BackupConnectionUsageLockObject)
                {
                    // perform code with locking
                    using (DbCommandWrapper command = new DbCommandWrapper(transaction_.Connection, transaction_, cmdText_, cmdParams_, cmdType_))
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
            using (DbCommandWrapper command = new DbCommandWrapper(transaction_.Connection, transaction_, cmdText_, cmdParams_, cmdType_))
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


