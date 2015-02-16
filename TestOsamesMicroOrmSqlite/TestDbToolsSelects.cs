using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OsamesMicroOrm;
using OsamesMicroOrm.Configuration;
using OsamesMicroOrm.Configuration.Tweak;
using OsamesMicroOrm.DbTools;
using OsamesMicroOrm.Utilities;
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
        /// Le mapping définit une colonne IdCustomer alors que le nom de la colonne en base est CustomerId.
        /// </summary>
        [TestMethod]
        [TestCategory("SqLite")]
        [TestCategory("Mapping")]
        [TestCategory("Select")]
        [ExpectedException(typeof(OOrmHandledException))]
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
                Assert.IsNotNull(customer, "Requête incorrecte");
            }
            catch (OOrmHandledException ex)
            {
                Console.WriteLine(ex.Message);
                Assert.AreEqual(OOrmErrorsHandler.FindHResultAndDescriptionByCode(HResultEnum.E_COLUMNDOESNOTEXIST).Key, ex.HResult);
                throw;
            }
            finally
            {
                Customizer.ConfigurationManagerRestoreKey(Customizer.AppSettingsKeys.mappingFileName.ToString());
            }
        }

        /// <summary>
        /// Test de haut niveau du Select.
        /// Test ORM-37. Configuration incorrecte du mapping : exception attendue.
        /// Le mapping définit une propriété CustomerId, une colonne IdCustomer alors que le nom de la colonne en base est CustomerId.
        /// C'est la requête SQL qui part en erreur.
        /// </summary>
        [TestMethod]
        [TestCategory("SqLite")]
        [TestCategory("Mapping")]
        [TestCategory("Select")]
        [ExpectedException(typeof(OOrmHandledException))]
        public void TestSelectSingleIncorrectMapping()
        {
            try
            {
                // Customization
                Customizer.ConfigurationManagerSetKeyValue(Customizer.AppSettingsKeys.mappingFileName.ToString(), _incorrectMappingFileFullPath);
                // Reload modified configuration
                ConfigurationLoader.Clear();
                _config = ConfigurationLoader.Instance;
                // Dans la DB j'ai vérifié que cette requête donne un résultat, 'City' de valeur 'Paris'
                Customer customer = DbToolsSelects.SelectSingle<Customer>(new List<string> { "CustomerId", "FirstName", "LastName" }, "BaseReadWhere", "Customer",
                    new List<string> { "City", "#" }, new List<object> { "Paris" }, _transaction);
                Assert.IsNotNull(customer, "Requête incorrecte");
            }
            catch (OOrmHandledException ex)
            {
                Console.WriteLine(ex.Message + (ex.InnerException != null ? ex.InnerException.Message : ""));
                Assert.AreEqual(OOrmErrorsHandler.FindHResultAndDescriptionByCode(HResultEnum.E_EXECUTEREADERFAILED).Key, ex.HResult);
                throw;
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
            Assert.IsTrue(count > 1);
        }


        /// <summary>
        /// Select en n'ayant pas le bon nombre de valeurs pour les paramètres ADO.NET.
        /// </summary>
        [TestMethod]
        [TestCategory("SqLite")]
        [TestCategory("Select")]
        [TestCategory("Parameter NOK")]
        [ExpectedException(typeof(OOrmHandledException))]
        [Owner("Barbara Post")]
        public void TestSelectParameterValuesMismatch()
        {
            try
            {

                // "BaseReadAllWhereBetween" : SELECT * FROM {0} WHERE {1} between {2} and {3};
                // Il manque le premier élément de la liste des valeurs qui doit être "CustomerId".
                List<Customer> customers = DbToolsSelects.SelectAllColumns<Customer>("BaseReadAllWhereBetween", "Customer", new List<string> { "%CustomerId", "#", "#" }, new List<object> { 3, 4 });
                Console.WriteLine("[0] : ID " + customers[0].IdCustomer + " Nom : " + customers[0].LastName + " prénom : " + customers[0].FirstName);
                Console.WriteLine("[1] : ID " + customers[1].IdCustomer + " Nom : " + customers[1].LastName + " prénom : " + customers[1].FirstName);
            }
            catch (OOrmHandledException ex)
            {
                Console.WriteLine(ex.Message + (ex.InnerException != null ? ex.InnerException.Message : ""));
                Assert.AreEqual(OOrmErrorsHandler.FindHResultAndDescriptionByCode(HResultEnum.E_STRINGFORMATCOUNTMISMATCH).Key, ex.HResult);
                throw;
            }

        }
    }
}
