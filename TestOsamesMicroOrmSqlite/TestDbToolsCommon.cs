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
using TestOsamesMicroOrmSqlite.Tools;

namespace TestOsamesMicroOrmSqlite
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class TestDbToolsCommon : OsamesMicroOrmSqliteTest
    {
        private readonly string _potentialSqlInjectionMappingFileFullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CommonSqlite.CST_POTENTIAL_SQL_INJECTION_MAPPING_CUSTOMER);

        readonly List<string> lstSyntaxticallyCorrectMetaNamesToProcess = new List<string> { "IdCustomer", "#", "@customValue", "#", "%chaine", "%chaine#{", "%chaine,", "%ma chaine", "FirstName", "LastName", "PostalCode", "Customer:IdCustomer", "Track:TrackId", null, "%UL%order by" };
        readonly List<string> lstSyntaxticallyIncorrectMetaNamesToProcess = new List<string> { "Customer::IdCustomer", "Customer:TrackId" };

      
        /// <summary>
        /// Valeurs attendues en sortie de DeterminePlaceholderType().
        /// 
        /// Column name, dynamic 0, paramname 1, dynamic 1, paramname 2, paramname 3...
        /// Look in mapping file : 
        /// - "FirstName" gives "FirstName, 'FirstName'" which will give "FirstName FirstName"
        /// - "LastName" gives "Last_Name" which will give "Last_Name"
        /// - "PostalCode" gives "Postal-Code" which will give "PostalCode"
        /// </summary>
        readonly List<string> LstExpectedStringForDeterminePlaceholderType = new List<string> { "CustomerId", "@p0", "@customvalue", "@p1", "chaine", "chaine", "chaine", "ma chaine", "FirstName FirstName", "Last_Name", "PostalCode", "Customer.CustomerId", "Track.TrackId", null, "%UL%order by" };

        /// <summary>
        /// Valeurs attendues en sortie de FillPlaceHoldersAndAdoParametersNamesAndValues.
        /// </summary>
        readonly List<string> LstExpectedStringForFillPlaceHoldersAndAdoParametersNamesAndValues = new List<string> { "[CustomerId]", "@p0", "@customvalue", "@p1", "chaine", "chaine", "chaine", "ma chaine", "[FirstName FirstName]", "[Last_Name]", "[PostalCode]", "[Customer].[CustomerId]", "[Track].[TrackId]", null, "order by" };


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
                
                bool unprotectedLiteral;

                List<string> lstResult = lstSyntaxticallyCorrectMetaNamesToProcess.Select(metaName_ => DbToolsCommon.DeterminePlaceholderType(metaName_, "Customer", ref parameterIndex, ref parameterAutomaticNameIndex, out unprotectedLiteral)).ToList();

                Assert.AreEqual(lstSyntaxticallyCorrectMetaNamesToProcess.Count, lstResult.Count, "Même nombre d'éléments");

 
                try
                {
                    for (int i = 0; i < LstExpectedStringForDeterminePlaceholderType.Count; i++)
                    {
                        Assert.AreEqual(LstExpectedStringForDeterminePlaceholderType[i], lstResult[i]);
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
                        DbToolsCommon.DeterminePlaceholderType(metaName, "Customer", ref parameterIndex, ref parameterAutomaticNameIndex, out unprotectedLiteral);
                    }
                    catch (OOrmHandledException ex)
                    {
                        exception = true;
                        Console.WriteLine(ex.Message);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        Assert.Fail("Only OormHandledException exceptions are expected to be thrown");
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

        /// <summary>
        /// Test de FillPlaceHoldersAndAdoParametersNamesAndValues qui détermine une chaîne.
        /// </summary>
        [TestMethod]
        [TestCategory("Meta name")]
        [Owner("Barbara Post")]
        [Ignore]
        public void TestFillPlaceHoldersAndAdoParametersNamesAndValues()
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

                bool unprotectedLiteral;

                List<string> lstResult = null; //TODO appel lstSyntaxticallyCorrectMetaNamesToProcess.Select(metaName_ => DbToolsCommon.FillPlaceHoldersAndAdoParametersNamesAndValues(metaName_, "Customer", ref parameterIndex, ref parameterAutomaticNameIndex, out unprotectedLiteral)).ToList();

                Assert.AreEqual(lstSyntaxticallyCorrectMetaNamesToProcess.Count, lstResult.Count, "Même nombre d'éléments");


                try
                {
                    for (int i = 0; i < LstExpectedStringForFillPlaceHoldersAndAdoParametersNamesAndValues.Count; i++)
                    {
                        Assert.AreEqual(LstExpectedStringForFillPlaceHoldersAndAdoParametersNamesAndValues[i], lstResult[i]);
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
                       // TODO DbToolsCommon.FillPlaceHoldersAndAdoParametersNamesAndValues(metaName, "Customer", ref parameterIndex, ref parameterAutomaticNameIndex, out unprotectedLiteral);
                    }
                    catch (OOrmHandledException ex)
                    {
                        exception = true;
                        Console.WriteLine(ex.Message);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        Assert.Fail("Only OormHandledException exceptions are expected to be thrown");
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
