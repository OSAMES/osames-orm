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

namespace OsamesMicroOrm
{
    /// <summary>
    /// Wrapper de la classe System.Data.Common.DbConnection pour gérer en plus un indicateur booléen.
    /// Elle expose les mêmes méthodes que System.Data.Common.DbConnection à qui elle délègue.
    /// On encapsule au lieu d'hériter car System.Data.Common.DbConnection est une classe abstraite.
    /// </summary>
    internal sealed class OOrmDbConnectionWrapper : IDisposable
    {
        /// <summary>
        /// Indicateur positionné à la création de l'objet connexion.
        /// Si à vrai c'est la connexion de secours, si à faux une connexion ordinaire (poolée).
        /// Cet indicateur est positionné et utilisé par DbManager.
        /// </summary>
        internal bool IsBackup { get; private set; }

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
        internal OOrmDbConnectionWrapper(System.Data.Common.DbConnection adoDbConnection_, bool defaultConnection_)
        {
            IsBackup = defaultConnection_;
            AdoDbConnection = adoDbConnection_;
        }

        /// <summary>
        /// Ferme la connexion ADO.NET associée.
        /// </summary>
        public void Dispose()
        {
            this.AdoDbConnection.Dispose();
        }

        /// <summary>
        /// Chaîne représentative de l'objet courant.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return IsBackup ? "[BACKUP]" : "[POOLED]" + AdoDbConnection;
        }

    }
}
