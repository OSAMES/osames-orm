using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OsamesMicroOrm;
using OsamesMicroOrm.Configuration;
using OsamesMicroOrm.Configuration.Tweak;
using OsamesMicroOrm.DbTools;
using SampleDbEntities.Chinook;
using TestOsamesMicroOrm.Tools;
using TestOsamesMicroOrmSqlite.Tools;

namespace TestOsamesMicroOrmSqlite
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class TestDbToolsSelects : OsamesMicroOrmSqliteTest
    {
        private readonly string _incorrectMappingFileFullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CommonSqlite.CST_INCORRECT_MAPPING_CUSTOMER);

        private readonly string _incorrectMappingFileFullPath2 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CommonSqlite.CST_INCORRECT_MAPPING_CUSTOMER2);


        /// <summary>
        /// Test de haut niveau du Select avec auto-détermination des propriétés et colonnes.
        /// Test ORM-37. Configuration incorrecte du mapping : exception attendue.
        /// 
        /// Le mapping définit une colonne IdCustomer alors que le nom de la colonne en base est CustomerId.
        /// On lève une exception SQL dans le DataReader mais on est en mesure de dire que c'est à cause de la tentative de lecture d'une colonne qui n'existe pas.
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
                Common.AssertOnHresultAndWriteToConsole(HResultEnum.E_COLUMNDOESNOTEXIST, ex);
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
        /// Le mapping définit une propriété IdCustomer, une colonne IdCustomer alors que le nom de la colonne en base est CustomerId.
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
                Customer customer = DbToolsSelects.SelectSingle<Customer>(new List<string> { "IdCustomer", "FirstName", "LastName" }, "BaseReadWhere", "Customer",
                    new List<string> { "City", "#" }, new List<object> { "Paris" }, _transaction);
                Assert.IsNotNull(customer, "Requête incorrecte");
            }
            catch (OOrmHandledException ex)
            {
                Common.AssertOnHresultAndWriteToConsole(HResultEnum.E_EXECUTEREADERFAILED, ex);
                throw;
            }
            finally
            {
                Customizer.ConfigurationManagerRestoreKey(Customizer.AppSettingsKeys.mappingFileName.ToString());
            }
        }

        /// <summary>
        /// Test de haut niveau du Select.
        /// Test ORM-156. Configuration incorrecte du mapping : exception attendue.
        /// Le mapping définit une propriété CustomerId, une colonne CustomerId alors que le nom de la propriété de Customer est IdCustomer.
        /// </summary>
        [TestMethod]
        [TestCategory("SqLite")]
        [TestCategory("Mapping")]
        [TestCategory("Select")]
        [ExpectedException(typeof(OOrmHandledException))]
        public void TestSelectSingleIncorrectMapping2()
        {
            try
            {
                // Customization
                Customizer.ConfigurationManagerSetKeyValue(Customizer.AppSettingsKeys.mappingFileName.ToString(), _incorrectMappingFileFullPath2);
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
                Common.AssertOnHresultAndWriteToConsole(HResultEnum.E_EXECUTEREADERFAILED, ex);
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
        public void TestSelectMetaNamesAndValuesCountMismatch()
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
                Common.AssertOnHresultAndWriteToConsole(HResultEnum.E_METANAMESVALUESCOUNTMISMATCH, ex);
                throw;
            }

        }

        /// <summary>
        /// Test du select sur un champ date.
        /// 1962/2/18 pour Adams Andrew.
        /// </summary>
        [TestMethod]
        [TestCategory("SqLite")]
        [TestCategory("Mapping")]
        [TestCategory("Select")]
        public void TestSelectDateData()
        {
            Employee employee = DbToolsSelects.SelectSingle<Employee>(new List<string> { "FirstName", "LastName", "BirthDate" }, "BaseReadWhereAndWhere", "Employee",
                new List<string> { "FirstName", "#", "LastName", "#" }, new List<object> { "Andrew", "Adams" });
            Console.WriteLine(employee.BirthDate);
            Assert.AreEqual(new DateTime(1962, 2, 18), employee.BirthDate);

        }

        /// <summary>
        /// Test du select sur un champ string et tentative de mise de la valeur dans une DateTime C#. Exception attendue avec détail. 
        /// 1962/2/18 pour Adams Andrew.
        /// </summary>
        [TestMethod]
        [TestCategory("SqLite")]
        [TestCategory("Mapping")]
        [TestCategory("Select")]
        [ExpectedException(typeof(OOrmHandledException))]
        public void TestSelectDateDataIncorrectMapping()
        {
            try
            {
                // Customization
                Customizer.ConfigurationManagerSetKeyValue(Customizer.AppSettingsKeys.mappingFileName.ToString(), _incorrectMappingFileFullPath);
                // Reload modified configuration
                ConfigurationLoader.Clear();
                _config = ConfigurationLoader.Instance;

                Employee employee = DbToolsSelects.SelectSingle<Employee>(new List<string> {"FirstName", "LastName", "BirthDate"}, "BaseReadWhereAndWhere", "Employee",
                    new List<string> {"FirstName", "#", "LastName", "#"}, new List<object> {"Andrew", "Adams"});
                Console.WriteLine(employee.BirthDate);
                Assert.AreEqual(new DateTime(1962, 2, 18), employee.BirthDate);

            }
            catch (OOrmHandledException ex)
            {
                Common.AssertOnHresultAndWriteToConsole(HResultEnum.E_CANNOTSETVALUEDATAREADERTODBENTITY, ex);
                throw;
            }
            finally
            {
                Customizer.ConfigurationManagerRestoreKey(Customizer.AppSettingsKeys.mappingFileName.ToString());
            }

        }
    }
}
