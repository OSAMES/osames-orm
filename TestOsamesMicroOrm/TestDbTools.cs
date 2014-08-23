using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OsamesMicroOrm.Configuration;
using OsamesMicroOrm.Configuration.Tweak;
using SampleDbEntities.Chinook;
using OsamesMicroOrm.DbTools;

namespace TestOsamesMicroOrm
{
    /// <summary>
    /// Tests unitaires de haut niveau des méthodes de formatage SQL, pas d'exécution.
    /// But : tester le mapping.
    /// </summary>
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class TestDbTools : OsamesMicroOrmTest
    {
        static Employee _employee = new Employee {LastName = "Doe", FirstName = "John"};
        static Customer _customer = new Customer();
        static Invoice _invoice = new Invoice();
        static InvoiceLine _invoiceLine = new InvoiceLine();
        static Track _track = new Track();

        /// <summary>
        /// Initialization once for all tests.
        /// </summary>
        [TestInitialize]
        public override void Setup()
        {
            // Obligatoire car ne prend pas en compte celui de la classe mère.
            var init = ConfigurationLoader.Instance;

            InitializeDbConnexion();

            _employee.Customer.Add(new Customer {FirstName = "toto"});
            foreach (var i in _employee.Customer)
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
        /// Test of FormatSqlFieldsListAsString with 2 values in list
        /// </summary>
        [TestMethod]
        [TestCategory("Sql formatting")]
        public void TestFormatSqlFieldsListAsString()
        {
            List<KeyValuePair<string, object>> adoParams = new List<KeyValuePair<string, object>>
                {
                    new KeyValuePair<string, object>("@firstname", "Barbara"),
                    new KeyValuePair<string, object>("@lastname", "Post")
                };
            StringBuilder sb;
            DbToolsCommon.FormatSqlFieldsListAsString(new List<string> { "FirstName", "LastName" }, out sb);

            Assert.IsFalse(string.IsNullOrWhiteSpace(sb.ToString()), "string builder empty");
            Assert.AreEqual(string.Format("{0}FirstName{1}, {0}LastName{1}", ConfigurationLoader.StartFieldEncloser, ConfigurationLoader.EndFieldEncloser), sb.ToString());
        }
        
        /// <summary>
        /// Test of DetermineDatabaseColumnNamesAndAdoParameters<T> with a single property.
        /// </summary>
        [TestMethod]
        [TestCategory("Mapping GetPropertyValue")]
        public void TestExtractFromProperty()
        {
            // DetermineDatabaseColumnNamesAndAdoParameters<T>(ref T dataObject_, string mappingDictionariesContainerKey_, string dataObjectPropertyName_, out string dbColumnName_, out KeyValuePair<string, object> adoParameterNameAndValue_)

            string dbColumnName;
            KeyValuePair<string, object> adoParams;
            DbToolsCommon.DetermineDatabaseColumnNameAndAdoParameter(ref _employee, "employee", "LastName", out dbColumnName, out adoParams);

            Assert.AreEqual("LastName", dbColumnName);

            Assert.AreEqual("@lastname", adoParams.Key);
            Assert.AreEqual(_employee.LastName, adoParams.Value);
        }

        /// <summary>
        /// Test of DetermineDatabaseColumnNamesAndAdoParameters<T> with a 2 properties.
        /// </summary>
        [TestMethod]
        [TestCategory("Mapping GetPropertyValue")]
        public void TestExtractFromPropertyMulti()
        {
            // DetermineDatabaseColumnNamesAndAdoParameters<T>(ref T dataObject_, string mappingDictionariesContainerKey_, List<string> lstDataObjectPropertyName_, out List<string> lstDbColumnName_, out List<KeyValuePair<string, object>> adoParameterNameAndValue_ )

            List<string> lstDbColumnNames;
            List<KeyValuePair<string, object>> adoParams;
            DbToolsCommon.DetermineDatabaseColumnNamesAndAdoParameters(ref _employee, "employee", new List<string> { "LastName", "FirstName" }, out lstDbColumnNames, out adoParams);

            Assert.AreEqual("LastName", lstDbColumnNames[0]);
            Assert.AreEqual("FirstName", lstDbColumnNames[1]);
            Assert.AreEqual(2, adoParams.Count, "no parameters generated");
            Assert.AreEqual("@lastname", adoParams[0].Key);
            Assert.AreEqual(_employee.LastName, adoParams[0].Value);
            Assert.AreEqual("@firstname", adoParams[1].Key);
            Assert.AreEqual(_employee.FirstName, adoParams[1].Value);
        }

        /// <summary>
        /// Test of FormatSqlForUpdate<T> with a list of 2 properties.
        /// </summary>
        [TestMethod]
        [TestCategory("Mapping")]
        [TestCategory("Sql formatting for Update")]
        [TestCategory("FIXME")]
        public void TestFormatSqlForUpdate()
        {
            try
            {

                // Utiliser la DB Sqlite
                Customizer.ConfigurationManagerSetKeyValue(Customizer.AppSettingsKeys.activeDbConnection.ToString(), "OsamesMicroORM.Sqlite");
                Customizer.ConfigurationManagerSetKeyValue(Customizer.AppSettingsKeys.dbName.ToString(), "Chinook_Sqlite.sqlite");

                // FormatSqlForUpdate<T>(ref T dataObject_, string mappingDictionariesContainerKey_, List<string> lstDataObjectPropertyName_, string primaryKeyPropertyName_, 
                //                        out string sqlCommand_, out List<KeyValuePair<string, object>> adoParameters_)

                string sqlCommand;
                List<KeyValuePair<string, object>> adoParams;
                DbToolsUpdates.FormatSqlForUpdate(ref _employee, "employee", "BaseUpdateOne", new List<string> { "LastName", "FirstName" }, new List<string> { "EmployeeId", null}, new List<object>{ 2 }, out sqlCommand, out adoParams);

                Assert.AreEqual("UPDATE [employee] SET [LastName] = @lastname, [FirstName] = @firstname WHERE [EmployeeId] = @p0;", sqlCommand);
                Assert.AreEqual(3, adoParams.Count, "no parameters generated");
                Assert.AreEqual("@lastname", adoParams[0].Key);
                Assert.AreEqual(_employee.LastName, adoParams[0].Value);
                Assert.AreEqual("@firstname", adoParams[1].Key);
                Assert.AreEqual(_employee.FirstName, adoParams[1].Value);
                Assert.AreEqual("@p0", adoParams[2].Key);
                Assert.AreEqual(2, adoParams[2].Value);
            } finally
            {
                Customizer.ConfigurationManagerRestoreAllKeys();
            }
        }

        /// <summary>
        /// Select avec clause where retournant un enregistrement.
        /// Ici le paramètre dynamique est représenté par "null".
        /// </summary>
        [TestMethod]
        [TestCategory("Mapping")]
        [TestCategory("Sql formatting for Select")]
        public void TestFormatSqlForSelect()
        {
            string sqlCommand;
            List<KeyValuePair<string, object>> adoParams;
            List<string> lstDbColumnNames;
            DbToolsSelects.FormatSqlForSelect("BaseReadWhere", new List<string> { "LastName", "FirstName", "Address" }, "employee", new List<string> { "EmployeeId", null }, new List<object> { 5 }, out sqlCommand, out adoParams, out lstDbColumnNames);

            Assert.AreEqual("SELECT [LastName], [FirstName], [Address] FROM [employee] WHERE [EmployeeId] = @p0;", sqlCommand);
            Assert.AreEqual(1, adoParams.Count);
            Assert.AreEqual("@p0", adoParams[0].Key);
            Assert.AreEqual(5, adoParams[0].Value);
            Assert.AreEqual(3, lstDbColumnNames.Count);

        }

        /// <summary>
        /// Select avec clause where retournant un enregistrement.
        /// Ici le paramètre dynamique est représenté par "@employeeId".
        /// </summary>
        [TestMethod]
        [TestCategory("Mapping")]
        [TestCategory("Sql formatting for Select")]
        public void TestFormatSqlForSelect2()
        {
            string sqlCommand;
            List<KeyValuePair<string, object>> adoParams;
            List<string> lstDbColumnNames;
            DbToolsSelects.FormatSqlForSelect("BaseReadWhere", new List<string> { "LastName", "FirstName", "Address" }, "employee", new List<string> { "EmployeeId", "@employeeId" }, new List<object> { 5 }, out sqlCommand, out adoParams, out lstDbColumnNames);

            Assert.AreEqual("SELECT [LastName], [FirstName], [Address] FROM [employee] WHERE [EmployeeId] = @employeeid;", sqlCommand);
            Assert.AreEqual(1, adoParams.Count);
            Assert.AreEqual("@employeeid", adoParams[0].Key);
            Assert.AreEqual(5, adoParams[0].Value);
            Assert.AreEqual(3, lstDbColumnNames.Count);

        }

        /// <summary>
        /// Select sans clause where retournant une liste d'enregistrements.
        /// </summary>
        [TestMethod]
        [TestCategory("Mapping")]
        [TestCategory("Sql formatting for Select")]
        public void TestFormatSqlForSelectMultipleRecordsWithoutWhere()
        {
            string sqlCommand;
            List<KeyValuePair<string, object>> adoParams;
            List<string> lstDbColumnNames;
            DbToolsSelects.FormatSqlForSelect("BaseRead", new List<string> { "LastName", "FirstName", "Address" }, "employee", null, null, out sqlCommand, out adoParams, out lstDbColumnNames);

            Assert.AreEqual("SELECT [LastName], [FirstName], [Address] FROM [employee];", sqlCommand);
            Assert.AreEqual(0, adoParams.Count);
            Assert.AreEqual(3, lstDbColumnNames.Count);

        }

        /// <summary>
        /// Test of DetermineDatabaseColumnsAndPropertiesNames for a given mapping.
        /// </summary>
        [TestMethod]
        [TestCategory("Mapping")]
        public void TestDetermineDatabaseColumnsAndPropertiesNames()
        {

            List<string> lstPropertiesNames;
            List<string> lstDbColumnNames;
            DbToolsCommon.DetermineDatabaseColumnsAndPropertiesNames("customer", out lstDbColumnNames, out lstPropertiesNames);
            Assert.IsFalse(lstDbColumnNames.Count == 0);
            Assert.IsFalse(lstPropertiesNames.Count == 0);
            Assert.AreEqual(lstDbColumnNames.Count, lstPropertiesNames.Count);
        }

        #endregion



    }
}
