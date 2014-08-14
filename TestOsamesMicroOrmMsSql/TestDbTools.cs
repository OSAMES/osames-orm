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
using TestOsamesMicroOrmMsSql.Tools;

namespace TestOsamesMicroOrmMsSql
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class TestDbTools : OsamesMicroOrmMsSqlTest
    {
        private readonly string _incorrectMappingFileFullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CommonMsSql.CST_INCORRECT_MAPPING_CUSTOMER);

       // TODO les différents tests seront à réintégrer ici.

        /// <summary>
        /// Test ORM-37. Configuration incorrecte du mapping : exception attendue.
        /// </summary>
        [TestMethod]
        [TestCategory("MsSql")]
        [TestCategory("Sql Select")]
        [TestCategory("Configuration")]
        [TestCategory("Sql execution")]
        public void TestExecuteReaderIncorrectMapping()
        {
            try
            {
                // Customization
                Customizer.ConfigurationManagerSetKeyValue(Customizer.AppSettingsKeys.mappingFileName.ToString(), _incorrectMappingFileFullPath);
                // Reload modified configuration
                _config = ConfigurationLoader.Instance;
                // Dans la DB j'ai vérifié que cette requête donne un résultat, 'City' de valeur 'Paris'
                Customer customer = DbToolsSelects.SelectSingleAllColumns<Customer>("BaseReadAll", "customer",
                    new List<string> {"City"}, new List<object> {"Paris"});
                Assert.IsNotNull(customer, "Pas d'enregistrement trouvé, requeête select à corriger");
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
