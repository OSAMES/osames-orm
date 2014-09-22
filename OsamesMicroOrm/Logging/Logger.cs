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
using System.Diagnostics;

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
        internal static void Log(TraceEventType logLevel_, string message_)
        {
            SimpleTraceSource.TraceEvent(logLevel_, 0, message_);
        }

        /// <summary>
        /// Log d'une exception détaillée dans le log détaillé.
        /// Ceci logguera en plus dans le log standard uniquement le message de l'exception et qu'il faut regarder dans le log détaillé.
        /// </summary>
        /// <param name="logLevel_">Niveau de log</param>
        /// <param name="error_">Exception</param>
        internal static void Log(TraceEventType logLevel_, Exception error_)
        {
            SimpleTraceSource.TraceEvent(logLevel_, 0, error_.Message);
            // TODO compléter le texte loggué avec l'info qu'il faut lire le log détaillé.

            DetailedTraceSource.TraceEvent(logLevel_, 0, error_.Message + " " + error_.StackTrace);
        }

        /// <summary>
        /// Vérifie si les loggers sont définis dans App.config comme il faut.
        /// Instancier les TraceSource d'après la configuration/la règle en cas de configuration incomplète.
        /// </summary>
        private static void CheckLoggerConfiguration()
        {
            SimpleTraceSource = new TraceSource("osamesOrmTraceSource");
            if (SimpleTraceSource.Listeners.Count == 0)
            {
                SimpleTraceSource.Switch.Level = SourceLevels.All;
                SimpleTraceSource.Listeners.Add(new EventLogTraceListener());
            }
            DetailedTraceSource = new TraceSource("osamesOrmDetailedTraceSource");
            if (DetailedTraceSource.Listeners.Count == 0)
            {
                DetailedTraceSource.Switch.Level = SourceLevels.All;
                DetailedTraceSource.Listeners.Add(new EventLogTraceListener());
            }

        }

    }
}
