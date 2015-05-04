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

namespace OsamesMicroOrm
{
    /// <summary>
    /// Representation of an ADO.NET parameter. Used same way as an ADO.NET parameter but without depending on System.Data namespace in user code.
    /// It means more code overhead but is fine to deal with list of complex objects rather than list of values.
    /// </summary>
    public struct OOrmDbParameter
    {
        /// <summary>
        /// 
        /// </summary>
        internal string ParamName;

        /// <summary>
        /// 
        /// </summary>
        internal object ParamValue;

        /// <summary>
        /// 
        /// </summary>
        internal ParameterDirection ParamDirection;

        /// <summary>
        /// Constructor with default "in" direction.
        /// </summary>
        /// <param name="name_">Name</param>
        /// <param name="value_">Value</param>
        internal OOrmDbParameter(string name_, object value_)
        {
            ParamName = name_;
            ParamValue = value_;
            ParamDirection = ParameterDirection.Input;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name_">Name</param>
        /// <param name="value_">Value</param>
        /// <param name="direction_">ADO.NET parameter direction</param>
        internal OOrmDbParameter(string name_, object value_, ParameterDirection direction_) : this (name_, value_)
        {
            ParamDirection = direction_;
        }
    }
}
