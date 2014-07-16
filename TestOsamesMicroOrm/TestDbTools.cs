using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OsamesMicroOrm;
using OsamesMicroOrm.Configuration;
using TestOsamesMicroOrm.TestDbEntities;

namespace TestOsamesMicroOrm
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class TestDbTools : OsamesMicroOrmTest
    {
        static TestClient _client = new TestClient();

        /// <summary>
        /// Initialization once for all tests.
        /// </summary>
        [ClassInitialize]
        public static void Setup(TestContext context_)
        {

            var init = ConfigurationLoader.Instance;
            
            _client.Adresses = new ObservableCollection<TestAdresse>(new List<TestAdresse>());

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


        }

        #region test sql formatting

        /// <summary>
        /// Test of FormatSqlNameEqualValue with a single value.
        /// </summary>
        [TestMethod]
        [TestCategory("Mapping")]
        [TestCategory("GetPropertyValue")]
        public void TestFormatSqlNameEqualValue()
        {
            KeyValuePair<string, object> adoParams = new KeyValuePair<string, object>("@nomsociete", "Société X");
            StringBuilder sb = new StringBuilder();
            DbTools.FormatSqlNameEqualValue("nom_societe", adoParams, ref sb);

            Assert.IsFalse(string.IsNullOrWhiteSpace(sb.ToString()), "string builder empty");
            Assert.AreEqual("nom_societe = @nomsociete", sb.ToString());
        }

        /// <summary>
        /// Test of FormatSqlNameEqualValue with 2 values.
        /// </summary>
        [TestMethod]
        [TestCategory("Mapping")]
        [TestCategory("GetPropertyValue")]
        public void TestFormatSqlNameEqualValueMulti()
        {
            List<KeyValuePair<string, object>> adoParams = new List<KeyValuePair<string, object>>
                {
                    new KeyValuePair<string, object>("@nomsociete", "Société X"),
                    new KeyValuePair<string, object>("@clientdate", DateTime.Today)
                };
            StringBuilder sb = new StringBuilder();
            DbTools.FormatSqlNameEqualValue(new List<string> { "nom_societe", "client_date" }, adoParams, ref sb, ", ");

            Assert.IsFalse(string.IsNullOrWhiteSpace(sb.ToString()), "string builder empty");
            Assert.AreEqual("nom_societe = @nomsociete, client_date = @clientdate", sb.ToString());
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
            DbTools.DetermineDatabaseColumnNameAndAdoParameter(ref _client, "clients", "NomSociete", out dbColumnName, out adoParams);
            
            Assert.AreEqual("nom_societe", dbColumnName);

            Assert.AreEqual("@nomsociete", adoParams.Key);
            Assert.AreEqual(_client.NomSociete, adoParams.Value);
        }

        /// <summary>
        /// Test of DetermineDatabaseColumnNamesAndAdoParameters<T> with a 2 properties.
        /// </summary>
        [TestMethod]
        [TestCategory("Mapping")]
        [TestCategory("GetPropertyValue")]
        [TestCategory("FIXME")]
        public void TestExtractFromPropertyMulti()
        {
            // DetermineDatabaseColumnNamesAndAdoParameters<T>(ref T dataObject_, string mappingDictionariesContainerKey_, List<string> lstDataObjectPropertyName_, out List<string> lstDbColumnName_, out List<KeyValuePair<string, object>> adoParameterNameAndValue_ )

            List<string> lstDbColumnNames;
            List<KeyValuePair<string, object>> adoParams;
            DbTools.DetermineDatabaseColumnNamesAndAdoParameters<TestClient>(ref _client, "clients", new List<string>{ "NomSociete", "ClientDate"}, out lstDbColumnNames, out adoParams);

            Assert.AreEqual("nom_societe", lstDbColumnNames[0]);
            Assert.AreEqual("client_date", lstDbColumnNames[1]);
            Assert.AreEqual(2, adoParams.Count, "no parameters generated");
            Assert.AreEqual("@nomsociete", adoParams[0].Key);
            Assert.AreEqual(_client.NomSociete, adoParams[0].Value);
            Assert.AreEqual("@clientdate", adoParams[1].Key);
            Assert.AreEqual(_client.ClientDate, adoParams[1].Value);
        }

        /// <summary>
        /// Test of FormatSqlForUpdate<T> with a list of 2 properties.
        /// </summary>
        [TestMethod]
        [TestCategory("Mapping")]
        [TestCategory("Sql formatting")]
        [Ignore]
        [TestCategory("FIXME")]
        public void TestFormatSqlForUpdate()
        {
            // FormatSqlForUpdate<T>(ref T dataObject_, string mappingDictionariesContainerKey_, List<string> lstDataObjectPropertyName_, string primaryKeyPropertyName_, 
            //                        out string sqlCommand_, out List<KeyValuePair<string, object>> adoParameters_)

            string sqlCommand;
            List<KeyValuePair<string, object>> adoParams;
            DbTools.FormatSqlForUpdate(ref _client, "clients", new List<string> {"NomSociete", "ClientDate"}, "IdClient", out sqlCommand, out adoParams);

            Assert.AreEqual("UPDATE clients SET nom_societe = @nomsociete, client_date = @clientdate WHERE id_client = @idclient;", sqlCommand);
            Assert.AreEqual(2, adoParams.Count, "no parameters generated");
            Assert.AreEqual("@nomsociete", adoParams[0].Key);
            Assert.AreEqual(_client.NomSociete, adoParams[0].Value);
            Assert.AreEqual("@clientdate", adoParams[1].Key);
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
            DbTools.FormatSqlForSelect("BaseReadWhere", new List<string> { "NomSociete", "Telephone", "IdConditionReglementRef" }, "clients", new List<string> {"NumeroClient", null}, new List<object>{1235}, out sqlCommand, out adoParams, out lstDbColumnNames);

            Assert.AreEqual("SELECT nom_societe, telephone, if_condition_reglement_ref FROM clients WHERE numero_client = @p0;", sqlCommand);
            Assert.AreEqual(1, adoParams.Count);
            Assert.AreEqual("@p0", adoParams[0].Key);
            Assert.AreEqual(1235, adoParams[0].Value);
            Assert.AreEqual(3, lstDbColumnNames.Count);

        }

        /// <summary>
        /// Select avec clause where retournant un enregistrement.
        /// Ici le paramètre dynamique est représenté par "@IdClient".
        /// </summary>
        [TestMethod]
        [TestCategory("Mapping")]
        [TestCategory("Sql formatting")]
        public void TestFormatSqlForSelect2()
        {
            string sqlCommand;
            List<KeyValuePair<string, object>> adoParams;
            List<string> lstDbColumnNames;
            DbTools.FormatSqlForSelect("BaseReadWhere", new List<string> { "NomSociete", "Telephone", "IdConditionReglementRef" }, "clients", new List<string> { "NumeroClient", "@IdClient" }, new List<object> { 1235 }, out sqlCommand, out adoParams, out lstDbColumnNames);

            Assert.AreEqual("SELECT nom_societe, telephone, if_condition_reglement_ref FROM clients WHERE numero_client = @idclient;", sqlCommand);
            Assert.AreEqual(1, adoParams.Count);
            Assert.AreEqual("@idclient", adoParams[0].Key);
            Assert.AreEqual(1235, adoParams[0].Value);
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
            DbTools.FormatSqlForSelect("BaseRead", new List<string> { "NomSociete", "Telephone", "IdConditionReglementRef" }, "clients", null, null, out sqlCommand, out adoParams, out lstDbColumnNames);

            Assert.AreEqual("SELECT nom_societe, telephone, if_condition_reglement_ref FROM clients;", sqlCommand);
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
            DbTools.DetermineDatabaseColumnsAndPropertiesNames("clients", out lstDbColumnNames, out lstPropertiesNames);
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
