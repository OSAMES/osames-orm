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
using System.Runtime.Serialization;

namespace OsamesMicroOrm
{
    /// <summary>
    /// Exception crée lorsqu'une exception non gérée se produit.
    /// Cette exception encapsule l'exception initiale et assure son traçage dans le log et le cas échéant d'autres traitements.
    /// C'est la seule exception en sortie de l'ORM vers l'application cliente.
    /// </summary>
    [Serializable]
    public class OOrmHandledException : Exception
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public OOrmHandledException() : base()
        {

        }

        /// <summary>
        /// Serialization Contructor
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected OOrmHandledException(SerializationInfo info, StreamingContext context) : base(info, context)
        {

        }

        
    }
}
