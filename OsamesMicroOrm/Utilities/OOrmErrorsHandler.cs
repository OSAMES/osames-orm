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
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace OsamesMicroOrm.Utilities
{
    internal static class OOrmErrorsHandler
    {
        private static KeyValuePair<ErrorType, string> ErrorMsg;
        internal static Dictionary<string, KeyValuePair<string, string>> HResultCode = new Dictionary<string, KeyValuePair<string, string>>();

        /// <summary>
        /// Cosntructor
        /// </summary>
        static OOrmErrorsHandler()
        {
            ReadHResultCodesFromResources("HResult Orm.csv", out HResultCode);
        }

        private static void ReadHResultCodesFromResources(string resource_, out Dictionary<string, KeyValuePair<string, string>> hresultCodes_)
        {
            hresultCodes_ = new Dictionary<string, KeyValuePair<string, string>>();

            using (Stream str = Assembly.GetExecutingAssembly().GetManifestResourceStream(typeof(OOrmErrorsHandler).Assembly.GetName().Name + ".Resources." + resource_))
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
                        if (string.IsNullOrWhiteSpace(row[1].Substring(1, row[1].Length - 2)) || string.IsNullOrWhiteSpace(row[0].Substring(1, row[0].Length - 2)))
                            continue;
                        hresultCodes_.Add(row[1].Substring(1, row[1].Length - 2), new KeyValuePair<string, string>(row[0].Substring(1, row[0].Length - 2), row[2].Substring(1, row[2].Length - 2)));
                    }
                }
            }
        }

        /// <summary>
        /// return a key value pair with hresult name and description
        /// </summary>
        /// <param name="code_"></param>
        /// <returns></returns>
        internal static string FindHResultByCode(string code_)
        {
            //int temporyvar = (int) new System.ComponentModel.Int32Converter().ConvertFromString(code_);
            if (HResultCode.ContainsKey(code_))
                return string.Format("{0} ({1})", HResultCode[code_].Value, HResultCode[code_].Key);
            return string.Format("No code {0} found.", code_);
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
