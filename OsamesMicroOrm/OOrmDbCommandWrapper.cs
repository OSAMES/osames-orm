﻿/*
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
    internal sealed class OOrmDbCommandWrapper : IDisposable
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
        internal OOrmDbCommandWrapper(OOrmDbConnectionWrapper connection_, OOrmDbTransactionWrapper transaction_, System.Data.Common.DbCommand command_)
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
        internal OOrmDbCommandWrapper(OOrmDbConnectionWrapper connection_, OOrmDbTransactionWrapper transaction_, string cmdText_, object[,] cmdParams_, CommandType cmdType_ = CommandType.Text)
        {
            this.PrepareCommand(connection_, transaction_, cmdText_, cmdParams_, cmdType_);
        }

        /// <summary>
        /// Constructeur
        /// </summary>
        /// <param name="connection_">Référence sur la DbConnection de l'ORM</param>
        /// <param name="transaction_">Référence sur la DbTransaction de l'ORM</param>
        /// <param name="cmdText_">Texte SQL</param>
        /// <param name="cmdParams_">Paramètres ADO.NET au format liste d'objets OrmDbParameter</param>
        /// <param name="cmdType_">Type de la commande SQL, texte par défaut</param>
        internal OOrmDbCommandWrapper(OOrmDbConnectionWrapper connection_, OOrmDbTransactionWrapper transaction_, string cmdText_, IEnumerable<OOrmDbParameter> cmdParams_, CommandType cmdType_ = CommandType.Text)
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
        internal OOrmDbCommandWrapper(OOrmDbConnectionWrapper connection_, OOrmDbTransactionWrapper transaction_, string cmdText_, IEnumerable<KeyValuePair<string, object>> cmdParams_, CommandType cmdType_ = CommandType.Text)
        {
            this.PrepareCommand(connection_, transaction_, cmdText_, cmdParams_, cmdType_);
        }

        public void Dispose()
        {
            this.AdoDbCommand.Dispose();
        }

        /// <summary>
        /// Obtient la collection d'objets DbParameter. 
        /// </summary>
        internal DbParameterCollection Parameters { get { return AdoDbCommand.Parameters; } }

        /// <summary>
        /// Crée une nouvelle instance d'un objet DbParameter.
        /// </summary>
        /// <returns></returns>
        internal DbParameter CreateParameter()
        {
            return AdoDbCommand.CreateParameter();
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
       /// <param name="adoParams_">ADO.NET parameters (name and value) as enumerable OrmDbParameter objects format</param>
        private void CreateDbParameters(IEnumerable<OOrmDbParameter> adoParams_)
        {
            foreach (OOrmDbParameter oParam in adoParams_)
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
        /// <param name="adoParams_">ADO.NET parameters (name and value) as enumerable OrmDbParameter objects format</param>
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
        private void PrepareCommand(OOrmDbConnectionWrapper connection_, OOrmDbTransactionWrapper transaction_, string cmdText_, object[,] cmdParams_, CommandType cmdType_ = CommandType.Text)
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
        /// <param name="cmdParams_">ADO.NET parameters (name and value) as an array of OrmDbParameter structures</param>
        private void PrepareCommand(OOrmDbConnectionWrapper connection_, OOrmDbTransactionWrapper transaction_, string cmdText_, IEnumerable<OOrmDbParameter> cmdParams_, CommandType cmdType_ = CommandType.Text)
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
        private void PrepareCommand(OOrmDbConnectionWrapper connection_, OOrmDbTransactionWrapper transaction_, string cmdText_, IEnumerable<KeyValuePair<string, object>> cmdParams_, CommandType cmdType_ = CommandType.Text)
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
        private void PrepareCommandWithoutParameter(OOrmDbConnectionWrapper connection_, OOrmDbTransactionWrapper transaction_, string cmdText_, CommandType cmdType_ = CommandType.Text)
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