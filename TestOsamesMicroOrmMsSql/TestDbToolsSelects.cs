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
using TestOsamesMicroOrmMsSql.Tools;

namespace TestOsamesMicroOrmMsSql
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class TestDbToolsSelects : OsamesMicroOrmMsSqlTest
    {
        private readonly string _incorrectMappingFileFullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CommonMsSql.CST_INCORRECT_MAPPING_CUSTOMER);

        /// <summary>
        /// Test de haut niveau du Select.
        /// Test ORM-37. Configuration incorrecte du mapping : exception attendue.
        /// Le mapping définit une propriété CustomerId, une colonne IdCustomer alors que le nom de la colonne en base est CustomerId.
        /// C'est la requête SQL qui part en erreur.
        /// </summary>
        [TestMethod]
        [TestCategory("MsSql")]
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
        /// Test ORM-37. Configuration incorrecte du mapping : exception attendue.
        /// Le mapping définit une colonne IdCustomer alors que le nom de la colonne en base est CustomerId.
        /// </summary>
        [TestMethod]
        [TestCategory("MsSql")]
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
        /// Test du select sur un champ date.
        /// 1962/2/18 pour Adams Andrew.
        /// </summary>
        [TestMethod]
        [TestCategory("MsSql")]
        [TestCategory("Mapping")]
        [TestCategory("Select")]
        public void TestSelectDateData()
        {
            Employee employee = DbToolsSelects.SelectSingle<Employee>(new List<string> {"FirstName", "LastName", "BirthDate"}, "BaseReadWhereAndWhere", "Employee",
                new List<string> {"FirstName", "#", "LastName", "#"}, new List<object> {"Andrew", "Adams"});
            Console.WriteLine(employee.BirthDate);
            Assert.AreEqual(new DateTime(1962,2,18), employee.BirthDate);

        }

        /// <summary>
        /// Test du select sur un champ string et tentative de mise de la valeur dans une DateTime C#. Exception attendue avec détail. 
        /// 1962/2/18 pour Adams Andrew.
        /// </summary>
        [TestMethod]
        [TestCategory("MsSql")]
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

                Employee employee = DbToolsSelects.SelectSingle<Employee>(new List<string> { "FirstName", "LastName", "BirthDate" }, "BaseReadWhereAndWhere", "Employee",
                    new List<string> { "FirstName", "#", "LastName", "#" }, new List<object> { "Andrew", "Adams" });
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
