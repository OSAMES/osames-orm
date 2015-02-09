using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OsamesMicroOrm.Configuration;
using OsamesMicroOrm.Configuration.Tweak;
using OsamesMicroOrm.DbTools;
using SampleDbEntities.Chinook;
using TestOsamesMicroOrmMsSql.Tools;

namespace TestOsamesMicroOrmMsSql
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class TestDbToolsSelects : OsamesMicroOrmMsSqlTest
    {
        private readonly string _incorrectMappingFileFullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CommonMsSql.CST_INCORRECT_MAPPING_CUSTOMER);

        /// <summary>
        /// Test de haut niveau du select.
        /// Test ORM-37. Configuration incorrecte du mapping : exception attendue.
        /// </summary>
        [TestMethod]
        [TestCategory("MsSql")]
        [TestCategory("Select")]
        [TestCategory("Mapping")]
        public void TestExecuteReaderIncorrectMapping()
        {
            try
            {
                // Customization
                Customizer.ConfigurationManagerSetKeyValue(Customizer.AppSettingsKeys.mappingFileName.ToString(), _incorrectMappingFileFullPath);
                // Reload modified configuration
                _config = ConfigurationLoader.Instance;
                // Dans la DB j'ai vérifié que cette requête donne un résultat, 'City' de valeur 'Paris'
                Customer customer = DbToolsSelects.SelectSingleAllColumns<Customer>("BaseReadAll", "Customer",
                    new List<string> {"City"}, new List<object> {"Paris"}, _transaction);
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
    }
}
