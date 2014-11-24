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
using System.Threading;
using System.Transactions;
using IsolationLevel = System.Data.IsolationLevel;

namespace OsamesMicroOrm
{
    /// <summary>
    /// Wrapper de la classe System.Data.Common.DbConnection pour gérer en plus un indicateur booléen.
    /// Elle expose les mêmes méthodes que System.Data.Common.DbConnection à qui elle délègue.
    /// On encapsule au lieu d'hériter car System.Data.Common.DbConnection est une classe abstraite.
    /// </summary>
    public sealed class DbConnectionWrapper : IDisposable
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
        public DbConnectionWrapper(System.Data.Common.DbConnection adoDbConnection_, bool defaultConnection_)
        {
            IsBackup = defaultConnection_;
            AdoDbConnection = adoDbConnection_;
        }

        #region reprise des mêmes propriétés publiques que System.Data.Common.DbConnection

        /// <summary>
        /// Obtient ou définit la chaîne utilisée pour ouvrir la connexion.
        /// </summary>
        public string ConnectionString { get { return AdoDbConnection.ConnectionString; } set { AdoDbConnection.ConnectionString = value; } }

        /// <summary>
        /// Obtient la durée d'attente préalable à l'établissement d'une connexion avant que la tentative ne soit abandonnée et qu'une erreur ne soit générée.
        /// </summary>
        public int ConnectionTimeout { get { return AdoDbConnection.ConnectionTimeout; } }

        /// <summary>
        /// Obtient le nom de la base de données active après avoir ouvert une connexion, ou le nom de la base de données spécifié dans la chaîne de connexion avant que la connexion ne soit ouverte.
        /// </summary>
        public string Database
        {
            get { return AdoDbConnection.Database; }
        }

        /// <summary>
        /// Obtient une chaîne qui décrit l'état de la connexion.
        /// </summary>
        public ConnectionState State
        {
            get { return AdoDbConnection.State; }
        }

        /// <summary>
        /// Obtient le nom du serveur de base de données auquel se connecter.
        /// </summary>
        public string DataSource
        {
            get { return AdoDbConnection.DataSource; }
        }

        /// <summary>
        /// Obtient une chaîne qui représente la version du serveur auquel l'objet est connecté.
        /// </summary>
        public string ServerVersion
        {
            get { return AdoDbConnection.ServerVersion; }
        }

        #endregion
        #region reprise des mêmes méthodes publiques que System.Data.Common.DbConnection

        /// <summary>
        /// Commence une transaction de base de données.
        /// </summary>
        /// <param name="isolationLevel_"></param>
        /// <returns></returns>
        internal DbTransactionWrapper BeginTransaction(IsolationLevel isolationLevel_)
        {
            return new DbTransactionWrapper(AdoDbConnection.BeginTransaction(isolationLevel_)) { Connection = this };
        }

        /// <summary>
        /// Commence une transaction de base de données.
        /// </summary>
        /// <returns></returns>
        internal DbTransactionWrapper BeginTransaction()
        {

            return new DbTransactionWrapper(AdoDbConnection.BeginTransaction()) { Connection = this };
        }

        /// <summary>
        /// Ferme la connexion à la base de données. C'est la méthode recommandée de fermeture d'une connexion ouverte.
        /// </summary>
        internal void Close()
        {
            AdoDbConnection.Close();
        }

        /// <summary>
        /// Libère toutes les ressources utilisées.
        /// </summary>
        public void Dispose()
        {
            AdoDbConnection.Dispose();
        }

        /// <summary>
        /// S'inscrit dans la transaction spécifiée.
        /// </summary>
        /// <param name="transaction_"></param>
        internal void EnlistTransaction(Transaction transaction_)
        {
            AdoDbConnection.EnlistTransaction(transaction_);
        }

        /// <summary>
        /// Retourne les informations de schéma pour la source de données de ce DbConnection.
        /// </summary>
        /// <returns></returns>
        internal DataTable GetSchema()
        {
            return AdoDbConnection.GetSchema();
        }

        /// <summary>
        /// Retourne des informations de schéma pour la source de données de ce DbConnection à l'aide de la chaîne spécifiée pour le nom de schéma.
        /// </summary>
        /// <param name="name_"></param>
        /// <returns></returns>
        internal DataTable GetSchema(string name_)
        {
            return AdoDbConnection.GetSchema(name_);
        }

        /// <summary>
        /// Retourne des informations de schéma pour la source de données de ce DbConnection à l'aide de la chaîne spécifiée pour le nom de schéma et du tableau de chaînes spécifié pour les valeurs de restriction.
        /// </summary>
        /// <param name="name_"></param>
        /// <param name="restrictions_"></param>
        /// <returns></returns>
        internal DataTable GetSchema(string name_, string[] restrictions_)
        {
            return AdoDbConnection.GetSchema(name_, restrictions_);
        }

        /// <summary>
        /// Modifie la base de données active d'une connexion ouverte.
        /// </summary>
        /// <param name="databaseName"></param>
        internal void ChangeDatabase(string databaseName)
        {
            AdoDbConnection.ChangeDatabase(databaseName);
        }

        /// <summary>
        /// Ouvre une connexion à une base de données avec les paramètres spécifiés par ConnectionString.
        /// </summary>
        internal void Open()
        {
            AdoDbConnection.Open();
        }

        /// <summary>
        /// Version asynchrone de Open, qui ouvre une connexion de base de données avec les paramètres spécifiés par ConnectionString. Cette méthode appelle la méthode virtuelle OpenAsync avec CancellationToken.None. 
        /// </summary>
        internal void OpenAsync()
        {
            AdoDbConnection.OpenAsync();
        }

        /// <summary>
        /// Il s'agit de la version asynchrone de Open.
        /// </summary>
        /// <param name="token_"></param>
        internal void OpenAsync(CancellationToken token_)
        {
            AdoDbConnection.OpenAsync(token_);
        }

        /// <summary>
        /// Crée et retourne un objet DbCommand associé à la connexion active.
        /// </summary>
        /// <returns></returns>
        internal DbCommandWrapper CreateDbCommand()
        {
            return new DbCommandWrapper(this, null, AdoDbConnection.CreateCommand());
        }

        /// <summary>
        /// Chaîne représentative de l'objet courant.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return IsBackup ? "[BACKUP]" : "[POOLED]" + AdoDbConnection;
        }
        #endregion
    }
}
