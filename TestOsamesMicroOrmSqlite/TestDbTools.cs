using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
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
    public class TestDbTools : OsamesMicroOrmSqliteTest
    {
        private readonly string _incorrectMappingFileFullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CommonSqlite.CST_INCORRECT_MAPPING_CUSTOMER);
        private readonly string _potentialSqlInjectionMappingFileFullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CommonSqlite.CST_POTENTIAL_SQL_INJECTION_MAPPING_CUSTOMER);



       // TODO les différents tests seront à réintégrer ici.

        /// <summary>
        /// Test de haut niveau du Select.
        /// Test ORM-37. Configuration incorrecte du mapping : exception attendue.
        /// </summary>
        [TestMethod]
        [TestCategory("SqLite")]
        [TestCategory("Configuration NOK")]
        [TestCategory("Select")]
        public void TestExecuteReaderIncorrectMapping()
        {
            try
            {
                // Customization
                Customizer.ConfigurationManagerSetKeyValue(Customizer.AppSettingsKeys.mappingFileName.ToString(), _incorrectMappingFileFullPath);
                // Reload modified configuration
                ConfigurationLoader.Clear();
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

        /// <summary>
        /// Test de DeterminePlaceholderType qui détermine une chaîne et gère des compteurs incrémentaux.
        /// </summary>
        [TestMethod]
        [TestCategory("Meta name")]
        [Owner("Barbara Post")]
        public void TestDeterminePlaceholderType()
        {

            try
            {
                // Customization, ce fichier de mapping contient un nom de colonne pour "FirstName" pouvant mener à une injection SQL
                Customizer.ConfigurationManagerSetKeyValue(Customizer.AppSettingsKeys.mappingFileName.ToString(), _potentialSqlInjectionMappingFileFullPath);
                // Reload modified configuration
                ConfigurationLoader.Clear();
                _config = ConfigurationLoader.Instance;

                int parameterIndex = -1;
                int parameterAutomaticNameIndex = -1;

                List<string> lstMetaNamesToProcess = new List<string> {"CustomerId", null, "@customValue", null, "%chaine", "%chaine#{", "%chaine,", "%ma chaine", "FirstName"};
                List<string> lstResult = new List<string>();

                foreach (string metaName in lstMetaNamesToProcess)
                {
                    lstResult.Add(DbToolsCommon.DeterminePlaceholderType(metaName, "customer", ref parameterIndex, ref parameterAutomaticNameIndex));
                }

                Assert.AreEqual(lstMetaNamesToProcess.Count, lstResult.Count, "Même nombre d'éléments");

                //Column name, dynamic 0, paramname 1, dynamic 1, paramname 2, paramname 3...
                List<string> lstExpected = new List<string> {"CustomerId", "@p0", "@customvalue", "@p1", "chaine", "chaine", "chaine", "ma chaine", "FirstName FirstName"};

                for (int i = 0; i < lstExpected.Count; i++)
                {
                    Assert.AreEqual(lstExpected[i], lstResult[i]);
                }
            }

            finally
            {
                Customizer.ConfigurationManagerRestoreKey(Customizer.AppSettingsKeys.mappingFileName.ToString());
            }
        }
    }
}
