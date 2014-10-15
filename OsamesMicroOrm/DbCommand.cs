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

using System.Data;
using System.Data.Common;

namespace OsamesMicroOrm
{
    /// <summary>
    /// Wrapper de la classe System.Data.Common.DbCommand pour gérer une référence vers la classe DbConnection de l'ORM.
    /// Elle expose les mêmes méthodes que System.Data.Common.DbCommand à qui elle délègue.
    /// On encapsule au lieu d'hériter car System.Data.Common.DbCommand est une classe abstraite.
    /// </summary>
    public class DbCommand
    {
        /// <summary>
        /// Commande telle que fournie par l'appel à DbProviderFactory.CreateCommand();
        /// Accessible en interne pour l'ORM.
        /// </summary>
        internal System.Data.Common.DbCommand AdoCommand { get; private set; }

        /// <summary>
        /// Constructeur.
        /// </summary>
        /// <param name="command_">DbConnection de l'ORM</param>
        internal DbCommand(System.Data.Common.DbCommand command_)
        {
            AdoCommand = command_;
        }

        // TODO même principe que pour DbConnection : délégation. Vérifier tout ce qui est public sur System.Data.Common.DbCommand et l'implémenter avec délégation.
        // TODO remplacer les "protected" par "internal" car on n'est plus dans le namespace d'origine.

        public void Prepare()
        {
            throw new System.NotImplementedException();
        }

        public string CommandText { get; set; }
        public int CommandTimeout { get; set; }
        public CommandType CommandType { get; set; }
        public UpdateRowSource UpdatedRowSource { get; set; }

        /// <summary>
        /// DbConnection de l'ORM.
        /// </summary>
        internal DbConnection DbConnection { get; set; }

        protected DbParameterCollection DbParameterCollection
        {
            get { throw new System.NotImplementedException(); }
        }

        protected System.Data.Common.DbTransaction DbTransaction { protected get; protected set; }
        public bool DesignTimeVisible { get; set; }

        public void Cancel()
        {
            throw new System.NotImplementedException();
        }

        protected DbParameter CreateDbParameter()
        {
            throw new System.NotImplementedException();
        }

        protected DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            throw new System.NotImplementedException();
        }

        public int ExecuteNonQuery()
        {
            throw new System.NotImplementedException();
        }

        public object ExecuteScalar()
        {
            throw new System.NotImplementedException();
        }
    }
}
