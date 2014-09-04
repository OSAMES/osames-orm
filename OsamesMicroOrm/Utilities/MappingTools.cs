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

namespace OsamesMicroOrm.Utilities
{
    /// <summary>
    /// Utility methods related to database/object mapping.
    /// </summary>
    public static class MappingTools
    {

        /// <summary>
        /// Reads value of DatabaseMapping class custom attribute.
        /// </summary>
        /// <param name="dataObject_">data object</param>
        /// <typeparam name="T">type indication</typeparam>
        /// <returns></returns>
        public static string GetDbEntityDictionnaryMappingKey<T>(T dataObject_)
        {
            // Get value
            object[] classAttributes = dataObject_.GetType().GetCustomAttributes(typeof(DatabaseMappingAttribute), false);
            if(classAttributes.Length == 0)
                throw new Exception("Type " + dataObject_.GetType().FullName + " doesn't define DatabaseMapping attribute (at class level)");
            
            if(classAttributes.Length > 1)
                throw new Exception("Type " + dataObject_.GetType().FullName + " defines more than one DatabaseMapping attribute (at class level)");

            string dbTableName = ((DatabaseMappingAttribute) classAttributes[0]).DbTableName;
            
            if (string.IsNullOrWhiteSpace(dbTableName))
                throw new Exception("Type " + dataObject_.GetType().FullName + " defines an empty DatabaseMapping attribute (at class level)");

            // Check that value exists in mapping
            if(!Configuration.ConfigurationLoader.MappingDictionnary.ContainsKey(dbTableName))
                throw new Exception("Key '" + dbTableName + "' not found in mapping configuration");

            return dbTableName;

        }
    }
}
