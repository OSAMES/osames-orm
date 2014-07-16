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
    /// When set on a class, this custom attribute indicates corresponding database table name to store object corresponding to class instance.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public class DatabaseMappingAttribute : Attribute
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="dbTableName_">Database table name</param>
        public DatabaseMappingAttribute(string dbTableName_)
        {
            DbTableName = dbTableName_;
        }

        /// <summary>
        /// Database table name
        /// </summary>
        public string DbTableName { get; set; }
    }
}
