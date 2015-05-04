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

namespace OsamesMicroOrm
{
    /// <summary>
    /// Interface à utiliser si on ne peut dériver de la classe abstraite DatabaseEntityObject qui est optimisée.
    /// </summary>
    public interface IDatabaseEntityObject
    {
        /// <summary>
        /// Retourne un identifiant unique pour la classe.
        /// </summary>
        string UniqueName { get; }
        
        /// <summary>
        /// Copie des valeurs de l'objet paramètre vers l'objet courant.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="object_"></param>
        /// <returns></returns>
        void Copy<T>(T object_);
    }
}
