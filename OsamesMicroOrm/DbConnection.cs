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
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Transactions;
using IsolationLevel = System.Data.IsolationLevel;

namespace OsamesMicroOrm
{
    /// <summary>
    /// Wrapper de la classe System.Data.Common.DbConnection pour gérer en plus un indicateur booléen.
    /// Elle expose les mêmes méthodes que System.Data.Common.DbConnection à qui elle délègue.
    /// </summary>
    public class DbConnection
    {
        /// <summary>
        /// Indicateur positionné à la création de l'objet connexion.
        /// Si à vrai c'est la connexion de secours, si à faux une connexion ordinaire (poolée).
        /// Cet indicateur est positionné et utilisé par DbManager.
        /// </summary>
        public bool IsBackup { get; private set; }

        /// <summary>
        /// Connexion telle que fournie par l'appel à DbProviderFactory.CreateConnection().
        /// Accessible en interne pour l'ORM.
        /// </summary>
        internal System.Data.Common.DbConnection AdoDbConnection { get; private set; }

        /// <summary>
        /// Constructeur.
        /// </summary>
        /// <param name="adoDbConnection_">Connexion telle que fournie par l'appel à DbProviderFactory.CreateConnection().</param>
        /// <param name="defaultConnection_">Si à vrai c'est la connexion de secours, si à faux une connexion ordinaire (poolée).</param>
        public DbConnection(System.Data.Common.DbConnection adoDbConnection_, bool defaultConnection_)
        {
            IsBackup = defaultConnection_;
            AdoDbConnection = adoDbConnection_;
        }

        #region reprise des propriétés et méthodes publiques de System.Data.Common.DbConnection

        // TODO copier les summary depuis http://msdn.microsoft.com/fr-fr/library/system.data.common.dbconnection(v=vs.110).aspx

        public System.Data.Common.DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            // TODO remplacer avec un OsamesMicroOrm.DbTransaction
            return AdoDbConnection.BeginTransaction(isolationLevel);
        }

        public System.Data.Common.DbTransaction BeginDbTransaction()
        {
            // TODO remplacer avec un OsamesMicroOrm.DbTransaction
            return AdoDbConnection.BeginTransaction();
        }

        public void Close()
        {
            AdoDbConnection.Close();
        }

        public void Dispose()
        {
            AdoDbConnection.Dispose();
        }

        public void EnlistTransaction(Transaction transaction_)
        {
            AdoDbConnection.EnlistTransaction(transaction_);
        }

        public DataTable GetSchema()
        {
            return AdoDbConnection.GetSchema();
        }

        public DataTable GetSchema(string name_)
        {
            return AdoDbConnection.GetSchema(name_);
        }

        public DataTable GetSchema(string name_, string[] restrictions_)
        {
            return AdoDbConnection.GetSchema(name_, restrictions_);
        }

        public Type GetType()
        {
            return AdoDbConnection.GetType();
        }

        public void ChangeDatabase(string databaseName)
        {
            AdoDbConnection.ChangeDatabase(databaseName);
        }

        public void Open()
        {
            AdoDbConnection.Open();
        }

        public void OpenAsync()
        {
            AdoDbConnection.OpenAsync();
        }

        public void OpenAsync(CancellationToken token_)
        {
            AdoDbConnection.OpenAsync(token_);
        }

        public string ConnectionString { get { return AdoDbConnection.ConnectionString; } set { AdoDbConnection.ConnectionString = value; } }

        public int ConnectionTimeout { get { return AdoDbConnection.ConnectionTimeout; } }

        public string Database
        {
            get { return AdoDbConnection.Database; }
        }

        public ConnectionState State
        {
            get { return AdoDbConnection.State; }
        }

        public string DataSource
        {
            get { return AdoDbConnection.DataSource; }
        }

        public string ServerVersion
        {
            get { return AdoDbConnection.ServerVersion; }
        }

        protected DbCommand CreateDbCommand()
        {
            return AdoDbConnection.CreateCommand();
        }

        public string ToString()
        {
            return IsBackup ? "[BACKUP]" : "[POOLED]" + AdoDbConnection;
        }
        #endregion
    }
}
