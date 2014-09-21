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

namespace OsamesMicroOrm.Utilities
{
    internal static class ErrorsHandler
    {
        private static KeyValuePair<ErrorType, string> ErrorMsg;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="errortype"></param>
        /// <param name="message_"></param>
        internal static void AddErrorMessage(ErrorType errortype, string message_)
        {
            ErrorMsg = new KeyValuePair<ErrorType, string>(errortype, DateTime.Now + " :: " + message_);
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
