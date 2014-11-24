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
using System.Collections.Generic;

namespace OsamesMicroOrm
{
    /// <summary>
    /// Wrapper de la classe System.Data.Common.DbCommand pour gérer une référence vers la classe DbConnection de l'ORM.
    /// Elle expose les mêmes méthodes que System.Data.Common.DbCommand à qui elle délègue.
    /// On encapsule au lieu d'hériter car System.Data.Common.DbCommand est une classe abstraite.
    /// </summary>
    public sealed class DbCommandWrapper : IDisposable
    {

        /// <summary>
        /// Commande telle que fournie par l'appel à DbProviderFactory.CreateCommand();
        /// Accessible en interne pour l'ORM.
        /// </summary>
        internal System.Data.Common.DbCommand AdoDbCommand { get; private set; }

        /// <summary>
        /// Constructeur.
        /// </summary>
        /// <param name="connection_">Référence sur la DbConnection de l'ORM</param>
        /// <param name="transaction_">Référence sur la DbTransaction de l'ORM</param>
        /// <param name="command_">DbCommand ADO.NET</param>
        internal DbCommandWrapper(DbConnectionWrapper connection_, DbTransactionWrapper transaction_, System.Data.Common.DbCommand command_)
        {
            AdoDbCommand = command_;
            AdoDbCommand.Connection = connection_.AdoDbConnection;
            if (transaction_ != null)
                AdoDbCommand.Transaction = transaction_.AdoDbTransaction;
        }

        /// <summary>
        /// Constructeur
        /// </summary>
        /// <param name="connection_">Référence sur la DbConnection de l'ORM</param>
        /// <param name="transaction_">Référence sur la DbTransaction de l'ORM</param>
        /// <param name="cmdText_">Texte SQL</param>
        /// <param name="cmdParams_">Paramètres ADO.NET au format tableau multidimensionnel</param>
        /// <param name="cmdType_">Type de la commande SQL, texte par défaut</param>
        internal DbCommandWrapper(DbConnectionWrapper connection_, DbTransactionWrapper transaction_, string cmdText_, object[,] cmdParams_, CommandType cmdType_ = CommandType.Text)
        {
            this.PrepareCommand(connection_, transaction_, cmdText_, cmdParams_, cmdType_);
        }

        /// <summary>
        /// Constructeur
        /// </summary>
        /// <param name="connection_">Référence sur la DbConnection de l'ORM</param>
        /// <param name="transaction_">Référence sur la DbTransaction de l'ORM</param>
        /// <param name="cmdText_">Texte SQL</param>
        /// <param name="cmdParams_">Paramètres ADO.NET au format liste d'objets Parameter</param>
        /// <param name="cmdType_">Type de la commande SQL, texte par défaut</param>
        internal DbCommandWrapper(DbConnectionWrapper connection_, DbTransactionWrapper transaction_, string cmdText_, IEnumerable<Parameter> cmdParams_, CommandType cmdType_ = CommandType.Text)
        {
            this.PrepareCommand(connection_, transaction_, cmdText_, cmdParams_, cmdType_);
        }

        /// <summary>
        /// Constructeur
        /// </summary>
        /// <param name="connection_">Référence sur la DbConnection de l'ORM</param>
        /// <param name="transaction_">Référence sur la DbTransaction de l'ORM</param>
        /// <param name="cmdText_">Texte SQL</param>
        /// <param name="cmdParams_">Paramètres ADO.NET au format liste de clés/valeurs</param>
        /// <param name="cmdType_">Type de la commande SQL, texte par défaut</param>
        internal DbCommandWrapper(DbConnectionWrapper connection_, DbTransactionWrapper transaction_, string cmdText_, IEnumerable<KeyValuePair<string, object>> cmdParams_, CommandType cmdType_ = CommandType.Text)
        {
            this.PrepareCommand(connection_, transaction_, cmdText_, cmdParams_, cmdType_);
        }

        #region reprise des mêmes propriétés publiques que System.Data.Common.DbCommand
        /// <summary>
        /// Obtient ou définit la commande de texte à exécuter par rapport à la source de données.
        /// </summary>
        internal string CommandText { get { return AdoDbCommand.CommandText; } set { AdoDbCommand.CommandText = value; } }

        /// <summary>
        /// Obtient ou définit la durée d'attente qui précède le moment où il est mis fin à une tentative d'exécution d'une commande et où une erreur est générée.
        /// </summary>
        internal int CommandTimeout { get { return AdoDbCommand.CommandTimeout; } set { AdoDbCommand.CommandTimeout = value; } }

        /// <summary>
        /// Indique ou spécifie la manière dont la propriété CommandText doit être interprétée.
        /// </summary>
        internal CommandType CommandType { get { return AdoDbCommand.CommandType; } set { AdoDbCommand.CommandType = value; } }

        /// <summary>
        /// Obtient ou définit la manière dont les résultats des commandes sont appliqués à DataRow lorsqu'ils sont utilisés par la méthode Update de DbDataAdapter.
        /// </summary>
        internal UpdateRowSource UpdatedRowSource { get { return AdoDbCommand.UpdatedRowSource; } set { AdoDbCommand.UpdatedRowSource = value; } }

        /// <summary>
        /// Obtient la collection d'objets DbParameter. 
        /// </summary>
        internal DbParameterCollection Parameters { get { return AdoDbCommand.Parameters; } }

        #endregion

        #region reprise des mêmes méthodes publiques que System.Data.Common.DbCommand

        /// <summary>
        /// Crée une version préparée (ou compilée) de la commande dans la source de données.
        /// </summary>
        internal void Prepare()
        {
            AdoDbCommand.Prepare();
        }


        /// <summary>
        /// Tente d'annuler l'exécution de DbCommand.
        /// </summary>
        internal void Cancel()
        {
            AdoDbCommand.Cancel();
        }

        /// <summary>
        /// Crée une nouvelle instance d'un objet DbParameter.
        /// </summary>
        /// <returns></returns>
        internal DbParameter CreateParameter()
        {
            return AdoDbCommand.CreateParameter();
        }

        /// <summary>
        /// Exécute CommandText par rapport à Connection, et retourne un DbDataReader.
        /// </summary>
        /// <param name="behavior_"></param>
        /// <returns></returns>
        internal DbDataReader ExecuteReader(CommandBehavior behavior_)
        {
            return AdoDbCommand.ExecuteReader();
        }

        /// <summary>
        /// Version asynchrone de ExecuteReader, qui exécute CommandText par rapport à Connection et retourne DbDataReader.Appelle ExecuteDbDataReaderAsync avec CancellationToken.None.
        /// </summary>
        /// <returns></returns>
        internal DbDataReader ExecuteReaderAsync()
        {
            return AdoDbCommand.ExecuteReader();
        }

        /// <summary>
        /// Exécute une instruction SQL par rapport à un objet de connexion.
        /// </summary>
        /// <returns></returns>
        internal int ExecuteNonQuery()
        {
            return AdoDbCommand.ExecuteNonQuery();
        }

        /// <summary>
        /// Version asynchrone de ExecuteNonQuery, qui exécute une instruction SQL par rapport à un objet de connexion.Appelle ExecuteNonQueryAsync avec CancellationToken.None.
        /// </summary>
        /// <returns></returns>
        internal void ExecuteNonQueryAsync()
        {
            AdoDbCommand.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Version asynchrone de ExecuteNonQuery, qui exécute une instruction SQL par rapport à un objet de connexion.
        /// </summary>
        /// <returns></returns>
        internal void ExecuteNonQueryAsync(CancellationToken token_)
        {
            AdoDbCommand.ExecuteNonQueryAsync(token_);
        }

        /// <summary>
        /// Exécute la requête et retourne la première colonne de la première ligne du jeu de résultats retourné par la requête. Toutes les autres colonnes et lignes sont ignorées.
        /// </summary>
        /// <returns></returns>
        internal object ExecuteScalar()
        {
            return AdoDbCommand.ExecuteScalar();
        }

        /// <summary>
        /// Version asynchrone de ExecuteScalar, qui exécute la requête et retourne la première colonne de la première ligne du jeu de résultats retourné par la requête. Toutes les autres colonnes et lignes sont ignorées. Appelle ExecuteScalarAsync avec CancellationToken.None.
        /// </summary>
        /// <returns></returns>
        internal void ExecuteScalarAsync()
        {
            AdoDbCommand.ExecuteScalarAsync();
        }

        /// <summary>
        /// Version asynchrone de ExecuteScalar, qui exécute la requête et retourne la première colonne de la première ligne du jeu de résultats retourné par la requête. Toutes les autres colonnes et lignes sont ignorées.
        /// </summary>
        /// <returns></returns>
        internal void ExecuteScalarAsync(CancellationToken token_)
        {
            AdoDbCommand.ExecuteScalarAsync(token_);
        }

        /// <summary>
        /// Libération des ressources utilisées.
        /// </summary>
        public void Dispose()
        {
            AdoDbCommand.Dispose();
        }

        #endregion

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

        #region CreateDbParameters

        /// <summary>
        /// Adds ADO.NET parameters to current DbCommand.
        /// Parameters are all input parameters.
        /// </summary>
        /// <param name="adoParams_">ADO.NET parameters (name and value) in multiple array format</param>
        private void CreateDbParameters(object[,] adoParams_)
        {
            for (int i = 0; i < adoParams_.Length / 2; i++)
            {
                DbParameter dbParameter = this.CreateParameter();
                dbParameter.ParameterName = adoParams_[i, 0].ToString();
                dbParameter.Value = adoParams_[i, 1];
                dbParameter.Direction = ParameterDirection.Input;
                this.Parameters.Add(dbParameter);
            }
        }

        /// <summary>
        /// Adds ADO.NET parameters to current DbCommand.
        /// Parameters can be input or output parameters.
        /// </summary>
       /// <param name="adoParams_">ADO.NET parameters (name and value) as enumerable Parameter objects format</param>
        private void CreateDbParameters(IEnumerable<Parameter> adoParams_)
        {
            foreach (Parameter oParam in adoParams_)
            {
                DbParameter dbParameter = this.CreateParameter();
                dbParameter.ParameterName = oParam.ParamName;
                dbParameter.Value = oParam.ParamValue;
                dbParameter.Direction = oParam.ParamDirection;
                this.Parameters.Add(dbParameter);
            }
        }

        /// <summary>
        /// Adds ADO.NET parameters to current DbCommand.
        /// Parameters are all input parameters.
        /// </summary>
        /// <param name="adoParams_">ADO.NET parameters (name and value) as enumerable Parameter objects format</param>
        private void CreateDbParameters(IEnumerable<KeyValuePair<string, object>> adoParams_)
        {
            foreach (KeyValuePair<string, object> oParam in adoParams_)
            {
                DbParameter dbParameter = this.CreateParameter();
                dbParameter.ParameterName = oParam.Key;
                dbParameter.Value = oParam.Value;
                dbParameter.Direction = ParameterDirection.Input;
                this.Parameters.Add(dbParameter);
            }
        }

        #endregion

        #region PrepareCommand

        /// <summary>
        /// Initializes current DbCommand with parameters and sets it ready for execution.
        /// </summary>
        /// <param name="connection_">Référence sur la DbConnection de l'ORM</param>
        /// <param name="transaction_">Référence sur la DbTransaction de l'ORM</param>
        /// <param name="cmdType_">Type of command (Text, StoredProcedure, TableDirect)</param>
        /// <param name="cmdText_">SQL command text</param>
        /// <param name="cmdParams_">ADO.NET parameters (name and value) as a two-dimensional array</param>
        private void PrepareCommand(DbConnectionWrapper connection_, DbTransactionWrapper transaction_, string cmdText_, object[,] cmdParams_, CommandType cmdType_ = CommandType.Text)
        {
            this.PrepareCommandWithoutParameter(connection_, transaction_, cmdText_, cmdType_);

            if (cmdParams_ != null)
                CreateDbParameters(cmdParams_);
        }

        /// <summary>
        /// Initializes current DbCommand object with parameters and sets it reay for execution.
        /// </summary>
        /// <param name="connection_">Référence sur la DbConnection de l'ORM</param>
        /// <param name="transaction_">Référence sur la DbTransaction de l'ORM</param>
        /// <param name="cmdType_">Type of command (Text, StoredProcedure, TableDirect)</param>
        /// <param name="cmdText_">SQL command text</param>
        /// <param name="cmdParams_">ADO.NET parameters (name and value) as an array of Parameter structures</param>
        private void PrepareCommand(DbConnectionWrapper connection_, DbTransactionWrapper transaction_, string cmdText_, IEnumerable<Parameter> cmdParams_, CommandType cmdType_ = CommandType.Text)
        {
            this.PrepareCommandWithoutParameter(connection_, transaction_, cmdText_, cmdType_);

            if (cmdParams_ != null)
                CreateDbParameters(cmdParams_);
        }

        /// <summary>
        /// Initializes current DbCommand object with parameters and sets it ready for execution.
        /// </summary>
        /// <param name="connection_">Référence sur la DbConnection de l'ORM</param>
        /// <param name="transaction_">Référence sur la DbTransaction de l'ORM</param>
        /// <param name="cmdType_">Type of command (Text, StoredProcedure, TableDirect)</param>
        /// <param name="cmdText_">SQL command text</param>
        /// <param name="cmdParams_">ADO.NET parameters (name and value) as an a list of string and value key value pairs</param>
        private void PrepareCommand(DbConnectionWrapper connection_, DbTransactionWrapper transaction_, string cmdText_, IEnumerable<KeyValuePair<string, object>> cmdParams_, CommandType cmdType_ = CommandType.Text)
        {
            this.PrepareCommandWithoutParameter(connection_, transaction_, cmdText_, cmdType_);

            if (cmdParams_ != null)
                CreateDbParameters(cmdParams_);
        }

        /// <summary>
        /// Initializes current DbCommand object without parameters and sets it ready for execution.
        /// </summary>
        /// <param name="connection_">Référence sur la DbConnection de l'ORM</param>
        /// <param name="transaction_">Référence sur la DbTransaction de l'ORM</param>
        /// <param name="cmdType_">Type of command (Text, StoredProcedure, TableDirect)</param>
        /// <param name="cmdText_">SQL command text</param>
        private void PrepareCommandWithoutParameter(DbConnectionWrapper connection_, DbTransactionWrapper transaction_, string cmdText_, CommandType cmdType_ = CommandType.Text)
        {
            AdoDbCommand = DbManager.Instance.DbProviderFactory.CreateCommand();

            if (AdoDbCommand == null)
            {
                throw new Exception("DbCommandWrapper, PrepareCommandWithoutParameter: ADO.NET command could not be created");
            }

            this.AdoDbCommand.Connection = connection_.AdoDbConnection;
            this.AdoDbCommand.CommandText = cmdText_;
            this.AdoDbCommand.CommandType = cmdType_;
            if (transaction_ != null)
                this.AdoDbCommand.Transaction = transaction_.AdoDbTransaction;
        }

        #endregion

    }
}
