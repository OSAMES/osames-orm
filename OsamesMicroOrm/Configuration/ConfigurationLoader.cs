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
using OsamesMicroOrm.Logging;
using OsamesMicroOrm.Utilities;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Text;

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
        internal static readonly Dictionary<string, string> DicInsertSql = new Dictionary<string, string>();

        /// <summary>
        /// Templates dictionary for update
        /// </summary>
        internal static readonly Dictionary<string, string> DicUpdateSql = new Dictionary<string, string>();

        /// <summary>
        /// Templates dictionary for select
        /// </summary>
        internal static readonly Dictionary<string, string> DicSelectSql = new Dictionary<string, string>();

        /// <summary>
        /// Templates dictionary for delete
        /// </summary>
        internal static readonly Dictionary<string, string> DicDeleteSql = new Dictionary<string, string>();

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
        /// Private constructor for singleton.
        /// </summary>
        private ConfigurationLoader()
        {
        }

        /// <summary>
        /// Singleton access. Creates an empty object once.
        /// </summary>
        /// <exception cref="OOrmHandledException">Erreurs diverses</exception>
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
        /// ORM configuration indicator that determines Exception behavior (ORM use context : webapp, winforms/wpf...).
        /// </summary>
        /// <exception cref="OverflowException"><paramref name="value" /> represents a number that is less than 0 or greater than 255. </exception>
        public static byte GetOrmContext
        {
            get
            {
                byte context;
                if (Byte.TryParse(ConfigurationManager.AppSettings["context"], out context))
                    return context;
                return 0;
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

        /// <summary>
        /// Lit la configuration dans les fichiers .config, vérifie la cohérence des valeurs. Teste que le provider factory peut être initialisé.
        /// Positionne les valeurs de la connexion string et du nom du provider sur le singleton de DbManager.
        /// </summary>
        /// <returns>false when configuration is wrong</returns>
        /// <exception cref="OOrmHandledException"></exception>
        private void CheckDatabaseConfiguration()
        {
            // 1. AppSettings : doit définir une connexion DB active
            string dbConnexion = ConfigurationManager.AppSettings["activeDbConnection"];
            if (string.IsNullOrWhiteSpace(dbConnexion))
                throw new OOrmHandledException(HResultEnum.E_NOACTIVECONNECTIONDEFINED, null);

            // 2. Cette connexion DB doit être trouvée dans les ConnectionStrings définies dans la configuration (attribute "Name")
            var activeConnection = ConfigurationManager.ConnectionStrings[dbConnexion];
            if (activeConnection == null)
                throw new OOrmHandledException(HResultEnum.E_NOACTIVECONNECTIONFOUND, null, "connection name: '" + dbConnexion + "'");

            // 3. Un provider doit être défini (attribut "ProviderName")
            string provider = activeConnection.ProviderName;
            if (string.IsNullOrWhiteSpace(provider))
                throw new OOrmHandledException(HResultEnum.E_NOPROVIDERNAMEFORCONNECTIONNAME, null, "connection name: '" + dbConnexion + "'");

            // 4. ce provider doit exister sur le système, si ce n'est pas le cas ce test lance une exception
            FindInProviderFactoryClasses(provider);

            // 5. Une chaîne de connexion doit être définie (attribut "ConnectionString")
            string conn = activeConnection.ConnectionString;
            if (string.IsNullOrWhiteSpace(conn))
                throw new OOrmHandledException(HResultEnum.E_NOCONNEXIONSTRINGDEFINED, null, null);

            Logger.Log(TraceEventType.Information, "Using DB connection string: '" + conn + "'");

            // Now pass information to DbHelper
            DbManager.ConnectionString = conn;
            DbManager.ProviderName = provider;

        }

        /// <summary>
        /// Reads configuration from appSettings then load specific configuration files to internal dictionaries.
        /// </summary>
        /// <exception cref="OOrmHandledException">Erreurs diverses</exception>
        private void LoadXmlConfiguration()
        {
            // 1. Load ORM Configuration File

            // Format path for loading the xsd schemas file
            string xsdSchemasPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppSettings["xmlSchemasFolder"].TrimStart('\\').TrimStart('/'));

                Logger.Log(TraceEventType.Information, "Osames ORM Initializing...");

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

                Logger.Log(TraceEventType.Information, "Osames ORM Initialized.");

                // 2. Load provider specific information
                string dbConnexionName = ConfigurationManager.AppSettings["activeDbConnection"];
                if (string.IsNullOrWhiteSpace(dbConnexionName))
                    throw new OOrmHandledException(HResultEnum.E_NOACTIVECONNECTIONDEFINED, null, null);

                LoadProviderSpecificInformation(xmlTemplatesNavigator, xmlPrefix[0], xmlNamespaces[0], dbConnexionName);

                // 3. Load SQL Templates
                FillTemplatesDictionaries(xmlTemplatesNavigator, xmlPrefix[0], xmlNamespaces[0]);

                // 4. Load mapping definitions
                FillMappingDictionary(xmlMappingNavigator, xmlPrefix[1], xmlNamespaces[1]);
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
        /// <exception cref="OOrmHandledException"></exception>
        private static void LoadProviderSpecificInformation(XPathNavigator xPathNavigator_, string xmlRootTagPrefix_, string xmlRootTagNamespace_, string activeDbConnectionName_)
        {
            XmlNamespaceManager xmlNamespaceManager = new XmlNamespaceManager(xPathNavigator_.NameTable);
            xmlNamespaceManager.AddNamespace(xmlRootTagPrefix_, xmlRootTagNamespace_);

            var activeConnection = ConfigurationManager.ConnectionStrings[activeDbConnectionName_];
            if (activeConnection == null)
                throw new OOrmHandledException(HResultEnum.E_NOACTIVECONNECTIONFOUND, null, "connection name: '" + activeDbConnectionName_ + "'");

            string providerInvariantName = activeConnection.ProviderName;
            if (string.IsNullOrWhiteSpace(providerInvariantName))
                throw new OOrmHandledException(HResultEnum.E_NOPROVIDERNAMEFORCONNECTIONNAME, null, "connection name: '" + activeDbConnectionName_ + "'");

            // Expression XPath pour se positionner sur le noeud Provider avec l'attribut "name" à la valeur désirée
            string strXPathExpression = "/*/" + xmlRootTagPrefix_ + ":ProviderSpecific/" + xmlRootTagPrefix_ + ":Provider[@name='" + providerInvariantName + "']";

            XPathNodeIterator xPathNodeIteratorProviderNode = xPathNavigator_.Select(strXPathExpression, xmlNamespaceManager);
            if (!xPathNodeIteratorProviderNode.MoveNext())
                throw new OOrmHandledException(HResultEnum.E_PROVIDERCONFIGMISSING, null, "provider name: " + providerInvariantName);

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
                throw new OOrmHandledException(HResultEnum.E_PROVIDERCONFIGMISSING, null, "provider name: '" + providerInvariantName + "' - missing information: Select[@name='getlastinsertid']");

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

            XmlNamespaceManager xmlNamespaceManager = new XmlNamespaceManager(xmlNavigator_.NameTable);
            xmlNamespaceManager.AddNamespace(xmlRootTagPrefix_, xmlRootTagNamespace_);
            // Table nodes
            XPathNodeIterator xPathNodeIterator = xmlNavigator_.Select("/*/" + xmlRootTagPrefix_ + ":Table", xmlNamespaceManager);

            var databaseTableDictionary = new Dictionary<string, string>();
            while (xPathNodeIterator.MoveNext()) // Read Table node
            {
                MappingDictionnary.Add(xPathNodeIterator.Current.GetAttribute("name", ""), databaseTableDictionary);
                xPathNodeIterator.Current.MoveToFirstChild();
                do
                {
                    if (xPathNodeIterator.Current.NodeType != XPathNodeType.Element)
                        continue;

                    databaseTableDictionary.Add(xPathNodeIterator.Current.GetAttribute("property", ""), xPathNodeIterator.Current.GetAttribute("column", ""));
                } while (xPathNodeIterator.Current.MoveToNext()); // Read next Mapping node

                databaseTableDictionary = new Dictionary<string, string>();
            }
        }

        /// <summary>
        /// Loads XML file which contains templates definitions to internal dictionary.
        /// </summary>
        /// <param name="xpathNavigator_">Reused XPathNavigator instance</param>
        /// <param name="xmlRootTagPrefix_"> </param>
        /// <param name="xmlRootTagNamespace_"> </param>
        /// <exception cref="OOrmHandledException"></exception>
        internal static void FillTemplatesDictionaries(XPathNavigator xpathNavigator_, string xmlRootTagPrefix_, string xmlRootTagNamespace_)
        {
            DicInsertSql.Clear();
            DicSelectSql.Clear();
            DicUpdateSql.Clear();
            DicDeleteSql.Clear();

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

        /// <summary>
        /// Extracts information from XML data which contains templates definitions to internal dictionary.
        /// </summary>
        /// <param name="node_"></param>
        /// <param name="workDictionary_"></param>
        /// <exception cref="OOrmHandledException"></exception>
        private static void FillSqlTemplateDictionary(XPathNodeIterator node_, Dictionary<string, string> workDictionary_ = null)
        {
            if (workDictionary_ == null) return;

            node_.Current.MoveToFirstChild();
            do
            {
                if (node_.Current.NodeType != XPathNodeType.Element)
                    continue;

                string name = node_.Current.GetAttribute("name", "");
                if (workDictionary_.ContainsKey(name))
                    throw new OOrmHandledException(HResultEnum.E_XMLNAMEATTRIBUTEMORETHANONCE, null, "value: " + name);
                string nodeValue = node_.Current.Value;
                if (node_.Current.Value[nodeValue.Length - 1] == ';')
                    workDictionary_.Add(name, nodeValue);
                else
                    workDictionary_.Add(name, nodeValue + ';');
            } while (node_.Current.MoveToNext());
        }

        /// <summary>
        /// Recherche sur le système d'un provider donné.
        /// Méthode static car ne dépend pas de la lecture de la configuration.
        /// </summary>
        /// <param name="providerFactoryToCheck_">Chaine qui est le nom invariant du provider.</param>
        /// <returns>Ne renvoie rien</returns>
        /// <exception cref="OOrmHandledException">Si provider non trouvé</exception>
        internal static void FindInProviderFactoryClasses(string providerFactoryToCheck_)
        {
            try
            {
                DbProviderFactories.GetFactory(providerFactoryToCheck_);
            }
            catch (ArgumentException ex)
            {
                throw new OOrmHandledException(HResultEnum.E_PROVIDERNOTINSTALLED, ex, "provider name: '" + providerFactoryToCheck_ + "'" + Environment.NewLine + Environment.NewLine + ListExistingProviders());
            }

        }
        /// <summary>
        /// Formate un texte listant les providers disponibles sur le système (nom invariant et description).
        /// </summary>
        /// <returns></returns>
        private static string ListExistingProviders()
        {
            StringBuilder sb = new StringBuilder();
            DataTable providers = DbProviderFactories.GetFactoryClasses();
                            DataColumn invariantNameCol = providers.Columns["InvariantName"];
                DataColumn descriptionColumn = providers.Columns["Description"];
            foreach (DataRow row in providers.Rows)
            {
                sb.Append(row[invariantNameCol]).Append(" (").Append(row[descriptionColumn]).Append(")").Append(Environment.NewLine);
            }
            return "Available providers invariant names: " + Environment.NewLine + sb;
        }

        /// <summary>
        /// Loads configuration.
        /// <para>Parses XML configuration to internal dictionaries</para>
        /// <para>Checks database configuration and sets values to DbManager</para>
        /// </summary>
        /// <exception cref="OOrmHandledException">Erreurs diverses</exception>
        private void LoadConfiguration()
        {
            LoadXmlConfiguration();
            CheckDatabaseConfiguration();
        }
    }
}