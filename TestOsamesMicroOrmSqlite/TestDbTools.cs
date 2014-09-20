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
    public class TestDbTools : OsamesMicroOrmSqliteTest
    {
        private readonly string _incorrectMappingFileFullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CommonSqlite.CST_INCORRECT_MAPPING_CUSTOMER);
        private readonly string _potentialSqlInjectionMappingFileFullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CommonSqlite.CST_POTENTIAL_SQL_INJECTION_MAPPING_CUSTOMER);



        // TODO les différents tests seront à réintégrer ici.

        /// <summary>
        /// Test de haut niveau du Select avec auto-détermination des propriétés et colonnes.
        /// Test ORM-37. Configuration incorrecte du mapping : exception attendue.
        /// </summary>
        [TestMethod]
        [TestCategory("SqLite")]
        [TestCategory("Configuration NOK")]
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
                    new List<string> { "City" }, new List<object> { "Paris" });
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
              new List<string> { "CustomerId" }, new List<object> { 1 });
            Assert.IsNotNull(customer, "Pas d'enregistrement trouvé, requête select à corriger");

            // TODO les asserts
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

                List<string> lstSyntaxticallyCorrectMetaNamesToProcess = new List<string> { "CustomerId", "#", "@customValue", "#", "%chaine", "%chaine#{", "%chaine,", "%ma chaine", "FirstName", "LastName", "PostalCode", "Customer:CustomerId", "Track:TrackId" };
                List<string> lstSyntaxticallyIncorrectMetaNamesToProcess = new List<string> { null, "Customer::CustomerId", "Customer:TrackId" };


                List<string> lstResult = lstSyntaxticallyCorrectMetaNamesToProcess.Select(metaName_ => DbToolsCommon.DeterminePlaceholderType(metaName_, "Customer", ref parameterIndex, ref parameterAutomaticNameIndex)).ToList();

                Assert.AreEqual(lstSyntaxticallyCorrectMetaNamesToProcess.Count, lstResult.Count, "Même nombre d'éléments");

                //Column name, dynamic 0, paramname 1, dynamic 1, paramname 2, paramname 3...
                //Look in mapping file : 
                // - "FirstName" gives "FirstName, 'FirstName'" which will give "FirstName FirstName"
                // - "LastName" gives "Last_Name" which will give "Last_Name"
                // - "PostalCode" gives "Postal-Code" which will give "PostalCode"
                List<string> lstExpected = new List<string> { "CustomerId", "@p0", "@customvalue", "@p1", "chaine", "chaine", "chaine", "ma chaine", "FirstName FirstName", "Last_Name", "PostalCode", "Customer.CustomerId", "Track.TrackId" };

                try
                {
                    for (int i = 0; i < lstExpected.Count; i++)
                    {
                        Assert.AreEqual(lstExpected[i], lstResult[i]);
                    }
                }
                catch (Exception ex)
                {
                    Assert.Fail("Avec ces valeurs de paramètres on ne doit pas avoir d'exception. Obtenu : " + ex.Message);
                }

                foreach (string metaName in lstSyntaxticallyIncorrectMetaNamesToProcess)
                {
                    bool exception = false;
                    try
                    {
                        DbToolsCommon.DeterminePlaceholderType(metaName, "Customer", ref parameterIndex, ref parameterAutomaticNameIndex);
                    }
                    catch (Exception ex)
                    {
                        exception = true;
                        Console.WriteLine(ex.Message);
                    }
                    finally
                    {
                        Assert.IsTrue(exception, "Exception attendue sur valeur testée : " + metaName);
                    }

                }

            }

            finally
            {
                Customizer.ConfigurationManagerRestoreKey(Customizer.AppSettingsKeys.mappingFileName.ToString());
            }
        }
    }
}
