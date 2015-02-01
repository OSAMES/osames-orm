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
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Principal;
using System.Windows.Forms;
using OsamesMicroOrm.Configuration;
using OsamesMicroOrm.Logging;

namespace OsamesMicroOrm.Utilities
{
    /// <summary>
    /// Boîte à outils pour la gestion des messages d'erreurs.
    /// </summary>
    internal class OOrmErrorsHandler
    {
        private const string SSource = "OsamesORM";
        private const string SNetRuntimeSource = "Application Error"; //".NET Runtime";
        private const string SLog = "Application";
        string sEvent;

        private static bool HasAdminPrivileges;
        private static string SActiveSource;

        /// <summary>
        /// Dictionnaire interne des erreurs au format suivant : clé : code d'erreur "E_XXX". Valeur : code HRESULT "0x1234" et texte.
        /// </summary>
        internal static readonly Dictionary<string, KeyValuePair<string, string>> HResultCode = new Dictionary<string, KeyValuePair<string, string>>();

        /// <summary>
        /// Cosntructor
        /// </summary>
        static OOrmErrorsHandler()
        {
            HasAdminPrivileges = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
            // Source active : si admin, source custom, sinon une source existante.
            SActiveSource = HasAdminPrivileges ? SSource : SNetRuntimeSource;
            Logger.Log(TraceEventType.Information, "If logging to event log is enabled (see configuration), used souce will be: " + SActiveSource);
            ReadHResultCodesFromResources("HResult Orm.csv", out HResultCode);
        }

        /// <summary>
        /// Lis les hresult dans la resource embarquée de l'ORM et places ces données dans un dictionnaire.
        /// </summary>
        /// <param name="resource_"></param>
        /// <param name="hresultCodes_"></param>
        /// <exception cref="Exception"></exception>
        private static void ReadHResultCodesFromResources(string resource_, out Dictionary<string, KeyValuePair<string, string>> hresultCodes_)
        {
            using (Stream str = Assembly.GetExecutingAssembly().GetManifestResourceStream(typeof(OOrmErrorsHandler).Assembly.GetName().Name + ".Resources." + resource_))
            {
                hresultCodes_ = new Dictionary<string, KeyValuePair<string, string>>();
                //null if resource doesn't exist
                if (str == null)
                    return;

                using (StreamReader sr = new StreamReader(str))
                {
                    //Skip csv header
                    sr.ReadLine();

                    string currentLine;
                    int currentLineNumber = 0;
                    // currentLine will be null when the StreamReader reaches the end of file

                    while ((currentLine = sr.ReadLine()) != null)
                    {
                        currentLineNumber++;
                        //Insert to kvp
                        string[] row = currentLine.Split(';');
                        if (row.Length > 0 && row.Length < 2)
                            throw new Exception(string.Format("Incorrect line ({0}) : needs at least E_CODE and HRESULT hexa code", currentLineNumber));
                        string eCode = row[1].Substring(1, row[1].Length - 2);
                        string hexCode = row[0].Substring(1, row[0].Length - 2);
                        if (string.IsNullOrWhiteSpace(eCode) || string.IsNullOrWhiteSpace(hexCode))
                            continue;
                        hresultCodes_.Add(eCode.ToUpperInvariant(), new KeyValuePair<string, string>(hexCode, row[2].Substring(1, row[2].Length - 2)));
                    }
                }
            }
        }

        /// <summary>
        /// Retourne une KeyValuePair avec : comme clé, le code hresult au format int au lieu de chaîne hexadécimale, comme valeur : le descriptif assocé au code.
        /// </summary>
        /// <param name="code_"></param>
        /// <returns></returns>
        internal static KeyValuePair<int, string> FindHResultAndDescriptionByCode(HResultEnum code_)
        {
            string code = code_.ToString().ToUpperInvariant();

            int hexaCode = -1;
            string description = string.Format("No code {0} found.", code_);

            if (HResultCode.ContainsKey(code))
            {
                // On enlève le "0x" en début de chaîne et ceci est la bonne façon de convertir
                hexaCode = Int32.Parse(HResultCode[code].Key.Substring(2), System.Globalization.NumberStyles.HexNumber);
                description = string.Format("{0} ({1})", HResultCode[code].Value, HResultCode[code].Key);
            }

            return new KeyValuePair<int, string>(hexaCode, description);
        }

        /// <summary>
        /// Permet d'affficher les erreurs pour un contexte de type winform ou wpf
        /// </summary>
        internal static void DisplayErrorMessageWinforms(ErrorType errorType_, string errorMessage_)
        {
            MessageBox.Show(errorMessage_, "ORM Message", MessageBoxButtons.OK, (MessageBoxIcon)errorType_);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="errorMessage_"></param>
        /// <param name="additionalErrorMsg_"></param>
        /// <returns></returns>
        internal static string FormatCustomerError(string errorMessage_, string additionalErrorMsg_ = null)
        {
            return string.Format(DateTime.Now + " :: {0} : {1}", errorMessage_, string.IsNullOrWhiteSpace(additionalErrorMsg_) ? "No additional information" : additionalErrorMsg_);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hresultCode_"></param>
        /// <param name="errorType_"></param>
        /// <param name="additionalErrorMsg_"></param>
        internal static void WriteToWindowsEventLog(HResultEnum hresultCode_, EventLogEntryType errorType_, string additionalErrorMsg_ = null)
        {
            string code = hresultCode_.ToString().ToUpperInvariant();
            KeyValuePair<string, string> hresultPair = HResultCode.ContainsKey(code)
                ? HResultCode[code]
                : new KeyValuePair<string, string>("n/a", "n/a");
            string errorMessage = hresultPair.Value + additionalErrorMsg_;

            WindowsImpersonationContext wic = null;

            if (!HasAdminPrivileges)
            {
                // Impersonation pour élever les privilèges
                wic = WindowsIdentity.Impersonate(IntPtr.Zero);
            }
            else
            {
                // En admin réel nous pouvons créer une source, avec l'impersonation nous ne le pouvons pas.
                if (!EventLog.SourceExists(SActiveSource))
                    EventLog.CreateEventSource(SActiveSource, SLog);
            }

            EventLog.WriteEntry(SActiveSource, errorMessage, errorType_, 1);

            if (!HasAdminPrivileges)
                wic.Undo();
        }

        /// <summary>
        /// </summary>
        /// <param name="hresultCode_"></param>
        /// <param name="additionalErrorMsg_"></param>
        /// <param name="errorType_"></param>
        /// <returns>KeyValuePair avec pour la clé le code hresult au format int au lieu de chaîne hexa "0xNNNN" et pour la valeur le message d'erreur complètement formaté à retourner à l'utilisateur</returns>
        /// <exception cref="IOException">An I/O error occurred. </exception>
        /// <exception cref="ArgumentNullException"><paramref name="format" /> is null. </exception>
        /// <exception cref="FormatException">The format specification in <paramref name="format" /> is invalid. </exception>
        internal static KeyValuePair<int, string> ProcessOrmException(HResultEnum hresultCode_, ErrorType errorType_ = ErrorType.ERROR, string additionalErrorMsg_ = null)
        {
            // Ecrire dans event log n'est possible qu'en admin sinon on utilisera le log classique uniquement.
            // Le test d'être admin doit être fait à l'initialisation de l'ORM.
            //TODO si un jour on décide de faire exécuter en mode admin l'orm, mais il faut de toute manière créer une clé dans le registre pour la catégorie dans le eventlog de windows.

            //disabling write to windows log
            //if (writeToWindowsEventLog_)
            //    WriteToWindowsEventLog(errorCode_, errorType_, additionalErrorMsg_);

            KeyValuePair<int, string> hresultCodeHexaAndDescription = FindHResultAndDescriptionByCode(hresultCode_);
            string errorDescription = hresultCodeHexaAndDescription.Value;

            Logger.Log((TraceEventType)errorType_, FormatCustomerError(errorDescription, additionalErrorMsg_));

            switch (ConfigurationLoader.GetOrmContext)
            {
                case 1:  // console
                    Console.WriteLine(errorDescription, additionalErrorMsg_);
                    break;
                case 2: // winform - wpf
                    DisplayErrorMessageWinforms(errorType_, string.Format("{0}\r\nAdditionnal information: {1}", errorDescription, additionalErrorMsg_));
                    break;
            }
            return new KeyValuePair<int, string>(hresultCodeHexaAndDescription.Key, FormatCustomerError(errorDescription, additionalErrorMsg_));
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal enum ErrorType
    {
        // ReSharper disable InconsistentNaming
        CRITICAL = MessageBoxIcon.Stop,  // for MessageBoxIcon it's MessageBoxIcon.Stop
        ERROR = MessageBoxIcon.Error,
        WARNING = MessageBoxIcon.Warning,
        INFORMATION = MessageBoxIcon.Information,
        // ReSharper enable InconsistentNaming
    }
}
