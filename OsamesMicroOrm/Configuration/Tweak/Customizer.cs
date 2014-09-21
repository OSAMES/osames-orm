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
using System.Configuration;
using System.Diagnostics;

namespace OsamesMicroOrm.Configuration.Tweak
{
#if DEBUG
    /// <summary>
    /// To tweak configuration.
    /// Only available in debug mode
    /// </summary>
    public static class Customizer
    {
        /// <summary>
        /// Dictionnaire de sauvegarde des valeurs de AppSettings avant modification.
        /// </summary>
        private static readonly Dictionary<string, string> AppSettingsOriginalValues = new Dictionary<string, string>();

        /// <summary>
        /// Permet de modifier une clé dans ConfigurationManager.AppSettings.
        /// <para>Si la clé modifiée ne fait pas partie de l'énum de la classe Customizer alors aucun n'appel de ConfigurationLoader.Clear() n'est fait</para>
        /// </summary>
        /// <param name="key_">Key for ConfigurationManager.AppSettings</param>
        /// <param name="keyValue_">Value for ConfigurationManager.AppSettings</param>
        /// <param name="customLogger_">if not null, TraceSource logger to use instead of default ORM TraceSource logger</param>
        public static void ConfigurationManagerSetKeyValue(string key_, string keyValue_, TraceSource customLogger_ = null)
        {
            if (string.IsNullOrEmpty(ConfigurationManager.AppSettings[key_]))
            {
                Log(customLogger_, "Cannot set value for ConfigurationManager.AppSettings key '" + key_ + "', it's not defined, nothing to override", true);
                return;
            }

            if (!AppSettingsOriginalValues.ContainsKey(key_))
            {
                AppSettingsOriginalValues.Add(key_,ConfigurationManager.AppSettings[key_]);
                Log(customLogger_, "Changing ConfigurationManager.AppSettings key '" + key_ + "' from '" + ConfigurationManager.AppSettings[key_] + "' to '" + keyValue_ + "'", false);
                ConfigurationManager.AppSettings[key_] = keyValue_;

                // Rechargement de la configuration
                ConfigurationLoader.Clear();
            }
            else
            {
                Log(customLogger_, "Key [" + key_ + "] already changed. Current value : " + ConfigurationManager.AppSettings[key_], false);
            }
        }

        /// <summary>
        /// Restore une clé précise dans ConfigurationManager.AppSettings
        /// </summary>
        /// <param name="key_">Nom de la clé à restorer</param>
        /// <param name="customLogger_">if not null, TraceSource logger to use instead of default ORM TraceSource logger</param>
        public static void ConfigurationManagerRestoreKey(string key_, TraceSource customLogger_ = null)
        {
            if (AppSettingsOriginalValues.ContainsKey(key_))
            {
                ConfigurationManager.AppSettings[key_] = AppSettingsOriginalValues[key_];

                // Rechargement de la configuration
                ConfigurationLoader.Clear();

                AppSettingsOriginalValues.Remove(key_);
                Log(customLogger_, "Key [" + key_ + "] was restored to original value", false);
            }
            else
            {
                Log(customLogger_, "Key [" + key_ + "] has no original value. Can't change it back", false);
            }
        }

        /// <summary>
        /// Restore toute les clés changées dans ConfigurationManager.AppSettings
        /// </summary>
        public static void ConfigurationManagerRestoreAllKeys()
        {
            foreach (var key in AppSettingsOriginalValues.Keys)
            {
                ConfigurationManager.AppSettings[key] = AppSettingsOriginalValues[key];
            }
            AppSettingsOriginalValues.Clear();

            // Rechargement de la configuration
            ConfigurationLoader.Clear();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="customLogger_">if not null, TraceSource logger to use instead of default ORM TraceSource logger</param>
        /// <param name="message_">Message</param>
        /// <param name="error_">True for error, false for info</param>
        private static void Log(TraceSource customLogger_, string message_, bool error_)
        {
            if (customLogger_ == null)
            {
                ConfigurationLoader.LoggerTraceSource.TraceEvent(error_ ? TraceEventType.Error : TraceEventType.Information, 0, message_);
            }
            else
            {
                customLogger_.TraceEvent(error_ ? TraceEventType.Error : TraceEventType.Information, 0, message_);
            }
        }

        /// <summary>
        /// Clés qu'on va souvent modifier dans AppSettings (fichier OsamesOrm.config) via la méthode ConfigurationManagerSetKeyValue de la classe courante.
        /// Pour les tests unitaires par exemple.
        /// </summary>
        public enum AppSettingsKeys
        {
            // ReSharper disable InconsistentNaming

            /// <summary>
            /// Connexion DB active
            /// </summary>
            activeDbConnection,
            /// <summary>
            /// Fichier XML des templates
            /// </summary>
            sqlTemplatesFileName,
            /// <summary>
            /// Fichier XML du mapping
            /// </summary>
            mappingFileName,
            /// <summary>
            /// Nom de la db a utiliser
            /// </summary>
            dbName,
            /// <summary>
            /// Mot de passe de la db
            /// </summary>
            dbPassword,
            /// <summary>
            /// Chemin vers la db
            /// </summary>
            dbPath,
            /// <summary>
            /// Dossier contenant la configuration de l'orm
            /// </summary>
            configurationFolder,
            /// <summary>
            /// Dossier contenant les schemas xml de l'orm
            /// </summary>
            xmlSchemasFolder
            // ReSharper restore InconsistentNaming
        }
    }
#endif
}
