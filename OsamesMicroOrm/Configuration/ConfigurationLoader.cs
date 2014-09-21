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
using System.Text;
using System.Xml.XPath;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Xml;
using OsamesMicroOrm.Utilities;
using System.Data;
using System.Data.Common;

namespace OsamesMicroOrm.Configuration
{
    /// <summary>
    /// This class is dedicated to reading configuration files: app.config and specific files in Config subdirectory.
    /// Then data is loaded into internal directories.
    /// There are also overloads to allow configuration bypass, for example in unit tests where it's easier to call an overload to use another XML template file. 
    /// </summary>
    public class ConfigurationLoader
    {
        private static ConfigurationLoader Singleton;
        private static readonly object SingletonInitLockObject = new object();

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
        /// External dictionary : key is a database table name, value is dictionary.
        /// Internal dictionary : key is a C# object property name, value is a database column name.
        /// </summary>
        internal static readonly Dictionary<string, Dictionary<string, string>> MappingDictionnary = new Dictionary<string, Dictionary<string, string>>();

        /// <summary>
        /// Caractère d'échappement en début de nom de colonne dans le texte d'une requête SQL.
        /// </summary>
        internal static string StartFieldEncloser;

        /// <summary>
        /// Caractère d'échappement en fin de nom de colonne dans le texte d'une requête SQL.
        /// </summary>
        internal static string EndFieldEncloser;

        /// <summary>
        /// Generic logging trace source that traces error messages only.
        /// </summary>
        internal static TraceSource LoggerTraceSource = new TraceSource("osamesOrmTraceSource");

        /// <summary>
        /// Specific logging trace source that traces error messages and stacktraces.
        /// </summary>
        internal static TraceSource DetailedLoggerTraceSource = new TraceSource("osamesOrmDetailedTraceSource");

        /// <summary>
        /// Private constructor for singleton.
        /// </summary>
        private ConfigurationLoader()
        {
        }

        /// <summary>
        /// Singleton access. Creates an empty object once.
        /// </summary>
        public static ConfigurationLoader Instance
        {
            get
            {
                lock (SingletonInitLockObject)
                {
                    if (Singleton != null)
                        return Singleton;

                    Singleton = new ConfigurationLoader();
                    Singleton.LoadConfiguration();
                    return Singleton;

                }
            }
        }

        /// <summary>
        /// Clears internal singleton, forcing reload to next call to "Instance".
        /// Useful for unit tests.
        /// </summary>
        public static void Clear()
        {
            lock (SingletonInitLockObject)
            {
                Singleton = null;
            }
        }

        #region mapping dictionary getter helpers

        /// <summary>
        /// Asks mapping dictionary for a DB column name, given a Db entity property name.
        /// </summary>
        /// <param name="mappingDictionaryName_">Nom du dictionnaire de mapping à utiliser</param>
        /// <param name="propertyName_">DB entity C# object property name. Ex: "CustomerId"</param>
        /// <returns>DB column name. Ex: "id_customer"</returns>
        public string GetDbColumnNameFromMappingDictionary(string mappingDictionaryName_, string propertyName_)
        {
            Dictionary<string, string> mappingObjectSet;
            string resultColumnName;

            MappingDictionnary.TryGetValue(mappingDictionaryName_, out mappingObjectSet);
            if (mappingObjectSet == null)
                throw new Exception("No mapping for key '" + mappingDictionaryName_ + "'");
            mappingObjectSet.TryGetValue(propertyName_, out resultColumnName);
            if (mappingObjectSet == null)
                throw new Exception("No mapping for key '" + mappingDictionaryName_ + "' and property name '" + propertyName_ + "'");

            return resultColumnName;
        }

        /// <summary>
        /// Asks mapping dictionary for a Db entity object property name, given a DB column name.
        /// </summary>
        /// <param name="mappingDictionaryName_">Nom du dictionnaire de mapping à utiliser</param>
        /// <param name="dbColumnName_">DB column name. Ex: "id_customer"</param>
        /// <returns>DB entity C# object property name. Ex: "CustomerId"</returns>
        public string GetDbEntityPropertyNameFromMappingDictionary(string mappingDictionaryName_, string dbColumnName_)
        {
            Dictionary<string, string> mappingObjectSet;

            MappingDictionnary.TryGetValue(mappingDictionaryName_, out mappingObjectSet);
            if (mappingObjectSet == null)
                throw new Exception("No mapping for key '" + mappingDictionaryName_ + "'");
            string resultPropertyName = (from mapping in mappingObjectSet where mapping.Value == dbColumnName_ select mapping.Value).FirstOrDefault();

            if (resultPropertyName == null)
                throw new Exception("No mapping for key '" + mappingDictionaryName_ + "' and DB column name '" + dbColumnName_ + "'");

            return resultPropertyName;
        }

        /// <summary>
        /// Asks mapping dictionary for all mapping defined for a given table.
        /// </summary>
        /// <param name="mappingDictionaryName_">Nom du dictionnaire de mapping à utiliser</param>
        /// <returns>Mapping dictionary</returns>
        public Dictionary<string, string> GetMappingDefinitionsForTable(string mappingDictionaryName_)
        {
            Dictionary<string, string> mappingObjectSet;

            MappingDictionnary.TryGetValue(mappingDictionaryName_, out mappingObjectSet);
            if (mappingObjectSet == null)
                throw new Exception("No mapping for key '" + mappingDictionaryName_ + "'");
            return mappingObjectSet;
        }

        #endregion

        /// <summary>
        /// Initialize active connection string values from configuration and setup DbHelper.
        /// </summary>
        /// <returns>false when configuration is wrong</returns>
        internal bool InitializeDatabaseConnection()
        {
            string dbPath = string.Concat(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), ConfigurationManager.AppSettings[@"dbPath"]);
           
            // 1. AppSettings : doit définir une connexion DB active

            string dbConnexion = ConfigurationManager.AppSettings["activeDbConnection"];
            if (string.IsNullOrWhiteSpace(dbConnexion))
            {
                LoggerTraceSource.TraceEvent(TraceEventType.Critical, 0, "No active connection name defined in appSettings ('activeDbConnection')");
                return false;
            }

            // 2. Cette connexion DB doit être trouvée dans les ConnectionStrings définies dans la configuration (attribute "Name")

            var activeConnection = ConfigurationManager.ConnectionStrings[dbConnexion];
            if (activeConnection == null)
            {
                LoggerTraceSource.TraceEvent(TraceEventType.Critical, 0, "Active connection not found in available connection strings (key : '" + dbConnexion + "'");
                return false;
            }
          
            // 3. Un provider doit être défini (attribut "ProviderName")
            string provider = activeConnection.ProviderName;
            if (string.IsNullOrWhiteSpace(provider))
            {
                LoggerTraceSource.TraceEvent(TraceEventType.Critical, 0, "No provider name defined in connection strings configuration for connection with name '" + dbConnexion + "'");
                return false;
            }

            // 4. ce provider doit exister sur le système
            if (!FindInProviderFactoryClasses(provider))
            {
                LoggerTraceSource.TraceEvent(TraceEventType.Critical, 0, "Provider with name '" + provider +"' is not installed '");
                return false;
            }
            // 5. Une chaîne de connexion doit être définie (attribut "ConnectionString")
            string conn = activeConnection.ConnectionString;
            if (string.IsNullOrWhiteSpace(conn))
            {
                LoggerTraceSource.TraceEvent(TraceEventType.Critical, 0, "No connection string value defined in connection strings configuration for connection with name '" + dbConnexion + "'");
                return false;
            }

            // Some database connection definition don't need a database path
            if (!string.IsNullOrWhiteSpace(dbPath))
                conn = (activeConnection.ConnectionString.Replace(@"$dbPath", dbPath));

            // 6. Nom de la base de données

            string dbName = ConfigurationManager.AppSettings["dbName"];
            if (string.IsNullOrWhiteSpace(dbName))
            {
                LoggerTraceSource.TraceEvent(TraceEventType.Critical, 0, "No database name defined in appSettings ('dbName')");
                return false;
            }
            
            conn = conn.Replace("$dbName", dbName);

            // 7. Mot de passe optionnel de la base de données

            string dbPassword = ConfigurationManager.AppSettings["dbPassword"];
            // Some database connection definition don't need a database password
            if (!string.IsNullOrWhiteSpace(dbPassword))
                conn = conn.Replace("$dbPassword", dbPassword);
            
            LoggerTraceSource.TraceEvent(TraceEventType.Information, 0, "Using DB connection string: " + conn);
            
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
                LoggerTraceSource.TraceEvent(TraceEventType.Information, 0, "Osames ORM Initializing...");

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

                LoggerTraceSource.TraceEvent(TraceEventType.Information, 0, "Osames ORM Initialized.");

                // 2. Load provider specific information
                string dbConnexionName = ConfigurationManager.AppSettings["activeDbConnection"];
                LoadProviderSpecificInformation(xmlTemplatesNavigator, xmlPrefix[0], xmlNamespaces[0], dbConnexionName);

                // 3. Load SQL Templates
                FillTemplatesDictionaries(xmlTemplatesNavigator, xmlPrefix[0], xmlNamespaces[0]);

                // 4. Load mapping definitions
                FillMappingDictionary(xmlMappingNavigator, xmlPrefix[1], xmlNamespaces[1]);


            }
            catch (Exception ex)
            {
                LoggerTraceSource.TraceEvent(TraceEventType.Critical, 0, "ConfigurationLoader LoadXmlConfiguration, see detailed log");
                DetailedLoggerTraceSource.TraceEvent(TraceEventType.Critical, 0, "ConfigurationLoader : " + ex);
                throw;
            }
        }

        /// <summary>
        /// Recherche du noeud Provider désiré (déterminé à partir du nom de la connexion active) puis :
        /// - lecture de ses attributs pour les fields enclosers -> mise à jour de variables de classe StartFieldEncloser et EndFieldEncloser.
        /// - lecture de ses enfants 
        /// (Select, pour le moment celui qui définit le texte SQL pour "last insert ID" -> assigne une valeur à DbManager.SelectLastInsertIdCommandText).
        /// </summary>
        /// <param name="xPathNavigator_">Navigateur XPath du fichier des templates</param>
        /// <param name="xmlRootTagPrefix_">Préfixe de tag</param>
        /// <param name="xmlRootTagNamespace_">Namespace racine</param>
        /// <param name="activeDbConnectionName_">Nom de la connexion DB active (AppSettings)</param>
        internal static void LoadProviderSpecificInformation(XPathNavigator xPathNavigator_, string xmlRootTagPrefix_, string xmlRootTagNamespace_, string activeDbConnectionName_)
        {
            XmlNamespaceManager xmlNamespaceManager = new XmlNamespaceManager(xPathNavigator_.NameTable);
            xmlNamespaceManager.AddNamespace(xmlRootTagPrefix_, xmlRootTagNamespace_);

            if (string.IsNullOrWhiteSpace(activeDbConnectionName_))
            {
                LoggerTraceSource.TraceEvent(TraceEventType.Critical, 0, "No active connection name defined in appSettings ('activeDbConnection')");
                return;
            }

            var activeConnection = ConfigurationManager.ConnectionStrings[activeDbConnectionName_];
            if (activeConnection == null)
            {
                LoggerTraceSource.TraceEvent(TraceEventType.Critical, 0, "Active connection not found in available connection strings (key : '" + activeDbConnectionName_ + "'");
                return;
            }
            string conn = activeConnection.Name;
            if (string.IsNullOrWhiteSpace(conn))
            {
                LoggerTraceSource.TraceEvent(TraceEventType.Critical, 0, "No active connection name defined in appSettings for active connection '" + activeDbConnectionName_+ "'");
                return;
            }
            string providerInvariantName = activeConnection.ProviderName;
            if (string.IsNullOrWhiteSpace(providerInvariantName))
            {
                LoggerTraceSource.TraceEvent(TraceEventType.Critical, 0, "No active connection provider invariant name defined in appSettings  for active connection '" + activeDbConnectionName_ + "'");
                return;
            }
            // Expression XPath pour se positionner sur le noeud Provider avec l'attribut "name" à la valeur désirée
            string strXPathExpression = "/*/" + xmlRootTagPrefix_ + ":ProviderSpecific/" + xmlRootTagPrefix_ + ":Provider[@name='" + providerInvariantName + "']";


            XPathNodeIterator xPathNodeIteratorProviderNode = xPathNavigator_.Select(strXPathExpression, xmlNamespaceManager);
            if (!xPathNodeIteratorProviderNode.MoveNext())
                throw new Exception("Provider with name '" + providerInvariantName + "' missing in XML");

            // Sur ce noeud, attributs pour définir les field enclosers
            // Assignation aux variables de classe StartFieldEncloser et EndFieldEncloser
            StartFieldEncloser = xPathNodeIteratorProviderNode.Current.GetAttribute("StartFieldEncloser", "");
            EndFieldEncloser = xPathNodeIteratorProviderNode.Current.GetAttribute("EndFieldEncloser", "");
            string singleFieldEncloser = xPathNodeIteratorProviderNode.Current.GetAttribute("FieldEncloser", "");
            if (!string.IsNullOrEmpty(singleFieldEncloser))
            {
                // Si l'attribut "FieldEncloser" est défini, alors il écrase la valeur des deux autres
                StartFieldEncloser = EndFieldEncloser = singleFieldEncloser;
            }

            // Expression XPath pour sélectionner le noeud enfant "Select" avec la valeur de "name" à 'getlastinsertid'
            strXPathExpression = xmlRootTagPrefix_ + ":Select[@name='getlastinsertid']";

            XPathNodeIterator xPathNodeIteratorSelectNode = xPathNodeIteratorProviderNode.Current.Select(strXPathExpression, xmlNamespaceManager);
            if (xPathNodeIteratorSelectNode.MoveNext())
            {
                DbManager.SelectLastInsertIdCommandText = xPathNodeIteratorSelectNode.Current.Value;
            }
            else
            {
                LoggerTraceSource.TraceEvent(TraceEventType.Critical, 0, "ConfigurationLoader LoadProviderSpecificInformation, no value matching XPath '" + strXPathExpression + "' for the provider '" + providerInvariantName + "'");
            }
        }

        /// <summary>
        /// Loads XML file which contains mapping definitions to internal dictionary.
        /// <para>Transforme en ToLowerInariant() le nom du dictionnaire de mapping (nom de la table de la DB)</para>
        /// </summary>
        /// <param name="xmlNavigator_">Reused XPathNavigator instance</param>
        /// <param name="xmlRootTagPrefix_"> </param>
        /// <param name="xmlRootTagNamespace_"> </param>
        internal static void FillMappingDictionary(XPathNavigator xmlNavigator_, string xmlRootTagPrefix_, string xmlRootTagNamespace_)
        {
            MappingDictionnary.Clear();
            try
            {
                XmlNamespaceManager xmlNamespaceManager = new XmlNamespaceManager(xmlNavigator_.NameTable);
                xmlNamespaceManager.AddNamespace(xmlRootTagPrefix_, xmlRootTagNamespace_);
                // Table nodes
                XPathNodeIterator xPathNodeIterator = xmlNavigator_.Select("/*/" + xmlRootTagPrefix_ + ":Table", xmlNamespaceManager);

                var propertyColumnDictionary = new Dictionary<string, string>();
                while (xPathNodeIterator.MoveNext()) // Read Table node
                {
                    MappingDictionnary.Add(xPathNodeIterator.Current.GetAttribute("name", ""), propertyColumnDictionary);
                    xPathNodeIterator.Current.MoveToFirstChild();
                    do
                    {
                        if (xPathNodeIterator.Current.NodeType != XPathNodeType.Element)
                            continue;

                        propertyColumnDictionary.Add(xPathNodeIterator.Current.GetAttribute("property", ""), xPathNodeIterator.Current.GetAttribute("column", ""));
                    } while (xPathNodeIterator.Current.MoveToNext()); // Read next Mapping node

                    propertyColumnDictionary = new Dictionary<string, string>();
                }
            }
            catch (Exception ex)
            {
                LoggerTraceSource.TraceEvent(TraceEventType.Critical, 0, "ConfigurationLoader FillMappingDictionary, see detailed log");
                DetailedLoggerTraceSource.TraceEvent(TraceEventType.Critical, 0, "ConfigurationLoader: XML mapping definitions analyzis error: " + ex);
                throw;
            }
        }

        /// <summary>
        /// Loads XML file which contains templates definitions to internal dictionary.
        /// </summary>
        /// <param name="xpathNavigator_">Reused XPathNavigator instance</param>
        /// <param name="xmlRootTagPrefix_"> </param>
        /// <param name="xmlRootTagNamespace_"> </param>
        internal static void FillTemplatesDictionaries(XPathNavigator xpathNavigator_, string xmlRootTagPrefix_, string xmlRootTagNamespace_)
        {
            DicInsertSql.Clear();
            DicSelectSql.Clear();
            DicUpdateSql.Clear();
            DicDeleteSql.Clear();
            try
            {
                XmlNamespaceManager xmlNamespaceManager = new XmlNamespaceManager(xpathNavigator_.NameTable);
                xmlNamespaceManager.AddNamespace(xmlRootTagPrefix_, xmlRootTagNamespace_);
                // Inserts nodes
                XPathNodeIterator xPathNodeIterator = xpathNavigator_.Select("/*/" + xmlRootTagPrefix_ + ":Inserts", xmlNamespaceManager);
                if (xPathNodeIterator.MoveNext())
                    FillSqlTemplateDictionary(xPathNodeIterator, DicInsertSql);
                // Selects nodes
                xPathNodeIterator = xpathNavigator_.Select("/*/" + xmlRootTagPrefix_ + ":Selects", xmlNamespaceManager);
                if (xPathNodeIterator.MoveNext())
                    FillSqlTemplateDictionary(xPathNodeIterator, DicSelectSql);
                // Updates nodes
                xPathNodeIterator = xpathNavigator_.Select("/*/" + xmlRootTagPrefix_ + ":Updates", xmlNamespaceManager);
                if (xPathNodeIterator.MoveNext())
                    FillSqlTemplateDictionary(xPathNodeIterator, DicUpdateSql);
                // Deletes nodes
                xPathNodeIterator = xpathNavigator_.Select("/*/" + xmlRootTagPrefix_ + ":Deletes", xmlNamespaceManager);
                if (xPathNodeIterator.MoveNext())
                    FillSqlTemplateDictionary(xPathNodeIterator, DicDeleteSql);

            }
            catch (Exception ex)
            {
                LoggerTraceSource.TraceEvent(TraceEventType.Critical, 0, "ConfigurationLoader FillTemplatesDictionaries, see detailed log");
                DetailedLoggerTraceSource.TraceEvent(TraceEventType.Critical, 0, "ConfigurationLoader: XML templates definitions analyzis error: " + ex);
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
                if (node_.Current.NodeType != XPathNodeType.Element)
                    continue;

                string name = node_.Current.GetAttribute("name", "");
                if (workDictionary_.ContainsKey(name))
                    throw new Exception("A 'name' attribute with value '" + name + "' has been defined more than one time, XML is invalid");
                workDictionary_.Add(name, node_.Current.Value);
            } while (node_.Current.MoveToNext());
        }

        /// <summary>
        /// Recherche dans le tableau des providers disponibles, un provider donné.
        /// Méthode static car ne dépend pas de la lecture de la configuration
        /// </summary>
        /// <param name="providerFactoryToCheck_">Chaine qui est le nom invariant du provider.</param>
        /// <returns>Retourne vrai ou faux</returns>
        internal static bool FindInProviderFactoryClasses(string providerFactoryToCheck_)
        {
            // Retrieve the installed providers and factories.
            DataTable table = DbProviderFactories.GetFactoryClasses();

            // Display each row and column value.
            foreach (DataRow row in table.Rows)
            {
                if (row[2].ToString().Contains(providerFactoryToCheck_))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Loads configuration.
        /// <para>Parses XML configuration to internal dictionaries</para>
        /// <para>Initializes database connection</para>
        /// </summary>
        private void LoadConfiguration()
        {
            LoadXmlConfiguration();
            InitializeDatabaseConnection();
        }
    }
}