using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
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

        /// <summary>
        /// Donnée d'entrée de méthode à tester. Meta names corrects.
        /// </summary>
        readonly List<string> LstSyntaxticallyCorrectMetaNamesToProcess = new List<string>
        {
            // 1. propriété d'objet db entity
            "IdCustomer", 
            // 2. paramètre dynamique
            "#", 
            // 3. paramètre dynamique custom
            "@customValue",
            // 4. 2e paramètre dynamique
            "#",
            // 5. litéral
            "%chaine", 
            // 6. litéral
            "%chaine#{", 
            // 7. litéral
            "%chaine,", 
            // 8. litéral
            "%ma chaine",
            // 9. propriété d'objet db entity
            "FirstName", 
            // 10. propriété d'objet db entity
            "LastName", 
            // 11. propriété d'objet db entity
            "PostalCode", 
            // 12. propriété d'objet db entity préfixée par le nom de l'objet db entity
            "Customer:IdCustomer", 
            // 13. propriété d'objet db entity préfixée par le nom de l'objet db entity
            "Track:TrackId", 
            // 14. valeur null
            null, 
            // 15. unprotected literal
            "%UL%order by",
            // 16. chaîne vide
            ""
        };
        /// <summary>
        /// Donnée d'entrée de méthode à tester. Meta names incorrects.
        /// </summary>
        readonly List<string> lstSyntaxticallyIncorrectMetaNamesToProcess = new List<string> { "Customer::IdCustomer", "Customer:TrackId", "Customer:" };
        /// <summary>
        /// Donnée d'entrée de méthode à tester. Valeurs correspondant à la liste des meta names corrects ci dessus.
        /// </summary>
        readonly List<object> LstValuesForSyntaxticallyCorrectMetaNamesToProcess = new List<object>
        {
            // 1.
            3,
            // 2. 
            "test", 
            // 3.
            "test", 
            // 4.
            "test",
            // 9.
            "a name",
            // 10.
            "a last name",
            // 11.
            "6060",
            // 12.
            12,
            // 13.
            6,
            // 14.
            null,
            // 15. nom de colonne pour placeholder après "order by"
            "CustomerId",
            // 16. chaîne vide
            ""
        };

        /// <summary>
        /// Valeurs attendues en sortie de DeterminePlaceholderValue().
        /// 
        /// Column name, dynamic 0, paramname 1, dynamic 1, paramname 2, paramname 3...
        /// Look in mapping file : 
        /// - "FirstName" gives "FirstName, 'FirstName'" which will give "FirstName FirstName"
        /// - "LastName" gives "Last_Name" which will give "Last_Name"
        /// - "PostalCode" gives "Postal-Code" which will give "PostalCode"
        /// </summary>
        readonly List<string> LstExpectedStringForDeterminePlaceholderValue = new List<string>
        {
            // 1. nom de colonne correspondant à la propriété d'objet db entity
            "[CustomerId]", 
            // 2. paramètre dynamique
            "@p0", 
            // 3. paramètre dynamique custom avec mise en minuscules
            "@customvalue",
            // 4. 2e paramètre dynamique
            "@p1",
            // 5. litéral nettoyé
            "[chaine]", 
            // 6. litéral nettoyé
            "[chaine]", 
            // 7. litéral nettoyé
            "[chaine]", 
            // 8. litéral nettoyé
            "[ma chaine]", 
            // 9. nom de colonne correspondant à la propriété d'objet db entity
            "[FirstName FirstName]", 
            // 10. nom de colonne correspondant à la propriété d'objet db entity
            "[Last_Name]", 
            // 11. nom de colonne correspondant à lapropriété d'objet db entity
            "[PostalCode]", 
            // 12. nom de table et nom de colonne correspondant à la propriété d'objet db entity préfixée par le nom de l'objet db entity
            "[Customer].[CustomerId]", 
            // 13. nom de table et nom de colonne correspondant à la propriété d'objet db entity préfixée par le nom de l'objet db entity
            "[Track].[TrackId]", 
            // 14. valeur null
            null, 
            // 15. unprotected literal
            "order by",
            // 16. chaîne vide : valeur null
            null

        };

        /// <summary>
        /// Valeurs attendues en sortie de FillPlaceHoldersAndAdoParametersNamesAndValues.
        /// </summary>
        readonly List<string> LstExpectedStringForFillPlaceHoldersAndAdoParametersNamesAndValues = new List<string>
        {
            // 1. nom de colonne correspondant à la propriété d'objet db entity, protégé
            "[CustomerId]",
            // 2. paramètre dynamique
            "@p0",
            // 3. paramètre dynamique custom avec mise en minuscules
            "@customvalue",
            // 4. 2e paramètre dynamique
            "@p1", 
            // 5. litéral nettoyé et protégé
            "[chaine]", 
            // 6. litéral nettoyé et protégé
            "[chaine]", 
            // 7. litéral nettoyé et protégé
            "[chaine]", 
            // 8. litéral nettoyé et protégé
            "[ma chaine]", 
            // 9. nom de colonne correspondant à la propriété d'objet db entity, protégé
            "[FirstName FirstName]", 
            // 10. nom de colonne correspondant à la propriété d'objet db entity, protégé
            "[Last_Name]", 
            // 11. nom de colonne correspondant à la propriété d'objet db entity, protégé
            "[PostalCode]", 
            // 12. nom de table et nom de colonne correspondant à la propriété d'objet db entity préfixée par le nom de l'objet db entity, protégés
            "[Customer].[CustomerId]", 
            // 13. nom de table et nom de colonne correspondant à la propriété d'objet db entity préfixée par le nom de l'objet db entity, protégés
            "[Track].[TrackId]", 
            // 14. valeur null : rien 
            // 15. litéral unprotected
            "order by"
            // 16. valeur "" : rien
        };


        /// <summary>
        /// Test de DeterminePlaceholderValue qui détermine une chaîne et gère des compteurs incrémentaux.
        /// </summary>
        [TestMethod]
        [TestCategory("Meta name")]
        [Owner("Barbara Post")]
        public void TestDeterminePlaceholderValue()
        {
            List<string> lstFailures = new List<string>();
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

                List<string> lstResult = LstSyntaxticallyCorrectMetaNamesToProcess.Select(metaName_ => DbToolsCommon.DeterminePlaceholderValue(metaName_, "Customer", ref parameterIndex, ref parameterAutomaticNameIndex, out unprotectedLiteral)).ToList();

                Assert.AreEqual(LstSyntaxticallyCorrectMetaNamesToProcess.Count, lstResult.Count, "Même nombre d'éléments attendus en sortie que de meta names à traiter");


                for (int i = 0; i < LstExpectedStringForDeterminePlaceholderValue.Count; i++)
                {
                    try
                    {
                        bool ok = LstExpectedStringForDeterminePlaceholderValue[i] == lstResult[i];
                        if (!ok)
                            lstFailures.Add(LstExpectedStringForDeterminePlaceholderValue[i] + " attendu mais obtenu : " + lstResult[i]);
                    }
                    catch (Exception ex)
                    {
                        Assert.Fail("Avec ces valeurs de paramètres on ne doit pas avoir d'exception. Obtenu : " + ex.Message + " pour l'index de test  " + i);
                    }
                }
                
                if (lstFailures.Count > 0)
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (string f in lstFailures)
                    {
                        sb.Append(f).Append(Environment.NewLine);
                    }
                    throw new Exception(sb.ToString());
                }

                foreach (string metaName in lstSyntaxticallyIncorrectMetaNamesToProcess)
                {
                    bool exception = false;
                    try
                    {
                        DbToolsCommon.DeterminePlaceholderValue(metaName, "Customer", ref parameterIndex, ref parameterAutomaticNameIndex, out unprotectedLiteral);
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
        /// Test de FillPlaceHoldersAndAdoParametersNamesAndValues qui détermine une valeur pour le nom du paramètre ADO.NET et une valeur pour la valeur du paramètre ADO.NET.
        /// </summary>
        [TestMethod]
        [TestCategory("Meta name")]
        [Owner("Barbara Post")]
        public void TestFillPlaceHoldersAndAdoParametersNamesAndValues()
        {

            try
            {
                // Customization, ce fichier de mapping contient un nom de colonne pour "FirstName" pouvant mener à une injection SQL
                Customizer.ConfigurationManagerSetKeyValue(Customizer.AppSettingsKeys.mappingFileName.ToString(), _potentialSqlInjectionMappingFileFullPath);
                // Reload modified configuration
                ConfigurationLoader.Clear();
                _config = ConfigurationLoader.Instance;

                List<string> lstFailures = new List<string>();

                List<string> lstPlaceholders = new List<string>();
                List<KeyValuePair<string, object>> lstAdoNetValues = new List<KeyValuePair<string, object>>();
                DbToolsCommon.FillPlaceHoldersAndAdoParametersNamesAndValues("Customer", LstSyntaxticallyCorrectMetaNamesToProcess, LstValuesForSyntaxticallyCorrectMetaNamesToProcess, lstPlaceholders, lstAdoNetValues);

                Assert.AreEqual(LstSyntaxticallyCorrectMetaNamesToProcess.Count - 2, lstPlaceholders.Count, "Même nombre d'éléments attendus en sortie que de meta names à traiter, moins les éléments chaîne vide et null");

                try
                {
                    for (int i = 0; i < LstExpectedStringForFillPlaceHoldersAndAdoParametersNamesAndValues.Count; i++)
                    {
                        string metaName = LstSyntaxticallyCorrectMetaNamesToProcess[i];
                        if (string.IsNullOrWhiteSpace(metaName))
                        {
                            // pas de donnée correspondante dans la liste de sortie
                            continue;
                        }
                        bool ok = LstExpectedStringForFillPlaceHoldersAndAdoParametersNamesAndValues[i] == lstPlaceholders[i];
                        if (!ok)
                            lstFailures.Add(LstExpectedStringForFillPlaceHoldersAndAdoParametersNamesAndValues[i] + " attendu mais obtenu : " + lstPlaceholders[i]);
                    }
                }
                catch (Exception ex)
                {
                    Assert.Fail("Avec ces valeurs de paramètres on ne doit pas avoir d'exception. Obtenu : " + ex.Message);
                }

                if (lstFailures.Count > 0)
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (string f in lstFailures)
                    {
                        sb.Append(f).Append(Environment.NewLine);
                    }
                    throw new Exception(sb.ToString());
                }

            }

            finally
            {
                Customizer.ConfigurationManagerRestoreKey(Customizer.AppSettingsKeys.mappingFileName.ToString());
            }
        }
    }
}
