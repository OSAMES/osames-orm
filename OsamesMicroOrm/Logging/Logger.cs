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
using System.Text;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace OsamesMicroOrm.Logging
{
    /// <summary>
    /// Logging basé sur System.Diagnotics.
    /// </summary>
    internal static class Logger
    {
        static Logger()
        {
            CheckLoggerConfiguration();
        }

        /// <summary>
        /// Generic logging trace source that traces error messages only.
        /// </summary>
        internal static TraceSource SimpleTraceSource;

        /// <summary>
        /// Specific logging trace source that traces error messages and stacktraces.
        /// </summary>
        internal static TraceSource DetailedTraceSource;

        /// <summary>
        /// Log d'une chaîne dans le log standard.
        /// </summary>
        /// <param name="logLevel_">Niveau de log</param>
        /// <param name="message_">Texte</param>
        /// <param name="memberName"></param>
        /// <param name="sourceFilePath"></param>
        /// <param name="sourceLineNumber"></param>
        internal static void Log(TraceEventType logLevel_, string message_, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            string contextualInfo = FormatContextualInformation(memberName, sourceFilePath, sourceLineNumber);
            string messageToLog = contextualInfo + message_;
            SimpleTraceSource.TraceEvent(logLevel_, 0, messageToLog);
        }

        /// <summary>
        /// Log d'une exception détaillée dans le log détaillé.
        /// Ceci logguera en plus dans le log standard uniquement le message de l'exception et qu'il faut regarder dans le log détaillé.
        /// </summary>
        /// <param name="logLevel_">Niveau de log</param>
        /// <param name="error_">Exception</param>
        /// <param name="memberName"></param>
        /// <param name="sourceFilePath"></param>
        /// <param name="sourceLineNumber"></param>
        internal static void Log(TraceEventType logLevel_, Exception error_, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            string contextualInfo = FormatContextualInformation(memberName, sourceFilePath, sourceLineNumber);
            string messageToLog = contextualInfo + error_.Message;

            SimpleTraceSource.TraceEvent(logLevel_, 0, messageToLog);
            // TODO compléter le texte loggué avec l'info qu'il faut lire le log détaillé.

            DetailedTraceSource.TraceEvent(logLevel_, 0, messageToLog + " " + error_.StackTrace);
        }

        /// <summary>
        /// Formatage lisible des informations contextuelles de l'appelant de la méthode de log.
        /// </summary>
        /// <param name="memberName"></param>
        /// <param name="sourceFilePath"></param>
        /// <param name="sourceLineNumber"></param>
        /// <returns></returns>
        private static string FormatContextualInformation(string memberName, string sourceFilePath, int sourceLineNumber)
        {
            StringBuilder logMessage = new StringBuilder();
            logMessage.Append("Calling method : '");
            logMessage.Append(memberName);
            logMessage.Append("()' in source file '");
            logMessage.Append(sourceFilePath);
            logMessage.Append("' at line number '");
            logMessage.Append(sourceLineNumber);
            logMessage.Append("' |---| ");

            return logMessage.ToString();
        }

        /// <summary>
        /// Vérifie si les loggers sont définis dans App.config comme il faut.
        /// Instancier les TraceSource d'après la configuration/la règle en cas de configuration incomplète.
        /// </summary>
        private static void CheckLoggerConfiguration()
        {
            SimpleTraceSource = new TraceSource("osamesOrmTraceSource");
            DetailedTraceSource = new TraceSource("osamesOrmDetailedTraceSource");

            // Si absence de configuration XML, alors un seul listener est présent, du type DefaultTraceListener

            if (SimpleTraceSource.Listeners.Count == 1 && SimpleTraceSource.Listeners[0] is DefaultTraceListener)
                AddEventLogListener(SimpleTraceSource);

            if (DetailedTraceSource.Listeners.Count == 1 && DetailedTraceSource.Listeners[0] is DefaultTraceListener)
                AddEventLogListener(DetailedTraceSource);
        }

        /// <summary>
        /// Configure un TraceSource pour lui adjoindre un listener de type EventLogTraceListener.
        /// </summary>
        /// <param name="source_"></param>
        private static void AddEventLogListener(TraceSource source_)
        {
            // source switch 
            source_.Switch = new SourceSwitch("mySwitch", "my switch")
                {
                    Level = SourceLevels.All
                };
            // listeners
            source_.Listeners.Clear();
            // tracer dans la section Application des évènements Windows
            source_.Listeners.Add(new EventLogTraceListener("Application")
                {
                    Filter = new EventTypeFilter(SourceLevels.All)
                });

        }

    }
}
