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
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace OsamesMicroOrm.Utilities
{
    internal static class ErrorsHandler
    {
        private static KeyValuePair<ErrorType, string> ErrorMsg;
        internal static Dictionary<string, string> HResultCode = new Dictionary<string, string>();

        /// <summary>
        /// Cosntructor
        /// </summary>
        static ErrorsHandler()
        {
            ReadHResultCodeFromResources("HResult Orm.csv", out HResultCode);
        }

        private static void ReadHResultCodeFromResources(string resource_, out Dictionary<string, string> hresultCodes_)
        {
            hresultCodes_ = new Dictionary<string, string>();

            using (Stream str = Assembly.GetExecutingAssembly().GetManifestResourceStream(typeof(ErrorsHandler).Assembly.GetName().Name + ".Resources." + resource_))
            {
                //null if resource doesn't exist
                if (str == null)
                    return;

                string[] row = new string[2];

                using (StreamReader sr = new StreamReader(str))
                {
                    //Skip csv header
                    sr.ReadLine();

                    string currentLine;
                    // currentLine will be null when the StreamReader reaches the end of file

                    while ((currentLine = sr.ReadLine()) != null)
                    {
                        //Insert to kvp
                        row = currentLine.Split(';');
                        if (string.IsNullOrWhiteSpace(row[1]))
                            continue;
                        hresultCodes_.Add(row[1], row[0]);
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="errorType"></param>
        /// <param name="message_"></param>
        internal static void AddErrorMessage(ErrorType errorType, string message_)
        {
            ErrorMsg = new KeyValuePair<ErrorType, string>(errorType, DateTime.Now + " :: " + message_);
        }

        internal static void DisplayErrorMessageWinforms()
        {
            System.Windows.Forms.MessageBoxIcon errorBoxIcon;
            switch (ErrorMsg.Key)
            {
                    case ErrorType.CRITICAL: errorBoxIcon = System.Windows.Forms.MessageBoxIcon.Stop; break;
                    case ErrorType.ERROR: errorBoxIcon = System.Windows.Forms.MessageBoxIcon.Error; break;
                    case ErrorType.WARNING: errorBoxIcon = System.Windows.Forms.MessageBoxIcon.Warning; break;
                    default: errorBoxIcon = System.Windows.Forms.MessageBoxIcon.None; break;
            }
            System.Windows.Forms.MessageBox.Show(ErrorMsg.Value, "ORM Message", System.Windows.Forms.MessageBoxButtons.OK, errorBoxIcon);
        }
    }

    internal enum ErrorType
    {
        // ReSharper disable InconsistentNaming
        CRITICAL,
        ERROR,
        WARNING,
        // ReSharper enable InconsistentNaming
    }
}
