using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OsamesMicroOrm;
using OsamesMicroOrm.Configuration;
using OsamesMicroOrm.DbTools;
using SampleDbEntities.Chinook;
using TestOsamesMicroOrm.Tools;

namespace TestOsamesMicroOrm
{
    /// <summary>
    /// Tests unitaires de haut niveau des méthodes de formatage SQL, pas d'exécution.
    /// But : tester le mapping.
    /// </summary>
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class TestDbToolsCommon : OsamesMicroOrmTest
    {
        static Employee Employee = new Employee { LastName = "Doe", FirstName = "John" };
        static Customer Customer = new Customer();
        static Invoice Invoice = new Invoice();
        static InvoiceLine InvoiceLine = new InvoiceLine();
        static Track Track = new Track();

        /// <summary>
        /// Initialization once for all tests.
        /// </summary>
        [TestInitialize]
        public override void Setup()
        {
            // Obligatoire car ne prend pas en compte celui de la classe mère.
            var init = ConfigurationLoader.Instance;

            // Pas de DB déployée donc ne pas appeler InitializeDbConnexion();

            Employee.Customer.Add(new Customer { FirstName = "toto" });
            foreach (var i in Employee.Customer)
            {
                //ici faire un select pour chaque invoice qui est lié à l'id de customer
                i.Invoice.Add(new Invoice());
                foreach (var j in i.Invoice)
                {
                    //ici faire un select pour chaque invoiceline qui est lié à l'id de invoice
                    j.InvoiceLine.Add(new InvoiceLine());
                    foreach (var k in j.InvoiceLine)
                    {
                        k.Track.Name = "totot";
                    }
                }
            }
        }

        #region test sql formatting

        /// <summary>
        /// Test of GenerateCommaSeparatedDbFieldsString with 2 values in list
        /// </summary>
        [TestMethod]
        [TestCategory("Sql formatting")]
        public void TestGenerateCommaSeparatedDbFieldsString()
        {
            List<KeyValuePair<string, object>> adoParams = new List<KeyValuePair<string, object>>
                {
                    new KeyValuePair<string, object>("@firstname", "Barbara"),
                    new KeyValuePair<string, object>("@lastname", "Post")
                };
            string str = DbToolsCommon.GenerateCommaSeparatedDbFieldsString(new List<string> { "FirstName", "LastName" });

            Assert.IsFalse(string.IsNullOrWhiteSpace(str), "string builder empty");
            Assert.AreEqual(string.Format("{0}FirstName{1}, {0}LastName{1}", ConfigurationLoader.StartFieldEncloser, ConfigurationLoader.EndFieldEncloser), str.ToString());
        }

        /// <summary>
        /// Test of DetermineDatabaseColumnNamesAndAdoParameters<T> with a single property.
        /// </summary>
        [TestMethod]
        [TestCategory("Mapping GetPropertyValue")]
        public void TestDetermineDatabaseColumnNameAndAdoParameter()
        {
            // DetermineDatabaseColumnNamesAndAdoParameters<T>(T databaseEntityObject_, string mappingDictionariesContainerKey_, string dataObjectPropertyName_, out string dbColumnName_, out KeyValuePair<string, object> adoParameterNameAndValue_)

            string dbColumnName;
            KeyValuePair<string, object> adoParams;
            DbToolsCommon.DetermineDatabaseColumnNameAndAdoParameter(Employee, "Employee", "LastName", out dbColumnName, out adoParams);

            // Ici on devra faire un Assert.IsTrue du retour de la méthode (méthode à modifier pour retourner true/false) - ORM #85

            Assert.AreEqual("LastName", dbColumnName);

            Assert.AreEqual("@lastname", adoParams.Key);
            Assert.AreEqual(Employee.LastName, adoParams.Value);
        }

        /// <summary>
        /// Test of DetermineDatabaseColumnNamesAndAdoParameters<T> with a single property and an empty string value.
        /// </summary>
        [TestMethod]
        [TestCategory("Mapping GetPropertyValue")]
        public void TestDetermineDatabaseColumnNameAndAdoParameter2()
        {
            // DetermineDatabaseColumnNamesAndAdoParameters<T>(T databaseEntityObject_, string mappingDictionariesContainerKey_, string dataObjectPropertyName_, out string dbColumnName_, out KeyValuePair<string, object> adoParameterNameAndValue_)

            string dbColumnName;
            KeyValuePair<string, object> adoParams;
            Employee.LastName = "";
            DbToolsCommon.DetermineDatabaseColumnNameAndAdoParameter(Employee, "Employee", "LastName", out dbColumnName, out adoParams);

            // Ici on devra faire un Assert.IsTrue du retour de la méthode (méthode à modifier pour retourner true/false) - ORM #85

            Assert.AreEqual("LastName", dbColumnName);

            Assert.AreEqual("@lastname", adoParams.Key);
            Assert.AreEqual(null, adoParams.Value);
            Assert.AreNotEqual(Employee.LastName, adoParams.Value);
        }

        /// <summary>
        /// Test of DetermineDatabaseColumnNamesAndAdoParameters<T> with a single property.
        /// </summary>
        [TestMethod]
        [TestCategory("Mapping GetPropertyValue")]
        [TestCategory("Parameter NOK")]
        [ExpectedException(typeof(OOrmHandledException))]
        public void TestDetermineDatabaseColumnNameAndAdoParameterWrongDictionaryName()
        {
            // DetermineDatabaseColumnNamesAndAdoParameters<T>(T databaseEntityObject_, string mappingDictionariesContainerKey_, string dataObjectPropertyName_, out string dbColumnName_, out KeyValuePair<string, object> adoParameterNameAndValue_)

            string dbColumnName;
            KeyValuePair<string, object> adoParams;
            DbToolsCommon.DetermineDatabaseColumnNameAndAdoParameter(Employee, "NotAnEmployee", "LastName", out dbColumnName, out adoParams);
        }

        /// <summary>
        /// Test of DetermineDatabaseColumnNamesAndAdoParameters<T> with 2 properties.
        /// </summary>
        [TestMethod]
        [TestCategory("Mapping GetPropertyValue")]
        public void TestDetermineDatabaseColumnNamesAndAdoParameters()
        {
            // DetermineDatabaseColumnNamesAndAdoParameters<T>(T databaseEntityObject_, string mappingDictionariesContainerKey_, List<string> lstDataObjectPropertyName_, out List<string> lstDbColumnName_, out List<KeyValuePair<string, object>> adoParameterNameAndValue_ )

            List<string> lstDbColumnNames;
            List<KeyValuePair<string, object>> adoParams;
            DbToolsCommon.DetermineDatabaseColumnNamesAndAdoParameters(Employee, "Employee", new List<string> { "LastName", "FirstName" }, out lstDbColumnNames, out adoParams);

            Assert.AreEqual("LastName", lstDbColumnNames[0]);
            Assert.AreEqual("FirstName", lstDbColumnNames[1]);
            Assert.AreEqual(2, adoParams.Count, "no parameters generated");
            Assert.AreEqual("@lastname", adoParams[0].Key);
            Assert.AreEqual(Employee.LastName, adoParams[0].Value);
            Assert.AreEqual("@firstname", adoParams[1].Key);
            Assert.AreEqual(Employee.FirstName, adoParams[1].Value);
        }

        /// <summary>
        /// Test of DetermineDatabaseColumnNamesAndAdoParameters<T> with 2 properties.
        /// Mauvais nom de dictionnaire.
        /// </summary>
        [TestMethod]
        [TestCategory("Mapping GetPropertyValue")]
        [TestCategory("Parameter NOK")]
        [ExpectedException(typeof(OOrmHandledException))]
        public void TestDetermineDatabaseColumnNamesAndAdoParametersWrongDictionaryName()
        {
            try
            {
                // DetermineDatabaseColumnNamesAndAdoParameters<T>(T databaseEntityObject_, string mappingDictionariesContainerKey_, List<string> lstDataObjectPropertyName_, out List<string> lstDbColumnName_, out List<KeyValuePair<string, object>> adoParameterNameAndValue_ )

                List<string> lstDbColumnNames;
                List<KeyValuePair<string, object>> adoParams;
                DbToolsCommon.DetermineDatabaseColumnNamesAndAdoParameters(Employee, "NotAnEmployee", new List<string> { "LastName", "FirstName" }, out lstDbColumnNames, out adoParams);
            }
            catch (OOrmHandledException ex)
            {
                Common.AssertOnHresultAndWriteToConsole(HResultEnum.E_NOMAPPINGKEY, ex);
                throw;
            }
        }
       
        #endregion

        #region test Determine

        /// <summary>
        /// Test of DetermineDatabaseColumnName for a given mapping.
        /// </summary>
        [TestMethod]
        [TestCategory("Mapping")]
        public void TestDetermineDatabaseColumnName()
        {
            string columnName;
            DbToolsCommon.DetermineDatabaseColumnName("Customer", "LastName", out columnName);
            Assert.AreEqual("LastName", columnName);
        }

        /// <summary>
        /// 
        /// </summary>
        [TestMethod]
        [TestCategory("Mapping")]
        public void TestDetermineDatabaseColumnNames()
        {
            List<string> columnsNames;
            DbToolsCommon.DetermineDatabaseColumnNames("Customer", new List<string> { "FirstName", "LastName" }, out columnsNames);
        }

        /// <summary>
        /// Test of DetermineDatabaseColumnName for a given mapping.
        /// On demande le mauvais nom de dictionnaire et deux propriétés.
        /// </summary>
        [TestMethod]
        [TestCategory("Mapping")]
        [TestCategory("Parameter NOK")]
        [ExpectedException(typeof(OOrmHandledException))]
        public void TestDetermineDatabaseColumnNamesNok()
        {
            try
            {
                List<string> columnsNames;
                DbToolsCommon.DetermineDatabaseColumnNames("NotCustomer", new List<string> { "FirstName", "LastName" }, out columnsNames);
            }
            catch (OOrmHandledException ex)
            {
                Common.AssertOnHresultAndWriteToConsole(HResultEnum.E_NOMAPPINGKEY, ex);
                throw;
            }
        }

        /// <summary>
        /// Test of DetermineDatabaseColumnName for a given mapping.
        /// On demande le mauvais nom de dictionnaire et une propriété.
        /// </summary>
        [TestMethod]
        [TestCategory("Mapping")]
        [TestCategory("Parameter NOK")]
        [ExpectedException(typeof(OOrmHandledException))]
        public void TestDetermineDatabaseColumnNameWrongDictionaryName()
        {
            try
            {
                string columnName;
                DbToolsCommon.DetermineDatabaseColumnName("NotACustomer", "LastName", out columnName);
            }
            catch (OOrmHandledException ex)
            {
                Common.AssertOnHresultAndWriteToConsole(HResultEnum.E_NOMAPPINGKEY, ex);
                throw;
            }
        }

        /// <summary>
        /// Test of DetermineDatabaseColumnNamesAndDataObjectPropertyNames for a given mapping.
        /// </summary>
        [TestMethod]
        [TestCategory("Mapping")]
        public void TestDetermineDatabaseColumnNamesAndDataObjectPropertyNames()
        {
            List<string> lstPropertiesNames;
            List<string> lstDbColumnNames;
            DbToolsCommon.DetermineDatabaseColumnNamesAndDataObjectPropertyNames("Customer", out lstDbColumnNames, out lstPropertiesNames);

            Assert.IsFalse(lstDbColumnNames.Count == 0);
            Assert.IsFalse(lstPropertiesNames.Count == 0);
            Assert.AreEqual(lstDbColumnNames.Count, lstPropertiesNames.Count);
        }

        /// <summary>
        /// Test of DetermineDatabaseColumnNamesAndDataObjectPropertyNames for a given mapping.
        /// Mauvais nom de dictionnaire demandé.
        /// </summary>
        [TestMethod]
        [TestCategory("Mapping")]
        [TestCategory("Parameter NOK")]
        [ExpectedException(typeof(OOrmHandledException))]
        public void TestDetermineDatabaseColumnNamesAndDataObjectPropertyNamesWrongDictionaryName()
        {
            try
            {
                List<string> lstDbColumnNames;
                List<string> lstPropertiesNames;
                DbToolsCommon.DetermineDatabaseColumnNamesAndDataObjectPropertyNames("NotACustomer", out lstDbColumnNames, out lstPropertiesNames);
            }
            catch (OOrmHandledException ex)
            {
                Common.AssertOnHresultAndWriteToConsole(HResultEnum.E_NOMAPPINGKEY, ex);
                throw;
            }
        }

        #endregion

    }
}
