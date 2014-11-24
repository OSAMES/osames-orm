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
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using OsamesMicroOrm.Configuration;
using OsamesMicroOrm.Logging;

namespace OsamesMicroOrm.Utilities
{
    /// <summary>
    /// Common utilities.
    /// </summary>
    internal static class Common
    {
        /// <summary>
        /// Checks that a file exists, else throws an ApplicativeException.
        /// </summary>
        /// <param name="fileFullPath_"></param>
        /// <param name="context_"></param>
        internal static void CheckFile(string fileFullPath_, string context_)
        {
            if (!File.Exists(fileFullPath_))
            {
                Logger.Log(TraceEventType.Critical, "ConfigurationLoader: XML templates definitions analysis error. Throw message : " + fileFullPath_ + " : file " + context_ + " does not exist.");
                
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
            int count = CountPlaceholders(stringWithPlaceholders_);
            return count == lstValuesForPlaceholders_.Count;
        }
    }
}
