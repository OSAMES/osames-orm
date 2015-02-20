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
    public sealed class DbManager : IDisposable
    {

        #region DECLARATIONS

        /// <summary>
        /// Current DB provider factory.
        /// </summary>
        internal DbProviderFactory DbProviderFactory;

        /// <summary>
        /// First created connection, to be used when pool is exhausted when pooling is active.
        /// </summary>
        private OOrmDbConnectionWrapper BackupConnection;

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
        /// Indicator set to true when connection string contains "pooling = false" indication.
        /// </summary>
        private static bool DisableConnexionPooling;

        /// <summary>
        /// Singleton.
        /// </summary>
        private static DbManager Singleton;
        /// <summary>
        /// Lock object for singleton initialization.
        /// </summary>
        private static readonly object SingletonInitLockObject = new object();

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
        /// </summary>
        /// <exception cref="OOrmHandledException">Si une valeur n'a pas été positionnée via le setter avant d'appeler ce getter</exception>
        internal static string ConnectionString
        {
            get
            {
                if (string.IsNullOrWhiteSpace(ConnectionStringField))
                    throw new OOrmHandledException(HResultEnum.E_DBMANAGERNOCONNECTIONSTRINGSET, null, null);
                return ConnectionStringField;
            }
            set
            {
                ConnectionStringField = value;
                DisableConnexionPooling = ConnectionStringField.Replace(" ", "").ToLowerInvariant().Contains("pooling=false");
                Logger.Log(TraceEventType.Information, "Connection pooling " + (DisableConnexionPooling ? "disabled" : "enabled"));

            }
        }

        /// <summary>
        /// Provider specific SQL query select instruction (= command text), to execute "Select Last Insert Id".
        /// It's necessary to define it because there is no ADO.NET generic way of retrieving last insert ID after a SQL update execution.
        /// </summary>
        /// <exception cref="OOrmHandledException">Si une valeur n'a pas été positionnée via le setter avant d'appeler ce getter</exception>
        internal static string SelectLastInsertIdCommandText
        {
            get
            {
                if (SelectLastInsertIdCommandTextField == null)
                    throw new OOrmHandledException(HResultEnum.E_DBMANAGERNOSELECTLASTINSERTIDCOMMANDSET, null, null);
                return SelectLastInsertIdCommandTextField;
            }
            set { SelectLastInsertIdCommandTextField = value; }
        }

        /// <summary>
        /// Database provider definition. ADO.NET provider invariant name.
        /// </summary>
        /// <exception cref="OOrmHandledException">Si une valeur n'a pas été positionnée via le setter avant d'appeler ce getter</exception>
        internal static string ProviderName
        {
            get
            {
                if (ProviderInvariantName == null)
                    throw new OOrmHandledException(HResultEnum.E_DBMANAGERNOPROVIDERNAME, null, null);
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
        /// Destructor. Calls Dispose().
        /// </summary>
        ~DbManager()
        {
            Dispose();
        }

        /// <summary>
        /// Closes backup connection that is managed by DbManager, if case applies.
        /// Sets singleton to null.
        /// </summary>
        public void Dispose()
        {
            try
            {
                if (BackupConnection != null && BackupConnection.AdoDbConnection.State == ConnectionState.Open)
                    BackupConnection.AdoDbConnection.Close();
            }
            catch (InvalidOperationException)
            {
                // ObjectDisposedException etc
            }

            Singleton = null;
        }

        #endregion

        #region CONNECTIONS

        /// <summary>
        /// Try to get a new connection, usually from pool (may get backup connection in this case) or single connection.
        /// Opens the connection before returning it.
        /// </summary>
        /// <exception cref="OOrmHandledException">No connection at all could be opened</exception>
        internal OOrmDbConnectionWrapper CreateConnection()
        {
            try
            {
                if (DisableConnexionPooling && BackupConnection != null)
                    return BackupConnection;

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
                    BackupConnection = new OOrmDbConnectionWrapper(adoConnection, true);

                    // When connection pooling is active, try to get a second connection and return it
                    // when it's disabled, return backup connexion
                    return DisableConnexionPooling ? BackupConnection : CreateConnection();
                }
                // Not the first connection
                OOrmDbConnectionWrapper pooledConnection = new OOrmDbConnectionWrapper(adoConnection, false);
                return pooledConnection;

            }
            catch (Exception ex)
            {
                // could not get a new connection !
                if (BackupConnection == null)
                {
                    // could not get any connection
                    throw new OOrmHandledException(HResultEnum.E_CREATECONNECTIONFAILED, ex, null);
                }
                // could not get a second connection
                // use backup connection
                return BackupConnection;
            }

        }

        #endregion

        #region TRANSACTION

        /// <summary>
        /// Starts a transaction and returns it.
        /// </summary>
        /// <returns>ADO.NET transaction wrapped in an OOrmDbTransactionWrapper instance</returns>
        /// <exception cref="OOrmHandledException">When a transaction cannot be started</exception>
        internal OOrmDbTransactionWrapper OpenTransaction(OOrmDbConnectionWrapper connection_)
        {
            try
            {
                return new OOrmDbTransactionWrapper(connection_);
            }
            catch (Exception ex)
            {
                throw new OOrmHandledException(HResultEnum.E_BEGINTRANSACTIONFAILED, ex, null);
            }
        }

        /// <summary>
        /// Commits and closes a transaction.
        /// </summary>
        /// <param name="transaction_">Transaction to manage</param>
        /// <param name="closeConnexion_">Si true ferme la connexion</param>
        /// <exception cref="OOrmHandledException">Si la transaction ne peut être validée</exception>
        public void CommitTransaction(OOrmDbTransactionWrapper transaction_, bool closeConnexion_ = true)
        {
            if (transaction_ == null) return;
            try
            {
                transaction_.AdoDbTransaction.Commit();
                if (closeConnexion_)
                    transaction_.AdoDbTransaction.Connection.Close();
            }
            catch (Exception ex)
            {
                throw new OOrmHandledException(HResultEnum.E_COMMITTRANSACTIONFAILED, ex, null);
            }
        }

        /// <summary>
        /// Rollbacks and closes a transaction.
        /// </summary>
        /// <param name="transaction_">Transaction to manage</param>
        /// <exception cref="OOrmHandledException">Si la transaction ne peut être invalidée</exception>
        public void RollbackTransaction(OOrmDbTransactionWrapper transaction_)
        {
            if (transaction_ == null) return;
            try
            {
                transaction_.AdoDbTransaction.Rollback();
            }
            catch (Exception ex)
            {
                throw new OOrmHandledException(HResultEnum.E_ROLLBACKTRANSACTIONFAILED, ex, null);
            }
        }

        /// <summary>
        /// Opens a transaction with connexion and returns it.
        /// </summary>
        /// <returns>void</returns>
        /// <exception cref="OOrmHandledException">Si la transaction ne peut être ouverte</exception>
        public OOrmDbTransactionWrapper BeginTransaction()
        {
            try
            {
                OOrmDbConnectionWrapper connexion = CreateConnection();
                return new OOrmDbTransactionWrapper(connexion);
            }
            catch (Exception ex)
            {
                // this also catches ObjectDisposedException
                throw new OOrmHandledException(HResultEnum.E_BEGINTRANSACTIONFAILED, ex, null);
            }
        }

        #endregion

        #region EXECUTE METHODS

        #region avec sortie d'un ID d'enregistrement (INSERT)

        /// <summary>
        /// Exécution de System.Data.Common.DbCommand.ExecuteScalar() pour exécuter une requête de type INSERT et obtenir l'ID de la ligne insérée.
        /// Attention : cmdText_ ne doit contenir qu'un seul insert car on n'est en mesure de récupérer qu'un seul ID après insertion.
        /// </summary>
        /// <param name="connection_">Connexion (sans transaction)</param>
        /// <param name="cmdParams_">Une des implémentations retenues pour les paramètres ADO.NET : object[,] (peut être null)</param>
        /// <param name="lastInsertedRowId_">Sortie : ID du dernier enregistrement inséré</param>
        /// <param name="cmdType_">Type de la commande, par défaut CommandType.Text</param>
        /// <param name="cmdText_">Texte de la requête SQL</param>
        /// <returns>Nombre de lignes affectées</returns>
        /// <exception cref="OOrmHandledException">Last inserted row ID isn't a long number, or other SQL execution error</exception>
        internal byte ExecuteNonQuery(OOrmDbConnectionWrapper connection_, CommandType cmdType_, string cmdText_, object[,] cmdParams_, out long lastInsertedRowId_)
        {
            if (connection_.IsBackup)
            {
                lock (BackupConnection)
                {
                    lastInsertedRowId_ = new DbManagerHelper<object[,]>(connection_, cmdType_, cmdText_ + ";" + SelectLastInsertIdCommandText, SqlCommandType.Insert).Execute<long>(cmdParams_);
                }
            }
            else
            {
                lastInsertedRowId_ = new DbManagerHelper<object[,]>(connection_, cmdType_, cmdText_ + ";" + SelectLastInsertIdCommandText, SqlCommandType.Insert).Execute<long>(cmdParams_);
            }
            return 1;
        }

        /// <summary>
        /// Exécution de System.Data.Common.DbCommand.ExecuteScalar() pour exécuter une requête de type INSERT et obtenir l'ID de la ligne insérée.
        /// Attention : cmdText_ ne doit contenir qu'un seul insert car on n'est en mesure de récupérer qu'un seul ID après insertion.
        /// </summary>
        /// <param name="connection_">Connexion (sans transaction)</param>
        /// <param name="cmdParams_">Une des implémentations retenues pour les paramètres ADO.NET : IEnumerable&lt;OrmDbParameter&gt; (peut être null)</param>
        /// <param name="lastInsertedRowId_">Sortie : ID du dernier enregistrement inséré</param>
        /// <param name="cmdType_">Type de la commande, par défaut CommandType.Text</param>
        /// <param name="cmdText_">Texte de la requête SQL</param>
        /// <returns>Nombre de lignes affectées</returns>
        /// <exception cref="OOrmHandledException">Last inserted row ID isn't a long number, or other SQL execution error</exception>
        internal byte ExecuteNonQuery(OOrmDbConnectionWrapper connection_, CommandType cmdType_, string cmdText_, IEnumerable<OOrmDbParameter> cmdParams_, out long lastInsertedRowId_)
        {
            if (connection_.IsBackup)
            {
                lock (BackupConnection)
                {
                    // perform code with locking
                    lastInsertedRowId_ = new DbManagerHelper<IEnumerable<OOrmDbParameter>>(connection_, cmdType_, cmdText_ + ";" + SelectLastInsertIdCommandText, SqlCommandType.Insert).Execute<long>(cmdParams_);

                }
            }
            else
            {
                lastInsertedRowId_ = new DbManagerHelper<IEnumerable<OOrmDbParameter>>(connection_, cmdType_, cmdText_ + ";" + SelectLastInsertIdCommandText, SqlCommandType.Insert).Execute<long>(cmdParams_);
            }
            return 1;
        }

        /// <summary>
        /// Exécution de System.Data.Common.DbCommand.ExecuteScalar() pour exécuter une requête de type INSERT et obtenir l'ID de la ligne insérée.
        /// Attention : cmdText_ ne doit contenir qu'un seul insert car on n'est en mesure de récupérer qu'un seul ID après insertion.
        /// </summary>
        /// <param name="connection_">Connexion (sans transaction)</param>
        /// <param name="cmdParams_">Une des implémentations retenues pour les paramètres ADO.NET : IEnumerable&lt;KeyValuePair&lt;string, object&gt;&gt; (peut être null)</param>
        /// <param name="lastInsertedRowId_">Sortie : ID du dernier enregistrement inséré</param>
        /// <param name="cmdType_">Type de la commande, par défaut CommandType.Text</param>
        /// <param name="cmdText_">Texte de la requête SQL</param>
        /// <returns>Nombre de lignes affectées</returns>
        /// <exception cref="OOrmHandledException">Last inserted row ID isn't a long number, or other SQL execution error</exception>
        internal byte ExecuteNonQuery(OOrmDbConnectionWrapper connection_, CommandType cmdType_, string cmdText_, IEnumerable<KeyValuePair<string, object>> cmdParams_, out long lastInsertedRowId_)
        {
            if (connection_.IsBackup)
            {
                lock (BackupConnection)
                {
                    lastInsertedRowId_ = new DbManagerHelper<IEnumerable<KeyValuePair<string, object>>>(connection_, cmdType_, cmdText_ + ";" + SelectLastInsertIdCommandText, SqlCommandType.Insert).Execute<long>(cmdParams_);
                }
            }
            else
            {
                lastInsertedRowId_ = new DbManagerHelper<IEnumerable<KeyValuePair<string, object>>>(connection_, cmdType_, cmdText_ + ";" + SelectLastInsertIdCommandText, SqlCommandType.Insert).Execute<long>(cmdParams_);
            }
            return 1;
        }

        /// <summary>
        /// Exécution de System.Data.Common.DbCommand.ExecuteScalar() pour exécuter une requête de type INSERT et obtenir l'ID de la ligne insérée.
        /// Attention : cmdText_ ne doit contenir qu'un seul insert car on n'est en mesure de récupérer qu'un seul ID après insertion.
        /// </summary>
        /// <param name="transaction_">Transaction avec une connexion associée</param>
        /// <param name="cmdParams_">Une des implémentations retenues pour les paramètres ADO.NET : object[,] (peut être null)</param>
        /// <param name="lastInsertedRowId_">Sortie : ID du dernier enregistrement inséré</param>
        /// <param name="cmdType_">Type de la commande, par défaut CommandType.Text</param>
        /// <param name="cmdText_">Texte de la requête SQL</param>
        /// <returns>Nombre de lignes affectées</returns>
        /// <exception cref="OOrmHandledException">Last inserted row ID isn't a long number, or other SQL execution error</exception>
        public byte ExecuteNonQuery(OOrmDbTransactionWrapper transaction_, CommandType cmdType_, string cmdText_, object[,] cmdParams_, out long lastInsertedRowId_)
        {
            if (transaction_.ConnectionWrapper.IsBackup)
            {
                lock (BackupConnection)
                {
                    // perform code with locking
                    lastInsertedRowId_ = new DbManagerHelper<object[,]>(transaction_.ConnectionWrapper, transaction_, cmdType_, cmdText_ + ";" + SelectLastInsertIdCommandText, SqlCommandType.Insert).Execute<long>(cmdParams_);
                }
            }
            else
            {
                // no lock
                lastInsertedRowId_ = new DbManagerHelper<object[,]>(transaction_.ConnectionWrapper, transaction_, cmdType_, cmdText_ + ";" + SelectLastInsertIdCommandText, SqlCommandType.Insert).Execute<long>(cmdParams_);
            }

            return 1;
        }

        /// <summary>
        /// Exécution de System.Data.Common.DbCommand.ExecuteScalar() pour exécuter une requête de type INSERT et obtenir l'ID de la ligne insérée.
        /// Attention : cmdText_ ne doit contenir qu'un seul insert car on n'est en mesure de récupérer qu'un seul ID après insertion.
        /// </summary>
        /// <param name="transaction_">Transaction avec sa connexion associée</param>
        /// <param name="cmdParams_">Une des implémentations retenues pour les paramètres ADO.NET : IEnumerable&lt;OrmDbParameter&gt; (peut être null)</param>
        /// <param name="lastInsertedRowId_">Sortie : ID du dernier enregistrement inséré</param>
        /// <param name="cmdType_">Type de la commande, par défaut CommandType.Text</param>
        /// <param name="cmdText_">Texte de la requête SQL</param>
        /// <returns>Nombre de lignes affectées</returns>
        /// <exception cref="OOrmHandledException">Last inserted row ID isn't a long number, or other SQL execution error</exception>
        public byte ExecuteNonQuery(OOrmDbTransactionWrapper transaction_, CommandType cmdType_, string cmdText_, IEnumerable<OOrmDbParameter> cmdParams_, out long lastInsertedRowId_)
        {

            if (transaction_.ConnectionWrapper.IsBackup)
            {
                lock (BackupConnection)
                {
                    // perform code with locking
                    lastInsertedRowId_ = new DbManagerHelper<IEnumerable<OOrmDbParameter>>(transaction_.ConnectionWrapper, transaction_, cmdType_, cmdText_ + ";" + SelectLastInsertIdCommandText, SqlCommandType.Insert).Execute<long>(cmdParams_);
                }
            }
            else
            {
                // no lock
                lastInsertedRowId_ = new DbManagerHelper<IEnumerable<OOrmDbParameter>>(transaction_.ConnectionWrapper, transaction_, cmdType_, cmdText_ + ";" + SelectLastInsertIdCommandText, SqlCommandType.Insert).Execute<long>(cmdParams_);
            }

            return 1;
        }

        /// <summary>
        /// Exécution de System.Data.Common.DbCommand.ExecuteScalar() pour exécuter une requête de type INSERT et obtenir l'ID de la ligne insérée.
        /// Attention : cmdText_ ne doit contenir qu'un seul insert car on n'est en mesure de récupérer qu'un seul ID après insertion.
        /// </summary>
        /// <param name="transaction_">Transaction avec connexion associée</param>
        /// <param name="cmdParams_">Une des implémentations retenues pour les paramètres ADO.NET : IEnumerable&lt;KeyValuePair&lt;string, object&gt;&gt; (peut être null)</param>
        /// <param name="lastInsertedRowId_">Sortie : ID du dernier enregistrement inséré</param>
        /// <param name="cmdType_">Type de la commande, par défaut CommandType.Text</param>
        /// <param name="cmdText_">Texte de la requête SQL</param>
        /// <returns>Nombre de lignes affectées</returns>
        /// <exception cref="OOrmHandledException">Last inserted row ID isn't a long number, or other SQL execution error</exception>
        public byte ExecuteNonQuery(OOrmDbTransactionWrapper transaction_, CommandType cmdType_, string cmdText_, IEnumerable<KeyValuePair<string, object>> cmdParams_, out long lastInsertedRowId_)
        {
            if (transaction_.ConnectionWrapper.IsBackup)
            {
                lock (BackupConnection)
                {
                    lastInsertedRowId_ = new DbManagerHelper<IEnumerable<KeyValuePair<string, object>>>(transaction_.ConnectionWrapper, transaction_, cmdType_, cmdText_ + ";" + SelectLastInsertIdCommandText, SqlCommandType.Insert).Execute<long>(cmdParams_);
                }
            }
            else
            {
                // no lock
                lastInsertedRowId_ = new DbManagerHelper<IEnumerable<KeyValuePair<string, object>>>(transaction_.ConnectionWrapper, transaction_, cmdType_, cmdText_ + ";" + SelectLastInsertIdCommandText, SqlCommandType.Insert).Execute<long>(cmdParams_);
            }
            return 1;
        }

        #endregion

        #region sans sortie d'ID d'enregistrement (UPDATE)

        /// <summary>
        /// Exécution de System.Data.Common.DbCommand.ExecuteNonQuery() pour exécuter une requête de type UPDATE.
        /// </summary>
        /// <param name="connection_">Connexion (sans transaction)</param>
        /// <param name="cmdParams_">Une des implémentations retenues pour les paramètres ADO.NET : object[,], OrmDbParameter[] ou List&lt;KeyValuePair&lt;string, oject&gt;&gt;, ou encore null</param>
        /// <param name="cmdType_">Type de la commande, par défaut CommandType.Text</param>
        /// <param name="cmdText_">Texte de la requête SQL</param>
        /// <returns>Nombre de lignes affectées</returns>
        /// <exception cref="OOrmHandledException">SQL execution error</exception>
        internal uint ExecuteNonQuery(OOrmDbConnectionWrapper connection_, CommandType cmdType_, string cmdText_, object[,] cmdParams_)
        {
            uint iNbAffectedRows;

            if (connection_.IsBackup)
            {
                lock (BackupConnection)
                {
                    iNbAffectedRows = new DbManagerHelper<object[,]>(connection_, cmdType_, cmdText_, SqlCommandType.Update).Execute<uint>(cmdParams_);
                }
            }
            else
            {
                iNbAffectedRows = new DbManagerHelper<object[,]>(connection_, cmdType_, cmdText_, SqlCommandType.Update).Execute<uint>(cmdParams_);
            }

            return iNbAffectedRows;
        }

        /// <summary>
        /// Exécution de System.Data.Common.DbCommand.ExecuteNonQuery() pour exécuter une requête de type UPDATE.
        /// </summary>
        /// <param name="connection_">Connexion (sans transaction)</param>
        /// <param name="cmdParams_">Une des implémentations retenues pour les paramètres ADO.NET : IEnumerable&lt;OrmDbParameter&gt;, ou encore null</param>
        /// <param name="cmdType_">Type de la commande, par défaut CommandType.Text</param>
        /// <param name="cmdText_">Texte de la requête SQL</param>
        /// <returns>Nombre de lignes affectées</returns>
        /// <exception cref="OOrmHandledException">SQL execution error</exception>
        internal uint ExecuteNonQuery(OOrmDbConnectionWrapper connection_, CommandType cmdType_, string cmdText_, IEnumerable<OOrmDbParameter> cmdParams_)
        {
            uint iNbAffectedRows;

            if (connection_.IsBackup)
            {
                lock (BackupConnection)
                {
                    iNbAffectedRows = new DbManagerHelper<IEnumerable<OOrmDbParameter>>(connection_, cmdType_, cmdText_, SqlCommandType.Update).Execute<uint>(cmdParams_);
                }
            }
            else
            {
                // no lock
                iNbAffectedRows = new DbManagerHelper<IEnumerable<OOrmDbParameter>>(connection_, cmdType_, cmdText_, SqlCommandType.Update).Execute<uint>(cmdParams_);
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
        /// <exception cref="OOrmHandledException">SQL execution error</exception>
        internal uint ExecuteNonQuery(OOrmDbConnectionWrapper connection_, CommandType cmdType_, string cmdText_, IEnumerable<KeyValuePair<string, object>> cmdParams_)
        {
            uint iNbAffectedRows;

            if (connection_.IsBackup)
            {
                lock (BackupConnection)
                {
                    // perform code with locking
                    iNbAffectedRows = new DbManagerHelper<IEnumerable<KeyValuePair<string, object>>>(connection_, cmdType_, cmdText_, SqlCommandType.Update).Execute<uint>(cmdParams_);
                }
            }
            else
            {
                // no lock
                iNbAffectedRows = new DbManagerHelper<IEnumerable<KeyValuePair<string, object>>>(connection_, cmdType_, cmdText_, SqlCommandType.Update).Execute<uint>(cmdParams_);
            }

            return iNbAffectedRows;
        }

        /// <summary>
        /// Exécution de System.Data.Common.DbCommand.ExecuteNonQuery() pour exécuter une requête de type UPDATE.
        /// </summary>
        /// <param name="transaction_">Transaction avec sa connexion associée</param>
        /// <param name="cmdParams_">Une des implémentations retenues pour les paramètres ADO.NET : object[,], OrmDbParameter[] ou List&lt;KeyValuePair&lt;string, oject&gt;&gt;, ou encore null</param>
        /// <param name="cmdType_">Type de la commande, par défaut CommandType.Text</param>
        /// <param name="cmdText_">Texte de la requête SQL</param>
        /// <returns>Nombre de lignes affectées</returns>
        /// <exception cref="OOrmHandledException">SQL execution error</exception>
        public uint ExecuteNonQuery(OOrmDbTransactionWrapper transaction_, CommandType cmdType_, string cmdText_, object[,] cmdParams_)
        {
            uint iNbAffectedRows;

            if (transaction_.ConnectionWrapper.IsBackup)
            {
                lock (BackupConnection)
                {
                    // perform code with locking
                    iNbAffectedRows = new DbManagerHelper<object[,]>(transaction_.ConnectionWrapper, transaction_, cmdType_, cmdText_, SqlCommandType.Update).Execute<uint>(cmdParams_);
                }
            }
            else
            {
                // no lock
                iNbAffectedRows = new DbManagerHelper<object[,]>(transaction_.ConnectionWrapper, transaction_, cmdType_, cmdText_, SqlCommandType.Update).Execute<uint>(cmdParams_);
            }

            return iNbAffectedRows;
        }

        /// <summary>
        /// Exécution de System.Data.Common.DbCommand.ExecuteNonQuery() pour exécuter une requête de type UPDATE.
        /// </summary>
        /// <param name="transaction_">Transaction avec sa connexion associée</param>
        /// <param name="cmdParams_">Une des implémentations retenues pour les paramètres ADO.NET : IEnumerable&lt;OrmDbParameter&gt;, ou encore null</param>
        /// <param name="cmdType_">Type de la commande, par défaut CommandType.Text</param>
        /// <param name="cmdText_">Texte de la requête SQL</param>
        /// <returns>Nombre de lignes affectées</returns>
        /// <exception cref="OOrmHandledException">SQL execution error</exception>
        public uint ExecuteNonQuery(OOrmDbTransactionWrapper transaction_, CommandType cmdType_, string cmdText_, IEnumerable<OOrmDbParameter> cmdParams_)
        {
            uint iNbAffectedRows;

            if (transaction_.ConnectionWrapper.IsBackup)
            {
                lock (BackupConnection)
                {
                    iNbAffectedRows = new DbManagerHelper<IEnumerable<OOrmDbParameter>>(transaction_.ConnectionWrapper, transaction_, cmdType_, cmdText_, SqlCommandType.Update).Execute<uint>(cmdParams_);
                }
            }
            else
            {
                // no lock
                iNbAffectedRows = new DbManagerHelper<IEnumerable<OOrmDbParameter>>(transaction_.ConnectionWrapper, transaction_, cmdType_, cmdText_, SqlCommandType.Update).Execute<uint>(cmdParams_);
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
        /// <exception cref="OOrmHandledException">SQL execution error</exception>
        public uint ExecuteNonQuery(OOrmDbTransactionWrapper transaction_, CommandType cmdType_, string cmdText_, IEnumerable<KeyValuePair<string, object>> cmdParams_)
        {
            uint iNbAffectedRows;

            if (transaction_.ConnectionWrapper.IsBackup)
            {
                lock (BackupConnection)
                {
                    iNbAffectedRows = new DbManagerHelper<IEnumerable<KeyValuePair<string, object>>>(transaction_.ConnectionWrapper, transaction_, cmdType_, cmdText_, SqlCommandType.Update).Execute<uint>(cmdParams_);
                }
            }
            else
            {
                // no lock
                iNbAffectedRows = new DbManagerHelper<IEnumerable<KeyValuePair<string, object>>>(transaction_.ConnectionWrapper, transaction_, cmdType_, cmdText_, SqlCommandType.Update).Execute<uint>(cmdParams_);
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
        /// <exception cref="OOrmHandledException">SQL execution error</exception>
        internal DbDataReader ExecuteReader(OOrmDbConnectionWrapper connection_, string cmdText_, object[,] cmdParams_, CommandType cmdType_ = CommandType.Text)
        {
            if (connection_.IsBackup)
            {
                lock (BackupConnection)
                {
                    // perform code with locking
                    return new DbManagerHelper<object[,]>(connection_, cmdType_, cmdText_, SqlCommandType.Update).ExecuteReader(cmdParams_);
                }
            }
            // no lock
            return new DbManagerHelper<object[,]>(connection_, cmdType_, cmdText_, SqlCommandType.Update).ExecuteReader(cmdParams_);
        }

        /// <summary>
        /// Executes a SQL select operation
        /// </summary>
        /// <param name="cmdType_">SQL command type (Text, StoredProcedure, TableDirect)</param>
        /// <param name="connection_">Connexion (sans transaction)</param>
        /// <param name="cmdText_">SQL command text</param>
        /// <param name="cmdParams_">ADO.NET parameters (name and value) in array of OrmDbParameter objects format</param>
        /// <returns>ADO .NET data reader</returns>
        /// <exception cref="OOrmHandledException">SQL execution error</exception>
        internal DbDataReader ExecuteReader(OOrmDbConnectionWrapper connection_, string cmdText_, IEnumerable<OOrmDbParameter> cmdParams_, CommandType cmdType_ = CommandType.Text)
        {
            if (connection_.IsBackup)
            {
                lock (BackupConnection)
                {
                    return new DbManagerHelper<IEnumerable<OOrmDbParameter>>(connection_, cmdType_, cmdText_, SqlCommandType.Update).ExecuteReader(cmdParams_);
                }
            }
            // no lock
            return new DbManagerHelper<IEnumerable<OOrmDbParameter>>(connection_, cmdType_, cmdText_, SqlCommandType.Update).ExecuteReader(cmdParams_);
        }

        /// <summary>
        /// Executes a SQL select operation
        /// </summary>
        /// <param name="cmdType_">SQL command type (Text, StoredProcedure, TableDirect)</param>
        /// <param name="connection_">Connexion (sans transaction)</param>
        /// <param name="cmdText_">SQL command text</param>
        /// <param name="cmdParams_">ADO.NET parameters (name and value) formatted as a list of key/value</param>
        /// <returns>ADO .NET data reader</returns>
        /// <exception cref="OOrmHandledException">SQL execution error</exception>
        internal DbDataReader ExecuteReader(OOrmDbConnectionWrapper connection_, string cmdText_, IEnumerable<KeyValuePair<string, object>> cmdParams_, CommandType cmdType_ = CommandType.Text)
        {
            if (connection_.IsBackup)
            {
                lock (BackupConnection)
                {
                    // perform code with locking
                    return new DbManagerHelper<IEnumerable<KeyValuePair<string, object>>>(connection_, cmdType_, cmdText_, SqlCommandType.Update).ExecuteReader(cmdParams_);
                }
            }
            // no lock
            return new DbManagerHelper<IEnumerable<KeyValuePair<string, object>>>(connection_, cmdType_, cmdText_, SqlCommandType.Update).ExecuteReader(cmdParams_);
        }

        /// <summary>
        /// Executes a SQL select operation
        /// </summary>
        /// <param name="cmdType_">SQL command type (Text, StoredProcedure, TableDirect)</param>
        /// <param name="transaction_">Transaction avec sa connexion associée</param>
        /// <param name="cmdText_">SQL command text</param>
        /// <param name="cmdParams_">ADO.NET parameters (name and value) in multiple array format</param>
        /// <returns>ADO .NET data reader</returns>
        /// <exception cref="OOrmHandledException">SQL execution error</exception>
        public DbDataReader ExecuteReader(OOrmDbTransactionWrapper transaction_, string cmdText_, object[,] cmdParams_, CommandType cmdType_ = CommandType.Text)
        {
            if (transaction_.ConnectionWrapper.IsBackup)
            {
                lock (BackupConnection)
                {
                    // perform code with locking
                    return new DbManagerHelper<object[,]>(transaction_.ConnectionWrapper, transaction_, cmdType_, cmdText_, SqlCommandType.Update).ExecuteReader(cmdParams_);
                }
            }
            // no lock
            return new DbManagerHelper<object[,]>(transaction_.ConnectionWrapper, transaction_, cmdType_, cmdText_, SqlCommandType.Update).ExecuteReader(cmdParams_);
        }

        /// <summary>
        /// Executes a SQL select operation
        /// </summary>
        /// <param name="cmdType_">SQL command type (Text, StoredProcedure, TableDirect)</param>
        /// <param name="transaction_">Transaction avec sa connexion associée</param>
        /// <param name="cmdText_">SQL command text</param>
        /// <param name="cmdParams_">ADO.NET parameters (name and value) in array of OrmDbParameter objects format</param>
        /// <returns>ADO .NET data reader</returns>
        /// <exception cref="OOrmHandledException">SQL execution error</exception>
        public DbDataReader ExecuteReader(OOrmDbTransactionWrapper transaction_, string cmdText_, IEnumerable<OOrmDbParameter> cmdParams_, CommandType cmdType_ = CommandType.Text)
        {
            if (transaction_.ConnectionWrapper.IsBackup)
            {
                lock (BackupConnection)
                {
                    return new DbManagerHelper<IEnumerable<OOrmDbParameter>>(transaction_.ConnectionWrapper, transaction_, cmdType_, cmdText_, SqlCommandType.Update).ExecuteReader(cmdParams_);
                }
            }
            // no lock
            return new DbManagerHelper<IEnumerable<OOrmDbParameter>>(transaction_.ConnectionWrapper, transaction_, cmdType_, cmdText_, SqlCommandType.Update).ExecuteReader(cmdParams_);
        }

        /// <summary>
        /// Executes a SQL select operation
        /// </summary>
        /// <param name="cmdType_">SQL command type (Text, StoredProcedure, TableDirect)</param>
        /// <param name="transaction_">Transaction avec sa connexion associée</param>
        /// <param name="cmdText_">SQL command text</param>
        /// <param name="cmdParams_">ADO.NET parameters (name and value) formatted as a list of key/value</param>
        /// <returns>ADO .NET data reader</returns>
        /// <exception cref="OOrmHandledException">SQL execution error</exception>
        public DbDataReader ExecuteReader(OOrmDbTransactionWrapper transaction_, string cmdText_, IEnumerable<KeyValuePair<string, object>> cmdParams_, CommandType cmdType_ = CommandType.Text)
        {
            if (transaction_.ConnectionWrapper.IsBackup)
            {
                lock (BackupConnection)
                {
                    // perform code with locking
                    return new DbManagerHelper<IEnumerable<KeyValuePair<string, object>>>(transaction_.ConnectionWrapper, transaction_, cmdType_, cmdText_, SqlCommandType.Update).ExecuteReader(cmdParams_);
                }
            }
            // no lock
            return new DbManagerHelper<IEnumerable<KeyValuePair<string, object>>>(transaction_.ConnectionWrapper, transaction_, cmdType_, cmdText_, SqlCommandType.Update).ExecuteReader(cmdParams_);
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
        /// <exception cref="OOrmHandledException">SQL execution error</exception>
        internal DataSet DataAdapter(OOrmDbConnectionWrapper connection_, string cmdText_, object[,] cmdParams_, CommandType cmdType_ = CommandType.Text)
        {

            if (connection_.IsBackup)
            {
                lock (BackupConnection)
                {
                    return new DbManagerHelper<object[,]>(connection_, cmdType_, cmdText_, SqlCommandType.Adapter).DataAdapter(cmdParams_);
                }
            }
            // no lock
            return new DbManagerHelper<object[,]>(connection_, cmdType_, cmdText_, SqlCommandType.Adapter).DataAdapter(cmdParams_);
        }

        /// <summary>
        /// Executes a SQL select operation
        /// </summary>
        /// <param name="cmdType_">SQL command type (Text, StoredProcedure, TableDirect)</param>
        /// <param name="connection_">Connexion (sans transaction)</param>
        /// <param name="cmdText_">SQL command text</param>
        /// <param name="cmdParams_">ADO.NET parameters (name and value) in array of OrmDbParameter objects format</param>
        /// <returns>ADO .NET dataset</returns>
        /// <exception cref="OOrmHandledException">SQL execution error</exception>
        internal DataSet DataAdapter(OOrmDbConnectionWrapper connection_, string cmdText_, IEnumerable<OOrmDbParameter> cmdParams_, CommandType cmdType_ = CommandType.Text)
        {

            if (connection_.IsBackup)
            {
                lock (BackupConnection)
                {
                    return new DbManagerHelper<IEnumerable<OOrmDbParameter>>(connection_, cmdType_, cmdText_, SqlCommandType.Adapter).DataAdapter(cmdParams_);
                }
            }
            // no lock
            return new DbManagerHelper<IEnumerable<OOrmDbParameter>>(connection_, cmdType_, cmdText_, SqlCommandType.Adapter).DataAdapter(cmdParams_);
        }

        /// <summary>
        /// Executes a SQL select operation
        /// </summary>
        /// <param name="cmdType_">SQL command type (Text, StoredProcedure, TableDirect)</param>
        /// <param name="connection_">Connexion (sans transaction)</param>
        /// <param name="cmdText_">SQL command text</param>
        /// <param name="cmdParams_">ADO.NET parameters (name and value) in list of key/value pair format</param>
        /// <returns>ADO .NET dataset</returns>
        /// <exception cref="OOrmHandledException">SQL execution error</exception>
        internal DataSet DataAdapter(OOrmDbConnectionWrapper connection_, string cmdText_, IEnumerable<KeyValuePair<string, object>> cmdParams_, CommandType cmdType_ = CommandType.Text)
        {

            if (connection_.IsBackup)
            {
                lock (BackupConnection)
                {
                    return new DbManagerHelper<IEnumerable<KeyValuePair<string, object>>>(connection_, cmdType_, cmdText_, SqlCommandType.Adapter).DataAdapter(cmdParams_);
                }
            }
            // no lock
            return new DbManagerHelper<IEnumerable<KeyValuePair<string, object>>>(connection_, cmdType_, cmdText_, SqlCommandType.Adapter).DataAdapter(cmdParams_);
        }

        /// <summary>
        /// Executes a SQL select operation
        /// </summary>
        /// <param name="cmdType_">SQL command type (Text, StoredProcedure, TableDirect)</param>
        /// <param name="transaction_">Transaction avec sa connexion associée</param>
        /// <param name="cmdText_">SQL command text</param>
        /// <param name="cmdParams_">ADO.NET parameters (name and value) in multiple object array format</param>
        /// <returns>ADO .NET dataset</returns>
        /// <exception cref="OOrmHandledException">SQL execution error</exception>
        public DataSet DataAdapter(OOrmDbTransactionWrapper transaction_, string cmdText_, object[,] cmdParams_, CommandType cmdType_ = CommandType.Text)
        {
            if (transaction_.ConnectionWrapper.IsBackup)
            {
                lock (BackupConnection)
                {
                    // perform code with locking
                    return new DbManagerHelper<object[,]>(transaction_.ConnectionWrapper, transaction_, cmdType_, cmdText_, SqlCommandType.Adapter).DataAdapter(cmdParams_);
                }
            }
            // no lock
            return new DbManagerHelper<object[,]>(transaction_.ConnectionWrapper, transaction_, cmdType_, cmdText_, SqlCommandType.Adapter).DataAdapter(cmdParams_);
        }

        /// <summary>
        /// Executes a SQL select operation
        /// </summary>
        /// <param name="cmdType_">SQL command type (Text, StoredProcedure, TableDirect)</param>
        /// <param name="transaction_">Transaction, avec sa connexion associée</param>
        /// <param name="cmdText_">SQL command text</param>
        /// <param name="cmdParams_">ADO.NET parameters (name and value) in array of OrmDbParameter objects format</param>
        /// <returns>ADO .NET dataset</returns>
        /// <exception cref="OOrmHandledException">SQL execution error</exception>
        public DataSet DataAdapter(OOrmDbTransactionWrapper transaction_, string cmdText_, IEnumerable<OOrmDbParameter> cmdParams_, CommandType cmdType_ = CommandType.Text)
        {

            if (transaction_.ConnectionWrapper.IsBackup)
            {
                lock (BackupConnection)
                {
                    // perform code with locking
                    return new DbManagerHelper<IEnumerable<OOrmDbParameter>>(transaction_.ConnectionWrapper, transaction_, cmdType_, cmdText_, SqlCommandType.Adapter).DataAdapter(cmdParams_);
                }
            }
            // no lock
            return new DbManagerHelper<IEnumerable<OOrmDbParameter>>(transaction_.ConnectionWrapper, transaction_, cmdType_, cmdText_, SqlCommandType.Adapter).DataAdapter(cmdParams_);
        }

        /// <summary>
        /// Executes a SQL select operation
        /// </summary>
        /// <param name="cmdType_">SQL command type (Text, StoredProcedure, TableDirect)</param>
        /// <param name="transaction_">Transaction, avec sa connexion associée</param>
        /// <param name="cmdText_">SQL command text</param>
        /// <param name="cmdParams_">ADO.NET parameters (name and value) in list of key/value pair format</param>
        /// <returns>ADO .NET dataset</returns>
        /// <exception cref="OOrmHandledException">SQL execution error</exception>
        public DataSet DataAdapter(OOrmDbTransactionWrapper transaction_, string cmdText_, IEnumerable<KeyValuePair<string, object>> cmdParams_, CommandType cmdType_ = CommandType.Text)
        {

            if (transaction_.ConnectionWrapper.IsBackup)
            {
                lock (BackupConnection)
                {
                    // perform code with locking
                    return new DbManagerHelper<IEnumerable<KeyValuePair<string, object>>>(transaction_.ConnectionWrapper, transaction_, cmdType_, cmdText_, SqlCommandType.Adapter).DataAdapter(cmdParams_);
                }
            }
            // no lock
            return new DbManagerHelper<IEnumerable<KeyValuePair<string, object>>>(transaction_.ConnectionWrapper, transaction_, cmdType_, cmdText_, SqlCommandType.Adapter).DataAdapter(cmdParams_);
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
        /// <exception cref="OOrmHandledException">SQL execution error</exception>
        internal object ExecuteScalar(OOrmDbConnectionWrapper connection_, string cmdText_, object[,] cmdParams_, CommandType cmdType_ = CommandType.Text)
        {
            if (connection_.IsBackup)
            {
                lock (BackupConnection)
                {
                    return new DbManagerHelper<object[,]>(connection_, cmdType_, cmdText_, SqlCommandType.Adapter).ExecuteScalar(cmdParams_);
                }
            }

            // no lock
            return new DbManagerHelper<object[,]>(connection_, cmdType_, cmdText_, SqlCommandType.Adapter).ExecuteScalar(cmdParams_);
        }

        /// <summary>
        /// Executes a SQL operation and returns value of first column and first line of data table result.
        /// Generally used for a query such as "count()".
        /// </summary>
        /// <param name="cmdType_">SQL command type (Text, StoredProcedure, TableDirect)</param>
        /// <param name="connection_">Connexion (sans transaction)</param>
        /// <param name="cmdText_">SQL command text</param>
        /// <param name="cmdParams_">ADO.NET parameters (name and value) in array of OrmDbParameter objects format. Can be null</param>
        /// <returns>data value</returns>
        /// <exception cref="OOrmHandledException">SQL execution error</exception>
        internal object ExecuteScalar(OOrmDbConnectionWrapper connection_, string cmdText_, IEnumerable<OOrmDbParameter> cmdParams_, CommandType cmdType_ = CommandType.Text)
        {

            if (connection_.IsBackup)
            {
                lock (BackupConnection)
                {
                    return new DbManagerHelper<IEnumerable<OOrmDbParameter>>(connection_, cmdType_, cmdText_, SqlCommandType.Adapter).ExecuteScalar(cmdParams_);
                }
            }

            // no lock
            return new DbManagerHelper<IEnumerable<OOrmDbParameter>>(connection_, cmdType_, cmdText_, SqlCommandType.Adapter).ExecuteScalar(cmdParams_);

        }

        /// <summary>
        /// Executes a SQL operation and returns value of first column and first line of data table result.
        /// Generally used for a query such as "count()".
        /// </summary>
        /// <param name="cmdType_">SQL command type (Text, StoredProcedure, TableDirect)</param>
        /// <param name="connection_">Connexion (sans transaction)</param>
        /// <param name="cmdText_">SQL command text</param>
        /// <param name="cmdParams_">ADO.NET parameters (name and value) in array of OrmDbParameter objects format. Can be null</param>
        /// <returns>data value</returns>
        /// <exception cref="OOrmHandledException">SQL execution error</exception>
        internal object ExecuteScalar(OOrmDbConnectionWrapper connection_, string cmdText_, IEnumerable<KeyValuePair<string, object>> cmdParams_, CommandType cmdType_ = CommandType.Text)
        {

            if (connection_.IsBackup)
            {
                lock (BackupConnection)
                {
                    return new DbManagerHelper<IEnumerable<KeyValuePair<string, object>>>(connection_, cmdType_, cmdText_, SqlCommandType.Adapter).ExecuteScalar(cmdParams_);
                }
            }

            // no lock
            return new DbManagerHelper<IEnumerable<KeyValuePair<string, object>>>(connection_, cmdType_, cmdText_, SqlCommandType.Adapter).ExecuteScalar(cmdParams_);

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
        /// <exception cref="OOrmHandledException">SQL execution error</exception>
        public object ExecuteScalar(OOrmDbTransactionWrapper transaction_, string cmdText_, object[,] cmdParams_, CommandType cmdType_ = CommandType.Text)
        {
            if (transaction_.ConnectionWrapper.IsBackup)
            {
                lock (BackupConnection)
                {
                    return new DbManagerHelper<object[,]>(transaction_.ConnectionWrapper, transaction_, cmdType_, cmdText_, SqlCommandType.Adapter).ExecuteScalar(cmdParams_);
                }
            }

            // no lock
            return new DbManagerHelper<object[,]>(transaction_.ConnectionWrapper, transaction_, cmdType_, cmdText_, SqlCommandType.Adapter).ExecuteScalar(cmdParams_);
        }

        /// <summary>
        /// Executes a SQL operation and returns value of first column and first line of data table result.
        /// Generally used for a query such as "count()".
        /// </summary>
        /// <param name="cmdType_">SQL command type (Text, StoredProcedure, TableDirect)</param>
        /// <param name="transaction_">Transaction avec sa connexion associée</param>
        /// <param name="cmdText_">SQL command text</param>
        /// <param name="cmdParams_">ADO.NET parameters (name and value) in array of OrmDbParameter objects format. Can be null</param>
        /// <returns>data value</returns>
        /// <exception cref="OOrmHandledException">SQL execution error</exception>
        public object ExecuteScalar(OOrmDbTransactionWrapper transaction_, string cmdText_, IEnumerable<OOrmDbParameter> cmdParams_, CommandType cmdType_ = CommandType.Text)
        {
            if (transaction_.ConnectionWrapper.IsBackup)
            {
                lock (BackupConnection)
                {
                    return new DbManagerHelper<IEnumerable<OOrmDbParameter>>(transaction_.ConnectionWrapper, transaction_, cmdType_, cmdText_, SqlCommandType.Adapter).ExecuteScalar(cmdParams_);
                }
            }

            // no lock
            return new DbManagerHelper<IEnumerable<OOrmDbParameter>>(transaction_.ConnectionWrapper, transaction_, cmdType_, cmdText_, SqlCommandType.Adapter).ExecuteScalar(cmdParams_);
        }

        /// <summary>
        /// Executes a SQL operation and returns value of first column and first line of data table result.
        /// Generally used for a query such as "count()".
        /// </summary>
        /// <param name="cmdType_">SQL command type (Text, StoredProcedure, TableDirect)</param>
        /// <param name="transaction_">Transaction avec sa connexion associée</param>
        /// <param name="cmdText_">SQL command text</param>
        /// <param name="cmdParams_">ADO.NET parameters (name and value) in array of OrmDbParameter objects format. Can be null</param>
        /// <returns>data value</returns>
        /// <exception cref="OOrmHandledException">SQL execution error</exception>
        public object ExecuteScalar(OOrmDbTransactionWrapper transaction_, string cmdText_, IEnumerable<KeyValuePair<string, object>> cmdParams_, CommandType cmdType_ = CommandType.Text)
        {

            if (transaction_.ConnectionWrapper.IsBackup)
            {
                lock (BackupConnection)
                {
                    return new DbManagerHelper<IEnumerable<KeyValuePair<string, object>>>(transaction_.ConnectionWrapper, transaction_, cmdType_, cmdText_, SqlCommandType.Adapter).ExecuteScalar(cmdParams_);
                }
            }

            // no lock
            return new DbManagerHelper<IEnumerable<KeyValuePair<string, object>>>(transaction_.ConnectionWrapper, transaction_, cmdType_, cmdText_, SqlCommandType.Adapter).ExecuteScalar(cmdParams_);
        }

        #endregion

    }
}


