using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Xml.XPath;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OsamesMicroOrm;
using OsamesMicroOrm.Configuration;
using OsamesMicroOrm.Configuration.Tweak;
using TestOsamesMicroOrm.Tools;

namespace TestOsamesMicroOrm
{
    [TestClass]
    [
        // fichiers spécifiques au projet de test pour ces tests
        DeploymentItem(Common.CST_SQL_TEMPLATES_XML_TEST_NODE_CASE, Common.CST_TEST_CONFIG),
        DeploymentItem(Common.CST_SQL_TEMPLATES_XML_TEST_DUPLICATE_SELECT, Common.CST_TEST_CONFIG)
    ]
    [ExcludeFromCodeCoverage]
    public class TestOrmConfigurationLoader : OsamesMicroOrmTest
    {
        private readonly string _templatesFileFullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Common.CST_SQL_TEMPLATES_XML);
        private readonly string _templatesTestNodeCase = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Common.CST_SQL_TEMPLATES_XML_TEST_NODE_CASE);
        private readonly string _templatesTestDuplicateSelect = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Common.CST_SQL_TEMPLATES_XML_TEST_DUPLICATE_SELECT);

        [TestInitialize]
        public override void Setup()
        {
            // Obligatoire car Resharper ne comprend pas qu'il faut initilaliser la classe mère.
            var tempo = ConfigurationLoader.Instance;
        }

        #region configurations erronées dans les fichiers .config

        /// <summary>
        /// Pas de valeur pour la connexion active dans AppSettings.
        /// </summary>
        [TestMethod]
        [ExcludeFromCodeCoverage]
        [Owner("Barbara Post")]
        [TestCategory("Configuration")]
        [ExpectedException(typeof(OOrmHandledException))]
        public void TestNoActiveConnectionDefinedAppSettings()
        {
            try
            {
                Customizer.ConfigurationManagerSetKeyValue(Customizer.AppSettingsKeys.activeDbConnection.ToString(), "");
                var test = ConfigurationLoader.Instance;
            }
            catch (OOrmHandledException ex)
            {
                Common.AssertOnHresultAndWriteToConsole(HResultEnum.E_NOACTIVECONNECTIONDEFINED, ex);
                throw;
            }
            finally
            {
                Customizer.ConfigurationManagerRestoreKey(Customizer.AppSettingsKeys.activeDbConnection.ToString());
                var test = ConfigurationLoader.Instance;
            }
        }

        /// <summary>
        /// La clé correspondant à la connexion active dans AppSettings n'a pas de correspondance dans le fichier des connexion strings.
        /// </summary>
        [TestMethod]
        [ExcludeFromCodeCoverage]
        [Owner("Barbara Post")]
        [TestCategory("Configuration")]
        [ExpectedException(typeof(OOrmHandledException))]
        public void TestActiveConnectionDefinedAppSettingsNotFoundAmongConnectionStrings()
        {
            try
            {
                Customizer.ConfigurationManagerSetKeyValue(Customizer.AppSettingsKeys.activeDbConnection.ToString(), "no match");
                var test = ConfigurationLoader.Instance;
            }
            catch (OOrmHandledException ex)
            {
                Common.AssertOnHresultAndWriteToConsole(HResultEnum.E_NOACTIVECONNECTIONFOUND, ex);
                throw;
            }
            finally
            {
                Customizer.ConfigurationManagerRestoreKey(Customizer.AppSettingsKeys.activeDbConnection.ToString());
                var test = ConfigurationLoader.Instance;
            }
        }

        /// <summary>
        /// Dans les connexion strings, une connexion string ne définit pas de provider name.
        /// Pour que ce test passe il doit y avoir une configuration dans le fichier des connection strings pour ce provider "test", avec un nom de provider à blanc.
        /// </summary>
        [TestMethod]
        [ExcludeFromCodeCoverage]
        [Owner("Barbara Post")]
        [TestCategory("Configuration")]
        [ExpectedException(typeof(OOrmHandledException))]
        [Ignore]
        public void TestConnectionStringMissingProviderName()
        {
            try
            {
                //ConfigurationManager.ConnectionStrings.Add(new ConnectionStringSettings(" ", "test"));
                Customizer.ConfigurationManagerSetKeyValue(Customizer.AppSettingsKeys.activeDbConnection.ToString(), "test");
                var test = ConfigurationLoader.Instance;
            }
            catch (OOrmHandledException ex)
            {
                Common.AssertOnHresultAndWriteToConsole(HResultEnum.E_NOPROVIDERNAMEFORCONNECTIONNAME, ex);
                throw;
            }
            finally
            {
                //ConfigurationManager.ConnectionStrings.Remove(" ");
                var test = ConfigurationLoader.Instance;
            }
        }

        /// <summary>
        /// Le provider défini dans le fichier des connexion strings (pour la connexion active) n'est pas installé sur le système.
        /// Pour que ce test passe il doit y avoir une configuration dans le fichier des templates (orm:ProviderSpecific) pour ce provider "test".
        /// </summary>
        [TestMethod]
        [ExcludeFromCodeCoverage]
        [Owner("Barbara Post")]
        [TestCategory("Configuration")]
        [ExpectedException(typeof(OOrmHandledException))]
        [Ignore]
        public void TestNotInstalledProvider()
        {
            try
            {
                Customizer.ConfigurationManagerSetKeyValue(Customizer.AppSettingsKeys.activeDbConnection.ToString(), "test");
                var test = ConfigurationLoader.Instance;
            }
            catch (OOrmHandledException ex)
            {
                Common.AssertOnHresultAndWriteToConsole(HResultEnum.E_PROVIDERNOTINSTALLED, ex);
                throw;
            }
            finally
            {
                Customizer.ConfigurationManagerRestoreKey(Customizer.AppSettingsKeys.activeDbConnection.ToString());
                var test = ConfigurationLoader.Instance;
            }
        }


        #endregion

        /// <summary>
        /// Load of correct configuration file.
        /// Assertions on internal data dictionaries fed by xml reading.
        /// </summary>
        [TestMethod]
        [ExcludeFromCodeCoverage]
        [Owner("Benjamin Nolmans")]
        [TestCategory("XML")]
        [TestCategory("Configuration")]
        public void TestConfigurationLoaderAssertOnInternalDictionaries()
        {
            Assert.IsTrue(ConfigurationLoader.MappingDictionnary.Keys.Count > 0, "Expected keys in dictionary after initialize");
            Assert.IsTrue(ConfigurationLoader.DicInsertSql.Keys.Count > 0, "Expected keys in INSERT dictionary after initialize");
            Assert.IsTrue(ConfigurationLoader.DicSelectSql.Keys.Count > 0, "Expected keys in SELECT dictionary after initialize");
            Assert.IsTrue(ConfigurationLoader.DicUpdateSql.Keys.Count > 0, "Expected keys in UPDATE dictionary after initialize");
            Assert.IsTrue(ConfigurationLoader.DicDeleteSql.Keys.Count > 0, "Expected keys in DELETE dictionary after initialize");

            // Control Dictionary content
            Console.WriteLine("== INSERTS ==");
            ConfigurationLoader.DicInsertSql.ToList().ForEach(x => Console.WriteLine(x.Key));
            Console.WriteLine("== SELECTS ==");
            ConfigurationLoader.DicSelectSql.ToList().ForEach(x => Console.WriteLine(x.Key));
            Console.WriteLine("== UPDATES ==");
            ConfigurationLoader.DicUpdateSql.ToList().ForEach(x => Console.WriteLine(x.Key));
            Console.WriteLine("== DELETES ==");
            ConfigurationLoader.DicDeleteSql.ToList().ForEach(x => Console.WriteLine(x.Key));

            // See TestFillTemplatesDictionaries and TestFillMappingDictionaries for detailed asserts 
        }

        /// <summary>
        /// Load of correct configuration file.
        /// Assertions on formatted string related to database access that was passed to DbHelper.
        /// </summary>
        [TestMethod]
        [ExcludeFromCodeCoverage]
        [Owner("Benjamin Nolmans")]
        [TestCategory("Configuration")]
        [TestCategory("MsSql")]
        public void TestConfigurationLoaderAssertOnMsSqlDatabaseParameters()
        {
            try
            {
                //AppDomain.CurrentDomain.SetData("DataDirectory", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ""));

                // Usage du tweak car nous ne sommes pas dans une classe de test d'un projet "OsamesMicroOrm[type de la DB]Test".
                Customizer.ConfigurationManagerSetKeyValue(Customizer.AppSettingsKeys.activeDbConnection.ToString(), "OsamesMicroORM.LocalDB");

                ConfigurationLoader tempo = ConfigurationLoader.Instance;

                //Console.WriteLine(AppDomain.CurrentDomain.GetData("DataDirectory").ToString());

                // "|DataDirectory|" est résolu au runtime, nous ne pouvons pas remplacer la valeur en TU et faire un assert dessus 
                Assert.AreEqual(string.Format("Data Source=(LocalDB)\\v11.0;AttachDbFilename={0}{1}", "|DataDirectory|", @"\DB\Chinook.mdf;Integrated Security=True;Connect Timeout=30"), DbManager.ConnectionString);
                Assert.AreEqual(@"System.Data.SqlClient", DbManager.ProviderName);
            }
            finally
            {
                Customizer.ConfigurationManagerRestoreKey(Customizer.AppSettingsKeys.activeDbConnection.ToString());
                Customizer.ConfigurationManagerRestoreKey(Customizer.AppSettingsKeys.dbName.ToString());
            }
        }

        /// <summary>
        /// Load of incorrect configuration file: duplicate "name" attribute for 2 Select items.
        /// </summary>
        [TestMethod]
        [ExcludeFromCodeCoverage]
        [Owner("Barbara Post")]
        [TestCategory("XML")]
        [TestCategory("Configuration")]
        [ExpectedException(typeof(OOrmHandledException))]
        public void TestConfigurationLoaderIncorrectXmlAssertOnInternalDictionaries()
        {
            try
            {
                Customizer.ConfigurationManagerSetKeyValue(Customizer.AppSettingsKeys.sqlTemplatesFileName.ToString(), _templatesTestDuplicateSelect);
                Customizer.ConfigurationManagerSetKeyValue(Customizer.AppSettingsKeys.mappingFileName.ToString(), _mappingFileFullPath);
                ConfigurationLoader config = ConfigurationLoader.Instance;
            }
            catch (OOrmHandledException ex)
            {
                Common.AssertOnHresultAndWriteToConsole(HResultEnum.E_XMLNAMEATTRIBUTEMORETHANONCE, ex);
                throw;
            }
            finally
            {
                Customizer.ConfigurationManagerRestoreKey(Customizer.AppSettingsKeys.sqlTemplatesFileName.ToString());
                Customizer.ConfigurationManagerRestoreKey(Customizer.AppSettingsKeys.mappingFileName.ToString());
            }
        }

        /// <summary>
        /// Load of configuration file with different node case.
        /// </summary>
        [TestMethod]
        [ExcludeFromCodeCoverage]
        [Owner("Barbara Post")]
        [TestCategory("XML")]
        [TestCategory("Configuration")]
        [TestCategory("Templates")]
        public void TestConfigurationLoaderNodeCase()
        {
            ConfigurationLoader.FillTemplatesDictionaries(new XPathDocument(_templatesTestNodeCase).CreateNavigator(), "orm", "http://www.osames.org/osamesorm");
            Assert.IsTrue(ConfigurationLoader.DicInsertSql.Keys.Count > 0, "Expected keys in INSERT dictionary after initialize");
            Assert.IsTrue(ConfigurationLoader.DicSelectSql.Keys.Count > 0, "Expected keys in SELECT dictionary after initialize");
            Assert.IsTrue(ConfigurationLoader.DicUpdateSql.Keys.Count > 0, "Expected keys in UPDATE dictionary after initialize");
            Assert.IsTrue(ConfigurationLoader.DicDeleteSql.Keys.Count > 0, "Expected keys in DELETE dictionary after initialize");
        }

        /// <summary>
        /// After test run, ConfigurationLoader internal dictionary should be populated.
        /// </summary>
        [TestMethod]
        [ExcludeFromCodeCoverage]
        [Owner("Barbara Post")]
        [TestCategory("XML")]
        [TestCategory("Configuration")]
        [TestCategory("Mapping")]
        public void TestFillMappingDictionary()
        {
            ConfigurationLoader.FillMappingDictionary(new XPathDocument(_mappingFileFullPath).CreateNavigator(), "orm", "http://www.osames.org/osamesorm");
            Assert.AreEqual(5, ConfigurationLoader.MappingDictionnary.Keys.Count, "Expected 5 keys in tables dictionary after initialize");
            Assert.IsTrue(ConfigurationLoader.MappingDictionnary.ContainsKey("Customer"), "Expected to find 'Customer' key");

            // Inspect detail for a specific case
            Dictionary<string, string> mappings = ConfigurationLoader.MappingDictionnary["Customer"];
            Assert.AreEqual(13, mappings.Keys.Count, "Expected 13 keys in dictionary for Customer mappings after initialize");
            Assert.IsTrue(mappings.ContainsKey("Email"), "Expected to find 'Email' key");
            Assert.AreEqual("Email", mappings["Email"], "Expected column 'Email' for property 'Email'");
        }

      

      

        /// <summary>
        /// After test run, ConfigurationLoader internal dictionary should be populated.
        /// </summary>
        [TestMethod]
        [ExcludeFromCodeCoverage]
        [Owner("Barbara Post")]
        [TestCategory("XML")]
        [TestCategory("Configuration")]
        public void TestFillTemplatesDictionaries()
        {
            ConfigurationLoader.FillTemplatesDictionaries(new XPathDocument(_templatesFileFullPath).CreateNavigator(), "orm", "http://www.osames.org/osamesorm");
            Assert.IsTrue(ConfigurationLoader.DicInsertSql.Keys.Count > 0, "Expected keys in INSERT dictionary after initialize");
            Assert.IsTrue(ConfigurationLoader.DicSelectSql.Keys.Count > 0, "Expected keys in SELECT dictionary after initialize");
            Assert.IsTrue(ConfigurationLoader.DicUpdateSql.Keys.Count > 0, "Expected keys in UPDATE dictionary after initialize");
            Assert.IsTrue(ConfigurationLoader.DicDeleteSql.Keys.Count > 0, "Expected keys in DELETE dictionary after initialize");

            // Control Dictionary content
            Console.WriteLine("== INSERTS ==");
            ConfigurationLoader.DicInsertSql.ToList().ForEach(x => Console.WriteLine(x.Key));
            Console.WriteLine("== SELECTS ==");
            ConfigurationLoader.DicSelectSql.ToList().ForEach(x => Console.WriteLine(x.Key));
            Console.WriteLine("== UPDATES ==");
            ConfigurationLoader.DicUpdateSql.ToList().ForEach(x => Console.WriteLine(x.Key));
            Console.WriteLine("== DELETES ==");
            ConfigurationLoader.DicDeleteSql.ToList().ForEach(x => Console.WriteLine(x.Key));

            // Inspect detail for a specific case
            Assert.IsTrue(ConfigurationLoader.DicSelectSql.ContainsKey("BaseReadWhere"), "'BaseReadWhere' key not in select dictionary");
            Assert.AreEqual("SELECT {0} FROM {1} WHERE {2} = {3};", ConfigurationLoader.DicSelectSql["BaseReadWhere"]);
        }

        /// <summary>
        /// Pour ce projet de TU il n'y a pas de providers définis dans App.Config.
        /// </summary>
        [TestMethod]
        [ExcludeFromCodeCoverage]
        [Owner("Barbara Post")]
        [TestCategory("Configuration")]
        [TestCategory("Sql provider search")]
        [ExpectedException(typeof(OOrmHandledException))]
        public void TestFindInProviderFactoryClassesSomeProvider()
        {
            ConfigurationLoader tempo = ConfigurationLoader.Instance;
            try
            {
                ConfigurationLoader.FindInProviderFactoryClasses("some.provider");
            }
            catch (OOrmHandledException ex)
            {
                Common.AssertOnHresultAndWriteToConsole(HResultEnum.E_PROVIDERNOTINSTALLED,ex);
                throw;
            }
        }

        /// <summary>
        /// Pour ce projet de TU il n'y a pas de providers définis dans App.Config.
        /// </summary>
        [TestMethod]
        [ExcludeFromCodeCoverage]
        [Owner("Barbara Post")]
        [TestCategory("Configuration")]
        [TestCategory("Sql provider search")]
        [ExpectedException(typeof(OOrmHandledException))]
        public void TestFindInProviderFactoryClassesSqlite()
        {
            ConfigurationLoader tempo = ConfigurationLoader.Instance;
            try
            {
                ConfigurationLoader.FindInProviderFactoryClasses("System.Data.SQLite");
            }
            catch (OOrmHandledException ex)
            {
                Common.AssertOnHresultAndWriteToConsole(HResultEnum.E_PROVIDERNOTINSTALLED, ex);
                throw;
            }
        }

        /// <summary>
        /// Pour ce projet de TU il n'y a pas de providers définis dans App.Config.
        /// </summary>
        [TestMethod]
        [ExcludeFromCodeCoverage]
        [Owner("Barbara Post")]
        [TestCategory("Configuration")]
        [TestCategory("Sql provider search")]
        public void TestFindInProviderFactoryClassesSqlclient()
        {
            ConfigurationLoader tempo = ConfigurationLoader.Instance;
            try
            {
                ConfigurationLoader.FindInProviderFactoryClasses("System.Data.SqlClient");
            }
            catch (OOrmHandledException ex)
            {
                // Si jamais on a une erreur c'est celle-ci qui doit sortir, cependant on ne doit pas avoir d'erreur
                Common.AssertOnHresultAndWriteToConsole(HResultEnum.E_PROVIDERNOTINSTALLED, ex);
                throw;
            }
        }

    }
}
