using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OsamesMicroOrm.Configuration;
using OsamesMicroOrm.Configuration.Tweak;
using OsamesMicroOrm.DbTools;
using SampleDbEntities.Chinook;
using TestOsamesMicroOrmSqlite.Tools;

namespace TestOsamesMicroOrmSqlite
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class TestDbToolsSelects : OsamesMicroOrmSqliteTest
    {
        private readonly string _incorrectMappingFileFullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CommonSqlite.CST_INCORRECT_MAPPING_CUSTOMER);

        /// <summary>
        /// Test de haut niveau du Select avec auto-détermination des propriétés et colonnes.
        /// Test ORM-37. Configuration incorrecte du mapping : exception attendue.
        /// </summary>
        [TestMethod]
        [TestCategory("SqLite")]
        [TestCategory("Mapping")]
        [TestCategory("Select")]
        public void TestSelectSingleAllColumnsIncorrectMapping()
        {
            try
            {
                // Customization
                Customizer.ConfigurationManagerSetKeyValue(Customizer.AppSettingsKeys.mappingFileName.ToString(), _incorrectMappingFileFullPath);
                // Reload modified configuration
                ConfigurationLoader.Clear();
                _config = ConfigurationLoader.Instance;
                // Dans la DB j'ai vérifié que cette requête donne un résultat, 'City' de valeur 'Paris'
                Customer customer = DbToolsSelects.SelectSingleAllColumns<Customer>("BaseReadAll", "Customer",
                    new List<string> { "City" }, new List<object> { "Paris" }, _transaction);
                Assert.IsNotNull(customer, "Pas d'enregistrement trouvé, requête select à corriger");
                // Si une exception est lancée, la ligne ci-dessous n'est pas exécutée.
                // Elle a vocation à faire échouer le test si elle s'exécute.
                Assert.Fail("Erreur, pas d'exception lancée/catchée ci-dessous");
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} {1}", ex.Message, ex.StackTrace);
                // Vérification de l'exception exacte qui a été lancée
                Assert.AreEqual("Column 'IdCustomer' doesn't exist in sql data reader", ex.Message);
            }
            finally
            {
                Customizer.ConfigurationManagerRestoreKey(Customizer.AppSettingsKeys.mappingFileName.ToString());
            }
        }

        /// <summary>
        /// Test de haut niveau du Select avec auto-détermination des propriétés et colonnes.
        /// </summary>
        [TestMethod]
        [TestCategory("SqLite")]
        [TestCategory("Select")]
        public void TestSelectSingleAllColumns()
        {
            _config = ConfigurationLoader.Instance;
            Customer customer = DbToolsSelects.SelectSingleAllColumns<Customer>("BaseReadAllWhere", "Customer",
              new List<string> { "IdCustomer", "#" }, new List<object> { 1 }, _transaction);
            Assert.IsNotNull(customer, "Pas d'enregistrement trouvé, requête select à corriger");

            // TODO les asserts
        }

        /// <summary>
        /// Test de haut niveau du Select avec auto-détermination des propriétés et colonnes.
        /// Pas de correspondance.
        /// </summary>
        [TestMethod]
        [TestCategory("SqLite")]
        [TestCategory("Select")]
        public void TestSelectSingleAllColumnsNoMatch()
        {
            _config = ConfigurationLoader.Instance;
            Customer customer = DbToolsSelects.SelectSingleAllColumns<Customer>("BaseReadAllWhere", "Customer",
              new List<string> { "IdCustomer", "#" }, new List<object> { -1 }, _transaction);
            Assert.IsNull(customer, "Doit retourner null");
        }

        /// <summary>
        /// Test de haut niveau du Select avec auto-détermination des propriétés et colonnes.
        /// Pas de correspondance.
        /// </summary>
        [TestMethod]
        [TestCategory("SqLite")]
        [TestCategory("Select")]
        public void TestSelectManyAllColumnsNoMatch()
        {
            _config = ConfigurationLoader.Instance;
            List<Customer> customers = DbToolsSelects.SelectAllColumns<Customer>("BaseReadAllWhereLessThan", "Customer",
              new List<string> { "IdCustomer", "#" }, new List<object> { 0 }, _transaction);
            Assert.AreEqual(0, customers.Count, "Doit retourner une liste vide");
        }

        

        [TestMethod]
        [TestCategory("Meta name")]
        [Owner("Benjamin Nolmans")]
        public void TestCount()
        {
            _config = ConfigurationLoader.Instance;
            long count = DbToolsSelects.Count("Count", "Customer");
            Console.WriteLine(count.ToString());
        }
    }
}
