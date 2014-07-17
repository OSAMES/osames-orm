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

namespace OsamesMicroOrm.Utilities
{
    /// <summary>
    /// Extension methods.
    /// </summary>
    public static class ExtensionMethods
    {

        /// <summary>
        /// Array safe access, to avoid IndexOutOfRangeException.
        /// </summary>
        /// <typeparam name="T">Array type</typeparam>
        /// <param name="array_">Instance of type array extension method applies to</param>
        /// <param name="index_">index</param>
        /// <param name="element_">Out value</param>
        /// <returns></returns>
        public static bool TryGetElement<T>(this T[] array_, int index_, out T element_)
        {
            if (index_ < array_.Length)
            {
                element_ = array_[index_];
                return true;
            }
            element_ = default(T);
            return false;
        }

        // TODO voir pour faire comme avec :
        // object dbValue = reader_[columnName];
        // un accès avec une string au lieu d'un int.

    }
}
