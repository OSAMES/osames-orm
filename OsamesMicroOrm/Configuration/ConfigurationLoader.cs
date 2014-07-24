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
using System.Diagnostics;
using System.Linq;
using System.Xml.XPath;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Xml;
using OsamesMicroOrm.Utilities;

namespace OsamesMicroOrm.Configuration
{
    /// <summary>
    /// This class is dedicated to reading configuration files: app.config and specific files in Config subdirectory.
    /// Then data is loaded into internal directories.
    /// There are also overloads to allow configuration bypass, for example in unit tests where it's easier to call an overload to use another XML template file. 
    /// </summary>
    public class ConfigurationLoader
    {
        private static ConfigurationLoader _singleton;
        private static readonly object _oSingletonInit = new object();

        /// <summary>
        /// Templates dictionary for select
        /// </summary>
        public static readonly Dictionary<string, string> DicInsertSql = new Dictionary<string, string>();

        /// <summary>
        /// Templates dictionary for update
        /// </summary>
        public static readonly Dictionary<string, string> DicUpdateSql = new Dictionary<string, string>();

        /// <summary>
        /// Templates dictionary for select
        /// </summary>
        public static readonly Dictionary<string, string> DicSelectSql = new Dictionary<string, string>();

        /// <summary>
        /// Templates dictionary for delete
        /// </summary>
        public static readonly Dictionary<string, string> DicDeleteSql = new Dictionary<string, string>();

        /// <summary>
        /// Mapping is stored as follows : an external dictionary and an internal dictionary.
        /// External dictionary : key is "clients" for example, value is a set of property name/column name correspondance.
        /// Property (dictionary key) and column name (dictionary value) are stored in the internal dictionary.
        /// </summary>
        internal static readonly Dictionary<string, Dictionary<string, string>> MappingDictionnary = new Dictionary<string, Dictionary<string, string>>();

        /// <summary>
        /// Generic logging trace source that traces error messages only.
        /// </summary>
        internal static TraceSource _loggerTraceSource = new TraceSource("osamesOrmTraceSource");

        /// <summary>
        /// Specific logging trace source that traces error messages and stacktraces.
        /// </summary>
        internal static TraceSource _detailedLoggerTraceSource = new TraceSource("osamesOrmDetailedTraceSource");

        /// <summary>
        /// Private constructor for singleton.
        /// </summary>
        private ConfigurationLoader()
        {
        }

        /// <summary>
        /// Loads configuration
        /// </summary>
        private void LoadConfiguration()
        {
            LoadXmlConfiguration();
            InitializeDatabaseConnection();
        }

        /// <summary>
        /// Singleton access. Creates an empty object once.
        /// </summary>
        public static ConfigurationLoader Instance
        {
            get
            {
                lock (_oSingletonInit)
                {
                    if (_singleton != null)
                        return _singleton;

                    _singleton = new ConfigurationLoader();
                    _singleton.LoadConfiguration();
                    return _singleton;

                }
            }
        }

        /// <summary>
        /// Clears internal singleton, forcing reload to next call to "Instance".
        /// Useful for unit tests.
        /// </summary>
        public static void Clear()
        {
            lock (_oSingletonInit)
            {
                _singleton = null;
            }
        }

        /// <summary>
        /// Initialize active connection string values from configuration and setup DbHelper.
        /// </summary>
        /// <returns>false when configuration is wrong</returns>
        internal bool InitializeDatabaseConnection()
        {
            string dbPath = string.Format("{0}{1}", Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), ConfigurationManager.AppSettings[@"dbPath"]);
            string dbName = ConfigurationManager.AppSettings["dbName"];
            if (string.IsNullOrWhiteSpace(dbName))
            {
                _loggerTraceSource.TraceEvent(TraceEventType.Critical, 0, "No database name defined in appSettings ('dbName')");
                return false;
            }

            string dbPassword = ConfigurationManager.AppSettings["dbPassword"];

            string dbConnexion = ConfigurationManager.AppSettings["activeDbConnection"];
            if (string.IsNullOrWhiteSpace(dbConnexion))
            {
                _loggerTraceSource.TraceEvent(TraceEventType.Critical, 0, "No active connection name defined in appSettings ('activeDbConnection')");
                return false;
            }
            var activeConnection = ConfigurationManager.ConnectionStrings[dbConnexion];
            if (activeConnection == null)
            {
                _loggerTraceSource.TraceEvent(TraceEventType.Critical, 0, string.Format("Active connection not found in available connection strings (key : '{0}'", dbConnexion));
                return false;
            }
            string conn = activeConnection.Name;
            if (string.IsNullOrWhiteSpace(conn))
            {
                _loggerTraceSource.TraceEvent(TraceEventType.Critical, 0, string.Format("No active connection name defined in appSettings for active connection '{0}'", dbConnexion));
                return false;
            }

            string provider = activeConnection.ProviderName;
            if (string.IsNullOrWhiteSpace(provider))
            {
                _loggerTraceSource.TraceEvent(TraceEventType.Critical, 0, string.Format("No active connection provider defined in appSettings  for active connection '{0}'", dbConnexion));
                return false;
            }

            // Some database connection definition don't need a database path
            if (!string.IsNullOrWhiteSpace(dbPath))
                conn = (ConfigurationManager.ConnectionStrings[dbConnexion].ToString().Replace(@"$dbPath", dbPath));

            conn = conn.Replace("$dbName", dbName);

            _loggerTraceSource.TraceEvent(TraceEventType.Information, 0, string.Format("Using DB connection string: {0}", conn));

            // Some database connection definition don't need a database password
            if (!string.IsNullOrWhiteSpace(dbPassword))
                conn = conn.Replace("$dbPassword", dbPassword);

            // Now pass information to DbHelper
            DbManager.ConnectionString = conn;
            DbManager.ProviderName = provider;

            return true;

        }


        /// <summary>
        /// Reads configuration from appSettings then load specific configuration files to internal dictionaries.
        /// </summary>
        private void LoadXmlConfiguration()
        {
            // 1. Load ORM Configuration File

            // Format path for loading the xsd schemas file
            string xsdSchemasPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppSettings["xmlSchemasFolder"].TrimStart('\\').TrimStart('/'));

            try
            {
                _loggerTraceSource.TraceEvent(TraceEventType.Information, 0, "Osames ORM Initializing...");

                // Format path for loading the configuration file
                string configBaseDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppSettings["configurationFolder"].TrimStart('\\').TrimStart('/'));

                // Concatenate path and xml file name for template
                string sqlTemplatesFullPath = Path.Combine(configBaseDirectory, ConfigurationManager.AppSettings["sqlTemplatesFileName"]);

                // Concatenate path and xml file name for mapping
                string sqlMappingsFullPath = Path.Combine(configBaseDirectory, ConfigurationManager.AppSettings["mappingFileName"]);

                // Get for templates and mapping files their root tag prefix and namespace.
                string[] xmlPrefix = new string[2];
                string[] xmlNamespaces = new string[2];
                XPathNavigator xmlTemplatesNavigator = XmlTools.GetRootTagInfos(sqlTemplatesFullPath, out xmlPrefix[0], out xmlNamespaces[0]);
                XPathNavigator xmlMappingNavigator = XmlTools.GetRootTagInfos(sqlMappingsFullPath, out xmlPrefix[1], out xmlNamespaces[1]);

                // Create Xml Validator passing namespaces. This also checks if the xsd files exist.
                XmlValidator xmlvalidator = new XmlValidator(new[] { xmlNamespaces[0], xmlNamespaces[1] },
                                new[] { Path.Combine(xsdSchemasPath, ConfigurationManager.AppSettings["sqlTemplatesSchemaFileName"]), 
                                         Path.Combine(xsdSchemasPath, ConfigurationManager.AppSettings["mappingSchemaFileName"])
                                 });

                // Validate SQL Templates and Mapping
                xmlvalidator.ValidateXml(new[] { sqlTemplatesFullPath, sqlMappingsFullPath });

                _loggerTraceSource.TraceEvent(TraceEventType.Information, 0, "Osames ORM Initialized.");

                // 2. Load provider specific string
                string dbConnexionName = ConfigurationManager.AppSettings["activeDbConnection"];
                LoadProviderString(xmlTemplatesNavigator, xmlPrefix[0], xmlNamespaces[0], dbConnexionName);

                // 3. Load SQL Templates
                FillTemplatesDictionaries(xmlTemplatesNavigator, xmlPrefix[0], xmlNamespaces[0]);

                // 4. Load mapping definitions
                FillMappingDictionary(xmlMappingNavigator, xmlPrefix[1], xmlNamespaces[1]);


            }
            catch (Exception ex)
            {
                _loggerTraceSource.TraceEvent(TraceEventType.Critical, 0, "ConfigurationLoader LoadXmlConfiguration, see detailed log");
                _detailedLoggerTraceSource.TraceEvent(TraceEventType.Critical, 0, "ConfigurationLoader : " + ex);
                throw;
            }
        }
        /// <summary>
        /// Assigns a value to DbManager.SelectLastInsertIdCommandText according to active DB connection string.
        /// </summary>
        /// <param name="xmlNavigator_"></param>
        /// <param name="xmlRootTagPrefix_"></param>
        /// <param name="xmlRootTagNamespace_"></param>
        /// <param name="activeDbConnectionName_"></param>
        internal static void LoadProviderString(XPathNavigator xmlNavigator_, string xmlRootTagPrefix_, string xmlRootTagNamespace_, string activeDbConnectionName_)
        {
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmlNavigator_.NameTable);
            nsmgr.AddNamespace(xmlRootTagPrefix_, xmlRootTagNamespace_);

            if (string.IsNullOrWhiteSpace(activeDbConnectionName_))
            {
                _loggerTraceSource.TraceEvent(TraceEventType.Critical, 0, "No active connection name defined in appSettings ('activeDbConnection')");
                return;
            }

            var activeConnection = ConfigurationManager.ConnectionStrings[activeDbConnectionName_];
            if (activeConnection == null)
            {
                _loggerTraceSource.TraceEvent(TraceEventType.Critical, 0, string.Format("Active connection not found in available connection strings (key : '{0}'", activeDbConnectionName_));
                return;
            }
            string conn = activeConnection.Name;
            if (string.IsNullOrWhiteSpace(conn))
            {
                _loggerTraceSource.TraceEvent(TraceEventType.Critical, 0, string.Format("No active connection name defined in appSettings for active connection '{0}'", activeDbConnectionName_));
                return;
            }
            string provider = activeConnection.ProviderName;
            if (string.IsNullOrWhiteSpace(provider))
            {
                _loggerTraceSource.TraceEvent(TraceEventType.Critical, 0, string.Format("No active connection provider defined in appSettings  for active connection '{0}'", activeDbConnectionName_));
                return;
            }
            // name and provider
            string strXPath = string.Format("/*/{0}:ProviderSpecific/{0}:Select[@name='getlastinsertid' and @provider='{1}']", xmlRootTagPrefix_, provider);
            XPathNodeIterator iter = xmlNavigator_.Select(strXPath, nsmgr);
            if (iter.MoveNext())
            {
                DbManager.SelectLastInsertIdCommandText = iter.Current.Value;
            }
            else
            {
                _loggerTraceSource.TraceEvent(TraceEventType.Critical, 0, "ConfigurationLoader LoadProviderString, no value matching XPath " + strXPath);
            }
        }

        /// <summary>
        /// Loads XML file which contains mapping definitions to internal dictionary.
        /// </summary>
        /// <param name="xmlNavigator_">Reused XPathNavigator instance</param>
        /// <param name="xmlRootTagPrefix_"> </param>
        /// <param name="xmlRootTagNamespace_"> </param>
        internal static void FillMappingDictionary(XPathNavigator xmlNavigator_, string xmlRootTagPrefix_, string xmlRootTagNamespace_)
        {
            MappingDictionnary.Clear();
            try
            {
                XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmlNavigator_.NameTable);
                nsmgr.AddNamespace(xmlRootTagPrefix_, xmlRootTagNamespace_);
                // Table nodes
                XPathNodeIterator iter = xmlNavigator_.Select(string.Format("/*/{0}:Table", xmlRootTagPrefix_), nsmgr);

                var propertyColumnDictionary = new Dictionary<string, string>();
                while (iter.MoveNext()) // Read Table node
                {
                    MappingDictionnary.Add(iter.Current.GetAttribute("name", ""), propertyColumnDictionary);
                    iter.Current.MoveToFirstChild();
                    do
                    {
                        propertyColumnDictionary.Add(iter.Current.GetAttribute("property", ""), iter.Current.GetAttribute("column", ""));
                    } while (iter.Current.MoveToNext()); // Read next Mapping node

                    propertyColumnDictionary = new Dictionary<string, string>();
                }
            }
            catch (Exception ex)
            {
                _loggerTraceSource.TraceEvent(TraceEventType.Critical, 0, "ConfigurationLoader FillMappingDictionary, see detailed log");
                _detailedLoggerTraceSource.TraceEvent(TraceEventType.Critical, 0, "ConfigurationLoader: XML mapping definitions analyzis error: " + ex);
                throw;
            }
        }

        /// <summary>
        /// Loads XML file which contains templates definitions to internal dictionary.
        /// </summary>
        /// <param name="xmlNavigator_">Reused XPathNavigator instance</param>
        /// <param name="xmlRootTagPrefix_"> </param>
        /// <param name="xmlRootTagNamespace_"> </param>
        internal static void FillTemplatesDictionaries(XPathNavigator xmlNavigator_, string xmlRootTagPrefix_, string xmlRootTagNamespace_)
        {
            DicInsertSql.Clear();
            DicSelectSql.Clear();
            DicUpdateSql.Clear();
            DicDeleteSql.Clear();
            try
            {
                XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmlNavigator_.NameTable);
                nsmgr.AddNamespace(xmlRootTagPrefix_, xmlRootTagNamespace_);
                // Inserts nodes
                XPathNodeIterator iter = xmlNavigator_.Select(string.Format("/*/{0}:Inserts", xmlRootTagPrefix_), nsmgr);
                if (iter.MoveNext())
                    FillSqlTemplateDictionary(iter, DicInsertSql);
                // Selects nodes
                iter = xmlNavigator_.Select(string.Format("/*/{0}:Selects", xmlRootTagPrefix_), nsmgr);
                if (iter.MoveNext())
                    FillSqlTemplateDictionary(iter, DicSelectSql);
                // Updates nodes
                iter = xmlNavigator_.Select(string.Format("/*/{0}:Updates", xmlRootTagPrefix_), nsmgr);
                if (iter.MoveNext())
                    FillSqlTemplateDictionary(iter, DicUpdateSql);
                // Deletes nodes
                iter = xmlNavigator_.Select(string.Format("/*/{0}:Deletes", xmlRootTagPrefix_), nsmgr);
                if (iter.MoveNext())
                    FillSqlTemplateDictionary(iter, DicDeleteSql);

            }
            catch (Exception ex)
            {
                _loggerTraceSource.TraceEvent(TraceEventType.Critical, 0, "ConfigurationLoader FillTemplatesDictionaries, see detailed log");
                _detailedLoggerTraceSource.TraceEvent(TraceEventType.Critical, 0, "ConfigurationLoader: XML templates definitions analyzis error: " + ex);
                throw;
            }
        }

        /// <summary>
        /// Extracts information from XML data which contains templates definitions to internal dictionary.
        /// </summary>
        /// <param name="node_"></param>
        /// <param name="workDictionary_"></param>
        internal static void FillSqlTemplateDictionary(XPathNodeIterator node_, Dictionary<string, string> workDictionary_ = null)
        {
            if (workDictionary_ == null) return;

            node_.Current.MoveToFirstChild();
            do
            {
                string name = node_.Current.GetAttribute("name", "");
                if (workDictionary_.ContainsKey(name))
                    throw new Exception(string.Format("A 'name' attribute with value '{0}' has been defined more than one time, XML is invalid", name));
                workDictionary_.Add(name, node_.Current.Value);
            } while (node_.Current.MoveToNext());
        }

        #region mapping dictionary getter helpers

        /// <summary>
        /// Asks mapping for a DB column name.
        /// </summary>
        /// <param name="mappingKey_">Mapping key (DB table name)</param>
        /// <param name="propertyName_">(DB persistent object) peroperty name, ex "IdXXX"</param>
        /// <returns>Db column name, ex "id_xxx"</returns>
        public string GetMappingDbColumnName(string mappingKey_, string propertyName_)
        {
            Dictionary<string, string> mappingObjectSet;
            string resultColumnName;

            MappingDictionnary.TryGetValue(mappingKey_, out mappingObjectSet);
            if (mappingObjectSet == null)
                throw new Exception(string.Format("No mapping for key '{0}'", mappingKey_));
            mappingObjectSet.TryGetValue(propertyName_, out resultColumnName);
            if (mappingObjectSet == null)
                throw new Exception(string.Format("No mapping for key '{0}' and property name '{1}'", mappingKey_, propertyName_));

            return resultColumnName;
        }

        /// <summary>
        /// Asks mapping for a (DB persistent object) property name.
        /// </summary>
        /// <param name="mappingKey_">Mapping key (DB table name)</param>
        /// <param name="dbColumnName_">DB column name, ex "id_xxx"</param>
        /// <returns>(Db persistent object) property name, ex "IdXXX"</returns>
        public string GetMappingPropertyName(string mappingKey_, string dbColumnName_)
        {
            Dictionary<string, string> mappingObjectSet;

            MappingDictionnary.TryGetValue(mappingKey_, out mappingObjectSet);
            if (mappingObjectSet == null)
                throw new Exception(string.Format("No mapping for key '{0}'", mappingKey_));
            string resultPropertyName = (from mapping in mappingObjectSet where mapping.Value == dbColumnName_ select mapping.Value).FirstOrDefault();

            if (resultPropertyName == null)
                throw new Exception(string.Format("No mapping for key '{0}' and DB column name '{1}'", mappingKey_, dbColumnName_));

            return resultPropertyName;
        }

        /// <summary>
        /// Asks for mapping set.
        /// </summary>
        /// <param name="mappingKey_">Mapping key (DB table name)</param>
        /// <returns>Mapping dictionary</returns>
        public Dictionary<string, string> GetMapping(string mappingKey_)
        {
            Dictionary<string, string> mappingObjectSet;

            MappingDictionnary.TryGetValue(mappingKey_, out mappingObjectSet);
            if (mappingObjectSet == null)
                throw new Exception(string.Format("No mapping for key '{0}'", mappingKey_));
            return mappingObjectSet;
        }

        #endregion

    }
}