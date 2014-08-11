using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OsamesMicroOrm;
using OsamesMicroOrm.Configuration;
using SampleDbEntities.Chinook;
using TestOsamesMicroOrm.TestDbEntities;
using SampleDbEntities;
using OsamesMicroOrm.DbTools;

namespace TestOsamesMicroOrm
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class TestDbTools : OsamesMicroOrmTest
    {
        static TestClient _client = new TestClient();

        static Employee _employee = new Employee();
        static Customer _customer = new Customer();
        static Invoice _invoice = new Invoice();
        static InvoiceLine _invoiceLine = new InvoiceLine();
        static Track _track = new Track();

        /// <summary>
        /// Initialization once for all tests.
        /// </summary>
        [ClassInitialize]
        public static void Setup(TestContext context_)
        {

            var init = ConfigurationLoader.Instance;

            _client.IdClient = 1;
            _client.NomSociete = "Test";
            _client.ClientDate = new DateTime(2005,8,15);
            _client.Email = "pasde@mail.com";
            _client.Ape = "";
            _client.Fax = "";
            _client.IdAdresseFacturationRef = 1;
            _client.IdConditionReglementRef = 2;
            _client.IdContactPrincipalRef = 1;
            _client.IdContactFacturationRef = 1;
            _client.IdTypeReglementRef = 1;
            _client.Siret = "";
            _client.SiteWeb = "";
            _client.Telephone = "";
            _client.Tva = "";
            _client.Adresses.Add(new TestAdresse {IdAdresse = 1, Country = "Belgique", IdClient = 1, IsFacturation = false, NumberStreetName = "45", Town = "Bruxelles", ZipCode = "1000"});

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
        /// Test of FormatSqlNameEqualValueString with a single value.
        /// </summary>
        [TestMethod]
        [TestCategory("Sql formatting")]
        public void TestFormatSqlNameEqualValueStringParam()
        {
            KeyValuePair<string, object> adoParams = new KeyValuePair<string, object>("@firstname", "Barbara");
            StringBuilder sb = new StringBuilder();
            DbToolsCommon.FormatSqlNameEqualValueString("FirstName", adoParams, ref sb);

            Assert.IsFalse(string.IsNullOrWhiteSpace(sb.ToString()), "string builder empty");
            Assert.AreEqual(string.Format("{0}FirstName{1} = @firstname", ConfigurationLoader.StartFieldEncloser, ConfigurationLoader.EndFieldEncloser), sb.ToString());
        }

        /// <summary>
        /// Test of FormatSqlNameEqualValueString with 2 values.
        /// </summary>
        [TestMethod]
        [TestCategory("Sql formatting")]
        public void TestFormatSqlNameEqualValueListParam()
        {
            List<KeyValuePair<string, object>> adoParams = new List<KeyValuePair<string, object>>
                {
                    new KeyValuePair<string, object>("@firstname", "Barbara"),
                    new KeyValuePair<string, object>("@lastname", "Post")
                };
            StringBuilder sb = new StringBuilder();
            DbToolsCommon.FormatSqlNameEqualValueString(new List<string> { "FirstName", "LastName" }, adoParams, ref sb, ", ");

            Assert.IsFalse(string.IsNullOrWhiteSpace(sb.ToString()), "string builder empty");
            Assert.AreEqual(string.Format("{0}FirstName{1} = @firstname, {0}LastName{1} = @lastname", ConfigurationLoader.StartFieldEncloser, ConfigurationLoader.EndFieldEncloser), sb.ToString());
        }

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
        [TestCategory("Mapping")]
        [TestCategory("GetPropertyValue")]
        public void TestExtractFromProperty()
        {
            // DetermineDatabaseColumnNamesAndAdoParameters<T>(ref T dataObject_, string mappingDictionariesContainerKey_, string dataObjectPropertyName_, out string dbColumnName_, out KeyValuePair<string, object> adoParameterNameAndValue_)

            string dbColumnName;
            KeyValuePair<string, object> adoParams;
            DbToolsCommon.DetermineDatabaseColumnNameAndAdoParameter(ref _client, "Employee", "LastName", out dbColumnName, out adoParams);

            Assert.AreEqual("LastName", dbColumnName);

            Assert.AreEqual("@lastname", adoParams.Key);
            Assert.AreEqual(_client.NomSociete, adoParams.Value);
        }

        /// <summary>
        /// Test of DetermineDatabaseColumnNamesAndAdoParameters<T> with a 2 properties.
        /// </summary>
        [TestMethod]
        [TestCategory("Mapping")]
        [TestCategory("GetPropertyValue")]
        public void TestExtractFromPropertyMulti()
        {
            // DetermineDatabaseColumnNamesAndAdoParameters<T>(ref T dataObject_, string mappingDictionariesContainerKey_, List<string> lstDataObjectPropertyName_, out List<string> lstDbColumnName_, out List<KeyValuePair<string, object>> adoParameterNameAndValue_ )

            List<string> lstDbColumnNames;
            List<KeyValuePair<string, object>> adoParams;
            DbToolsCommon.DetermineDatabaseColumnNamesAndAdoParameters(ref _client, "Employee", new List<string> { "LastName", "FirstName" }, out lstDbColumnNames, out adoParams);

            Assert.AreEqual("LastName", lstDbColumnNames[0]);
            Assert.AreEqual("FirstName", lstDbColumnNames[1]);
            Assert.AreEqual(2, adoParams.Count, "no parameters generated");
            Assert.AreEqual("@lastname", adoParams[0].Key);
            Assert.AreEqual(_client.NomSociete, adoParams[0].Value);
            Assert.AreEqual("@firstname", adoParams[1].Key);
            Assert.AreEqual(_client.ClientDate, adoParams[1].Value);
        }

        /// <summary>
        /// Test of FormatSqlForUpdate<T> with a list of 2 properties.
        /// </summary>
        [TestMethod]
        [TestCategory("Mapping")]
        [TestCategory("Sql formatting")]
        [TestCategory("FIXME")]
        public void TestFormatSqlForUpdate()
        {
            // FormatSqlForUpdate<T>(ref T dataObject_, string mappingDictionariesContainerKey_, List<string> lstDataObjectPropertyName_, string primaryKeyPropertyName_, 
            //                        out string sqlCommand_, out List<KeyValuePair<string, object>> adoParameters_)

            // TODO FIXME : échec du test sur une liste à 1 élément et l'autre à 0 dans DbTools > FormatSqlNameEqualValueString

            string sqlCommand;
            List<KeyValuePair<string, object>> adoParams;
            DbToolsUpdates.FormatSqlForUpdate(ref _client, "Employee", new List<string> { "LastName", "FirstName" }, "EmployeeId", out sqlCommand, out adoParams);

            Assert.AreEqual("UPDATE [Employee] SET [LastName] = @nomsociete, [FirstName] = @firstname WHERE [EmployeeId] = @employeeid;", sqlCommand);
            Assert.AreEqual(2, adoParams.Count, "no parameters generated");
            Assert.AreEqual("@lastname", adoParams[0].Key);
            Assert.AreEqual(_client.NomSociete, adoParams[0].Value);
            Assert.AreEqual("@firstname", adoParams[1].Key);
            Assert.AreEqual(_client.ClientDate, adoParams[1].Value);
        }

        /// <summary>
        /// Select avec clause where retournant un enregistrement.
        /// Ici le paramètre dynamique est représenté par "null".
        /// </summary>
        [TestMethod]
        [TestCategory("Mapping")]
        [TestCategory("Sql formatting")]
        public void TestFormatSqlForSelect()
        {
            string sqlCommand;
            List<KeyValuePair<string, object>> adoParams;
            List<string> lstDbColumnNames;
            DbToolsSelects.FormatSqlForSelect("BaseReadWhere", new List<string> { "LastName", "FirstName", "Address" }, "Employee", new List<string> { "EmployeeId", null }, new List<object> { 5 }, out sqlCommand, out adoParams, out lstDbColumnNames);

            Assert.AreEqual("SELECT [LastName], [FirstName], [Address] FROM [Employee] WHERE [EmployeeId] = @p0;", sqlCommand);
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
        [TestCategory("Sql formatting")]
        public void TestFormatSqlForSelect2()
        {
            string sqlCommand;
            List<KeyValuePair<string, object>> adoParams;
            List<string> lstDbColumnNames;
            DbToolsSelects.FormatSqlForSelect("BaseReadWhere", new List<string> { "LastName", "FirstName", "Address" }, "Employee", new List<string> { "EmployeeId", "@employeeId" }, new List<object> { 5 }, out sqlCommand, out adoParams, out lstDbColumnNames);

            Assert.AreEqual("SELECT [LastName], [FirstName], [Address] FROM [Employee] WHERE [EmployeeId] = @employeeid;", sqlCommand);
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
        [TestCategory("Sql formatting")]
        public void TestFormatSqlForSelectMultipleRecordsWithoutWhere()
        {
            string sqlCommand;
            List<KeyValuePair<string, object>> adoParams;
            List<string> lstDbColumnNames;
            DbToolsSelects.FormatSqlForSelect("BaseRead", new List<string> { "LastName", "FirstName", "Address" }, "Employee", null, null, out sqlCommand, out adoParams, out lstDbColumnNames);

            Assert.AreEqual("SELECT [LastName], [FirstName], [Address] FROM [Employee];", sqlCommand);
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
            DbToolsCommon.DetermineDatabaseColumnsAndPropertiesNames("Customer", out lstDbColumnNames, out lstPropertiesNames);
            Assert.IsFalse(lstDbColumnNames.Count == 0);
            Assert.IsFalse(lstPropertiesNames.Count == 0);
            Assert.AreEqual(lstDbColumnNames.Count, lstPropertiesNames.Count);
        }

 /*       /// <summary>
        /// Tested template : "update {0} set {1} where {2} = {3}"
        /// </summary>
        [TestMethod]
        [ExcludeFromCodeCoverage]
        [Owner("Barbara Post")]
        public void TestDbToolsFormatUpdateSimpleWhere()
        {

            List<KeyValuePair<string, object>> testingkvp = new List<KeyValuePair<string, object>>();

            testingkvp.Add(new KeyValuePair<string, object>("colA", "valueA"));
            testingkvp.Add(new KeyValuePair<string, object>("colB", "valueB"));
            testingkvp.Add(new KeyValuePair<string, object>("colC", "valueC"));

            Query query = DbTools.FormatUpdate("testtable", "update {0} set {1} where {2} = {3}",
                   testingkvp, new List<string> { "id_testtable", null }, new List<object> { 33 });
            Assert.IsNotNull(query, "Expected no incoherence, query object not null");

            Assert.AreEqual("update testtable set colA = @p0, colB = @p1, colC = @p2 where id_testtable = @p3", query.SqlString);
            Assert.AreEqual(4, query.SqlValues.Count, "Attendu 4 valeurs de paramètres");
            Assert.AreEqual("valueA", query.SqlValues[0]);
            Assert.AreEqual("valueB", query.SqlValues[1]);
            Assert.AreEqual("valueC", query.SqlValues[2]);
            Assert.AreEqual(33, query.SqlValues[3]);
        }

        /// <summary>
        /// Tested template : "update {0} set {1}"
        /// </summary>
        [TestMethod]
        [ExcludeFromCodeCoverage]
        [Owner("Barbara Post")]
        public void TestDbToolsFormatUpdateWithoutWhere()
        {

            List<KeyValuePair<string, object>> testingkvp = new List<KeyValuePair<string, object>>();

            testingkvp.Add(new KeyValuePair<string, object>("colA", "valueA"));
            testingkvp.Add(new KeyValuePair<string, object>("colB", "valueB"));
            testingkvp.Add(new KeyValuePair<string, object>("colC", "valueC"));

            Query query = DbTools.FormatUpdate("testtable", "update {0} set {1}",
                   testingkvp, null, null);
            Assert.IsNotNull(query, "Expected no incoherence, query object not null");

            Assert.AreEqual("update testtable set colA = @p0, colB = @p1, colC = @p2", query.SqlString);
            Assert.AreEqual(3, query.SqlValues.Count, "Attendu 3 valeurs de paramètres");
            Assert.AreEqual("valueA", query.SqlValues[0]);
            Assert.AreEqual("valueB", query.SqlValues[1]);
            Assert.AreEqual("valueC", query.SqlValues[2]);
        }

        /// <summary>
        /// Tested template : "update {0} set {1} where {2} = {3} and {4} = {5}"
        /// </summary>
        [TestMethod]
        [ExcludeFromCodeCoverage]
        [Owner("Barbara Post")]
        public void TestDbToolsFormatUpdateDoubleWhere()
        {
            List<KeyValuePair<string, object>> testingkvp = new List<KeyValuePair<string, object>>();

            testingkvp.Add(new KeyValuePair<string, object>("colA", "valueA"));
            testingkvp.Add(new KeyValuePair<string, object>("colB", "valueB"));
            testingkvp.Add(new KeyValuePair<string, object>("colC", "valueC"));

            Query query = DbTools.FormatUpdate("testtable", "update {0} set {1} where {2} = {3} and {4} = {5}",
                   testingkvp, new List<string> { "id_testtable", null, "testCol", null }, new List<object> { 33, "test" });
            Assert.IsNotNull(query, "Expected no incoherence, query object not null");

            Assert.AreEqual("update testtable set colA = @p0, colB = @p1, colC = @p2 where id_testtable = @p3 and testCol = @p4", query.SqlString);
            Assert.AreEqual(5, query.SqlValues.Count, "Attendu 5 valeurs de paramètres");
            Assert.AreEqual("valueA", query.SqlValues[0]);
            Assert.AreEqual("valueB", query.SqlValues[1]);
            Assert.AreEqual("valueC", query.SqlValues[2]);
            Assert.AreEqual(33, query.SqlValues[3]);
            Assert.AreEqual("test", query.SqlValues[4]);
        }

        /// <summary>
        /// Tested template : "update {0} set {1} where {2} is null"
        /// </summary>
        [TestMethod]
        [ExcludeFromCodeCoverage]
        [Owner("Barbara Post")]
        public void TestDbToolsFormatUpdateWhereIsNull()
        {

            List<KeyValuePair<string, object>> testingkvp = new List<KeyValuePair<string, object>>();

            testingkvp.Add(new KeyValuePair<string, object>("colA", "valueA"));
            testingkvp.Add(new KeyValuePair<string, object>("colB", "valueB"));
            testingkvp.Add(new KeyValuePair<string, object>("colC", "valueC"));

            Query query = DbTools.FormatUpdate("testtable", "update {0} set {1} where {2} is null",
                   testingkvp, new List<string> { "testCol" }, null);
            Assert.IsNotNull(query, "Expected no incoherence, query object not null");

            Assert.AreEqual("update testtable set colA = @p0, colB = @p1, colC = @p2 where testCol is null", query.SqlString);
            Assert.AreEqual(3, query.SqlValues.Count, "Attendu 3 valeurs de paramètres");
            Assert.AreEqual("valueA", query.SqlValues[0]);
            Assert.AreEqual("valueB", query.SqlValues[1]);
            Assert.AreEqual("valueC", query.SqlValues[2]);
        }

        /// <summary>
        /// Tested template : "update {0} set {1} where {2} > {3}"
        /// </summary>
        [TestMethod]
        [ExcludeFromCodeCoverage]
        [Owner("Barbara Post")]
        public void TestDbToolsFormatUpdateWhereCompareTwoColumns()
        {

            List<KeyValuePair<string, object>> testingkvp = new List<KeyValuePair<string, object>>();

            testingkvp.Add(new KeyValuePair<string, object>("colA", "valueA"));
            testingkvp.Add(new KeyValuePair<string, object>("colB", "valueB"));
            testingkvp.Add(new KeyValuePair<string, object>("colC", "valueC"));

            Query query = DbTools.FormatUpdate("testtable", "update {0} set {1} where {2} > {3}",
                   testingkvp, new List<string> { "testCol", "testCol2" }, null);
            Assert.IsNotNull(query, "Expected no incoherence, query object not null");

            Assert.AreEqual("update testtable set colA = @p0, colB = @p1, colC = @p2 where testCol > testCol2", query.SqlString);
            Assert.AreEqual(3, query.SqlValues.Count, "Attendu 3 valeurs de paramètres");
            Assert.AreEqual("valueA", query.SqlValues[0]);
            Assert.AreEqual("valueB", query.SqlValues[1]);
            Assert.AreEqual("valueC", query.SqlValues[2]);
        }

        /// <summary>
        /// Tested template : "update {0} set {1} where {2} = {3}"
        /// 2 parameters values instead of 1 are passed to tested method.
        /// </summary>
        [TestMethod]
        [ExcludeFromCodeCoverage]
        [Owner("Barbara Post")]
        [Ignore] // à corriger #OSAMESORM-81
        public void TestDbToolsFormatUpdateSimpleWhereWithError()
        {

            List<KeyValuePair<string, object>> testingkvp = new List<KeyValuePair<string, object>>();

            testingkvp.Add(new KeyValuePair<string, object>("colA", "valueA"));
            testingkvp.Add(new KeyValuePair<string, object>("colB", "valueB"));
            testingkvp.Add(new KeyValuePair<string, object>("colC", "valueC"));
            // Pass 2 values instead of one
            Query query = DbTools.FormatUpdate("testtable", "update {0} set {1} where {2} = {3}",
                   testingkvp, new List<string> { "id_testtable", null }, new List<object> { 33, 34 });
            Assert.IsNull(query, "Expected incoherence, query object should be null");

            Assert.AreEqual("update testtable set colA = @p0, colB = @p1, colC = @p2 where id_testtable = @p3", query.SqlString);
            Assert.AreEqual(4, query.SqlValues.Count, "Attendu 4 valeurs de paramètres");
            Assert.AreEqual("valueA", query.SqlValues[0]);
            Assert.AreEqual("valueB", query.SqlValues[1]);
            Assert.AreEqual("valueC", query.SqlValues[2]);
            Assert.AreEqual(33, query.SqlValues[3]);
        }
  * */

        #endregion



    }
}
