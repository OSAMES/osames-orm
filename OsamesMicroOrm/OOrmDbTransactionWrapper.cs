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

namespace OsamesMicroOrm
{
    /// <summary>
    /// Wrapper de la classe System.Data.Common.DbTransaction pour gérer une référence vers la classe DbConnection de l'ORM.
    /// Elle expose les mêmes méthodes que System.Data.Common.DbTransaction à qui elle délègue.
    /// On encapsule au lieu d'hériter car System.Data.Common.DbConnection est une classe abstraite.
    /// </summary>
    public sealed class OOrmDbTransactionWrapper : IDisposable
    {
        /// <summary>
        /// Connexion telle que fournie par l'appel à DbProviderFactory.CreateConnection().
        /// Accessible en interne pour l'ORM.
        /// </summary>
        internal System.Data.Common.DbTransaction AdoDbTransaction { get; private set; }

        /// <summary>
        /// Constructeur.
        /// </summary>
        /// <param name="connectionWrapper_"></param>
        /// <exception cref="ArgumentException"></exception>
        internal OOrmDbTransactionWrapper(OOrmDbConnectionWrapper connectionWrapper_)
        {
            ConnectionWrapper = connectionWrapper_;
            if(ConnectionWrapper == null)
                throw new ArgumentException("Connection cannot be null", "connectionWrapper_");

            if(ConnectionWrapper.AdoDbConnection.State != ConnectionState.Open)
                ConnectionWrapper.AdoDbConnection.Open();
            AdoDbTransaction = ConnectionWrapper.AdoDbConnection.BeginTransaction();
        }

        /// <summary>
        /// Connexion parente.
        /// </summary>
        internal OOrmDbConnectionWrapper ConnectionWrapper { get; set; }

        /// <summary>
        /// Ferme la transaction ADO.NET associée.
        /// </summary>
        public void Dispose()
        {
            AdoDbTransaction.Dispose();
            AdoDbTransaction = null;
        }

    }
}
