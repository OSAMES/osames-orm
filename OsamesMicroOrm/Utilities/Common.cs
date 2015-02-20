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

using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace OsamesMicroOrm.Utilities
{
    /// <summary>
    /// Common utilities.
    /// </summary>
    internal static class Common
    {
        /// <summary>
        /// Checks that a file exists.
        /// </summary>
        /// <param name="fileFullPath_"></param>
        /// <exception cref="OOrmHandledException"></exception>
        internal static void CheckFile(string fileFullPath_)
        {
            if (!File.Exists(fileFullPath_))
            {
                throw new OOrmHandledException(HResultEnum.E_CONFIGFILENOTFOUND, null, fileFullPath_); 
            }
        }

        /// <summary>
        /// Count placeholders in parameter string.
        /// </summary>
        /// <param name="stringWithPlaceholders_">String with substrings like "{digits}"</param>
        /// <returns>Number of placeholders</returns>
        internal static int CountPlaceholders(string stringWithPlaceholders_)
        {
            // Je capture la chaîne "{digits}".
            var matches = Regex.Matches(stringWithPlaceholders_, @"(\{[0-9]+.*?\})");
            // Le nombre de match donne le résultat.
            return matches.Count;
        }

        /// <summary>
        /// Checks that placeholders in parameter string and number of values in parameter list match.
        /// </summary>
        /// <param name="stringWithPlaceholders_">String with substrings like "{digits}"</param>
        /// <param name="lstValuesForPlaceholders_">List of string values</param>
        /// <returns>True/false</returns>
        internal static bool CheckPlaceholdersAndParametersNumbers(string stringWithPlaceholders_, List<string> lstValuesForPlaceholders_)
        {
            return CountPlaceholders(stringWithPlaceholders_) == lstValuesForPlaceholders_.Count;
        }
    }
}
