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
            // TODO

            SimpleTraceSource = new TraceSource("osamesOrmTraceSource");
            DetailedTraceSource = new TraceSource("osamesOrmDetailedTraceSource");

        }

    }
}
