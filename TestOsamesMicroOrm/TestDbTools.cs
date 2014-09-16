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

            //InitializeDbConnexion();

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

        #region DbToolsCommon - test sql formatting

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
            // DetermineDatabaseColumnNamesAndAdoParameters<T>(ref T dataObject_, string mappingDictionariesContainerKey_, string dataObjectPropertyName_, out string dbColumnName_, out KeyValuePair<string, object> adoParameterNameAndValue_)

            string dbColumnName;
            KeyValuePair<string, object> adoParams;
            DbToolsCommon.DetermineDatabaseColumnNameAndAdoParameter(ref Employee, "Employee", "LastName", out dbColumnName, out adoParams);

            // Ici on devra faire un Assert.IsTrue du retour de la méthode (méthode à modifier pour retourner true/false) - ORM #85

            Assert.AreEqual("LastName", dbColumnName);

            Assert.AreEqual("@lastname", adoParams.Key);
            Assert.AreEqual(Employee.LastName, adoParams.Value);
        }

        /// <summary>
        /// Test of DetermineDatabaseColumnNamesAndAdoParameters<T> with a single property.
        /// </summary>
        [TestMethod]
        [TestCategory("Mapping GetPropertyValue")]
        [TestCategory("Parameter NOK")]
        public void TestDetermineDatabaseColumnNameAndAdoParameterWrongDictionaryName()
        {
            // DetermineDatabaseColumnNamesAndAdoParameters<T>(ref T dataObject_, string mappingDictionariesContainerKey_, string dataObjectPropertyName_, out string dbColumnName_, out KeyValuePair<string, object> adoParameterNameAndValue_)

            string dbColumnName;
            KeyValuePair<string, object> adoParams;
            DbToolsCommon.DetermineDatabaseColumnNameAndAdoParameter(ref Employee, "NotAnEmployee", "LastName", out dbColumnName, out adoParams);

            // Ici on devra faire un Assert.IsFalse du retour de la méthode (méthode à modifier pour retourner true/false) - ORM #85

        }

        /// <summary>
        /// Test of DetermineDatabaseColumnNamesAndAdoParameters<T> with 2 properties.
        /// </summary>
        [TestMethod]
        [TestCategory("Mapping GetPropertyValue")]
        public void TestDetermineDatabaseColumnNamesAndAdoParameters()
        {
            // DetermineDatabaseColumnNamesAndAdoParameters<T>(ref T dataObject_, string mappingDictionariesContainerKey_, List<string> lstDataObjectPropertyName_, out List<string> lstDbColumnName_, out List<KeyValuePair<string, object>> adoParameterNameAndValue_ )

            List<string> lstDbColumnNames;
            List<KeyValuePair<string, object>> adoParams;
            DbToolsCommon.DetermineDatabaseColumnNamesAndAdoParameters(ref Employee, "Employee", new List<string> { "LastName", "FirstName" }, out lstDbColumnNames, out adoParams);

            // Ici on devra faire un Assert.IsTrue du retour de la méthode (méthode à modifier pour retourner true/false) - ORM #85

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
        public void TestDetermineDatabaseColumnNamesAndAdoParametersWrongDictionaryName()
        {
            // DetermineDatabaseColumnNamesAndAdoParameters<T>(ref T dataObject_, string mappingDictionariesContainerKey_, List<string> lstDataObjectPropertyName_, out List<string> lstDbColumnName_, out List<KeyValuePair<string, object>> adoParameterNameAndValue_ )

            List<string> lstDbColumnNames;
            List<KeyValuePair<string, object>> adoParams;
            DbToolsCommon.DetermineDatabaseColumnNamesAndAdoParameters(ref Employee, "NotAnEmployee", new List<string> { "LastName", "FirstName" }, out lstDbColumnNames, out adoParams);

            // Ici on devra faire un Assert.IsFalse du retour de la méthode (méthode à modifier pour retourner true/false) - ORM #85

        }

        /// <summary>
        /// Test of FormatSqlForUpdate<T> with a list of 2 properties.
        /// </summary>
        [TestMethod]
        [TestCategory("Mapping")]
        [TestCategory("Sql formatting for Update")]
        public void TestFormatSqlForUpdate()
        {
            try
            {

                // Utiliser la DB Sqlite
                Customizer.ConfigurationManagerSetKeyValue(Customizer.AppSettingsKeys.activeDbConnection.ToString(), "OsamesMicroORM.Sqlite");
                Customizer.ConfigurationManagerSetKeyValue(Customizer.AppSettingsKeys.dbName.ToString(), "Chinook_Sqlite.sqlite");

                // FormatSqlForUpdate<T>(ref T dataObject_, string mappingDictionariesContainerKey_, List<string> lstDataObjectPropertyName_, string primaryKeyPropertyName_, 
                //                        out string sqlCommand_, out List<KeyValuePair<string, object>> adoParameters_)

                string sqlCommand, strErrorMsg_;
                List<KeyValuePair<string, object>> adoParams;
                DbToolsUpdates.FormatSqlForUpdate(ref Employee, "BaseUpdateOne", "Employee", new List<string> { "LastName", "FirstName" }, new List<string> { "EmployeeId", null }, new List<object> { 2 }, out sqlCommand, out adoParams, out strErrorMsg_);

                Assert.AreEqual("UPDATE [Employee] SET [LastName] = @lastname, [FirstName] = @firstname WHERE [EmployeeId] = @p0;", sqlCommand);
                Assert.AreEqual(3, adoParams.Count, "no parameters generated");
                Assert.AreEqual("@lastname", adoParams[0].Key);
                Assert.AreEqual(Employee.LastName, adoParams[0].Value);
                Assert.AreEqual("@firstname", adoParams[1].Key);
                Assert.AreEqual(Employee.FirstName, adoParams[1].Value);
                Assert.AreEqual("@p0", adoParams[2].Key);
                Assert.AreEqual(2, adoParams[2].Value);
            }
            finally
            {
                Customizer.ConfigurationManagerRestoreAllKeys();
            }
        }

        /// <summary>
        /// Nom de template incorrect !
        /// Test of FormatSqlForUpdate<T> with a list of 2 properties.
        /// </summary>
        [TestMethod]
        [TestCategory("Mapping")]
        [TestCategory("Sql formatting for Update")]
        [TestCategory("Parameter NOK")]
        [ExpectedException(typeof(KeyNotFoundException))]
        public void TestFormatSqlForUpdateIncorrectTemplateName()
        {
            try
            {

                // Utiliser la DB Sqlite
                Customizer.ConfigurationManagerSetKeyValue(Customizer.AppSettingsKeys.activeDbConnection.ToString(), "OsamesMicroORM.Sqlite");
                Customizer.ConfigurationManagerSetKeyValue(Customizer.AppSettingsKeys.dbName.ToString(), "Chinook_Sqlite.sqlite");

                // FormatSqlForUpdate<T>(ref T dataObject_, string mappingDictionariesContainerKey_, List<string> lstDataObjectPropertyName_, string primaryKeyPropertyName_, 
                //                        out string sqlCommand_, out List<KeyValuePair<string, object>> adoParameters_)

                string sqlCommand, strErrorMsg_;
                List<KeyValuePair<string, object>> adoParams;
                DbToolsUpdates.FormatSqlForUpdate(ref Employee, "ThisTemplateDoesntExist", "Employee", new List<string> { "LastName", "FirstName" }, new List<string> { "EmployeeId", null }, new List<object> { 2 }, out sqlCommand, out adoParams, out strErrorMsg_);

                Assert.IsNull(sqlCommand);

            }
            finally
            {
                Customizer.ConfigurationManagerRestoreAllKeys();
            }
        }



        #endregion

        #region DbToolsSelect - test sql formatting

        /// <summary>
        /// Select avec clause where retournant un enregistrement.
        /// Ici le paramètre dynamique est représenté par "null".
        /// </summary>
        [TestMethod]
        [TestCategory("Mapping")]
        [TestCategory("Sql formatting for Select")]
        public void TestFormatSqlForSelect()
        {
            string sqlCommand, strErrorMsg;
            List<KeyValuePair<string, object>> adoParams;
            List<string> lstDbColumnNames;
            DbToolsSelects.FormatSqlForSelect("BaseReadWhere", "Employee", new List<string> { "LastName", "FirstName", "Address" }, new List<string> { "EmployeeId", null }, new List<object> { 5 }, out sqlCommand, out adoParams, out lstDbColumnNames, out strErrorMsg);

            Assert.AreEqual("SELECT [LastName], [FirstName], [Address] FROM [Employee] WHERE [EmployeeId] = @p0;", sqlCommand);
            Assert.AreEqual(1, adoParams.Count);
            Assert.AreEqual("@p0", adoParams[0].Key);
            Assert.AreEqual(5, adoParams[0].Value);
            Assert.AreEqual(3, lstDbColumnNames.Count);

        }

        /// <summary>
        /// Select avec clause where retournant un enregistrement. Auto-détermination des champs à sélectionner d'après le mapping utilisé.
        /// Ici le paramètre dynamique est représenté par "null".
        /// </summary>
        [TestMethod]
        [TestCategory("Mapping")]
        [TestCategory("Sql formatting for Select")]
        public void TestFormatSqlForSelectAutoDetermineSelectedFields()
        {
            string sqlCommand, strErrorMsg;
            List<KeyValuePair<string, object>> adoParams;
            // cette liste va être créée par la méthode testée
            List<string> lstDbColumnNames;
            DbToolsSelects.FormatSqlForSelectAutoDetermineSelectedFields("BaseReadAllWhere", "Employee", new List<string> { "EmployeeId", null }, new List<object> { 5 }, out lstDbColumnNames, out sqlCommand, out adoParams, out strErrorMsg);

            Assert.AreEqual("SELECT * FROM [Employee] WHERE [EmployeeId] = @p0;", sqlCommand);
            Assert.AreEqual(1, adoParams.Count);
            Assert.AreEqual("@p0", adoParams[0].Key);
            Assert.AreEqual(5, adoParams[0].Value);
            // TODO FIXME les noms des colonnes ne sont pas générés en sortie
            Assert.AreEqual(3, lstDbColumnNames.Count);

        }

        /// <summary>
        /// Mauvais template utilisé !
        /// Select avec clause where retournant un enregistrement.
        /// Ici le paramètre dynamique est représenté par "null".
        /// </summary>
        [TestMethod]
        [TestCategory("Mapping")]
        [TestCategory("Sql formatting for Select")]
        [TestCategory("Parameter NOK")]
        [ExpectedException(typeof(KeyNotFoundException))]
        public void TestFormatSqlForSelectIncorrectTemplateName()
        {
            string sqlCommand, strErrorMsg;
            List<KeyValuePair<string, object>> adoParams;
            List<string> lstDbColumnNames;
            DbToolsSelects.FormatSqlForSelect("ThisTemplateDoesntExist", "Employee", new List<string> { "LastName", "FirstName", "Address" }, new List<string> { "EmployeeId", null }, new List<object> { 5 }, out sqlCommand, out adoParams, out lstDbColumnNames, out strErrorMsg);

            Assert.IsNull(sqlCommand);

        }

        /// <summary>
        /// Mauvais template utilisé !
        /// Select avec clause where retournant un enregistrement. Auto-détermination des champs à sélectionner d'après le mapping utilisé.
        /// Ici le paramètre dynamique est représenté par "null".
        /// </summary>
        [TestMethod]
        [TestCategory("Mapping")]
        [TestCategory("Sql formatting for Select")]
        [TestCategory("Parameter NOK")]
        [ExpectedException(typeof(KeyNotFoundException))]
        public void TestFormatSqlForSelectAutoDetermineSelectedFieldsIncorrectTemplateName()
        {
            string sqlCommand, strErrorMsg;
            List<KeyValuePair<string, object>> adoParams;
            // cette liste va être créée par la méthode testée
            List<string> lstDbColumnNames;
            DbToolsSelects.FormatSqlForSelectAutoDetermineSelectedFields("ThisTemplateDoesntExist", "Employee", new List<string> { "EmployeeId", null }, new List<object> { 5 }, out lstDbColumnNames, out sqlCommand, out adoParams, out strErrorMsg);

            Assert.IsNull(sqlCommand);

        }

        /// <summary>
        /// Select avec clause where retournant un enregistrement.
        /// Ici le paramètre dynamique est représenté par "@employeeId".
        /// </summary>
        [TestMethod]
        [TestCategory("Mapping")]
        [TestCategory("Sql formatting for Select")]
        public void TestFormatSqlForSelectNamedDynamicParameter()
        {
            string sqlCommand, strErrorMsg;
            List<KeyValuePair<string, object>> adoParams;
            List<string> lstDbColumnNames;
            DbToolsSelects.FormatSqlForSelect("BaseReadWhere", "Employee", new List<string> { "LastName", "FirstName", "Address" }, new List<string> { "EmployeeId", "@employeeId" }, new List<object> { 5 }, out sqlCommand, out adoParams, out lstDbColumnNames, out strErrorMsg);

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
        [TestCategory("Sql formatting for Select")]
        public void TestFormatSqlForSelectMultipleRecordsWithoutWhere()
        {
            string sqlCommand, strErrorMsg;
            List<KeyValuePair<string, object>> adoParams;
            List<string> lstDbColumnNames;
            DbToolsSelects.FormatSqlForSelect("BaseRead", "Employee", new List<string> { "LastName", "FirstName", "Address" }, null, null, out sqlCommand, out adoParams, out lstDbColumnNames, out strErrorMsg);

            Assert.AreEqual("SELECT [LastName], [FirstName], [Address] FROM [Employee];", sqlCommand);
            Assert.AreEqual(0, adoParams.Count);
            Assert.AreEqual(3, lstDbColumnNames.Count);

        }

        #endregion

        #region DbToolsCommon - test Determine

        /// <summary>
        /// Test of DetermineDatabaseColumnName for a given mapping.
        /// </summary>
        [TestMethod]
        [TestCategory("Mapping")]
        public void TestDetermineDatabaseColumnName()
        {
            string columnName, strErrorMsg_;
            DbToolsCommon.DetermineDatabaseColumnName("Customer", "LastName", out columnName, out strErrorMsg_);
            // TODO cette méthode est à modifier pour renvoyer ici true (ajouter un assert) #ORM-85
            Assert.AreEqual("LastName", columnName);
        }

        /// <summary>
        /// 
        /// </summary>
        [TestMethod]
        [TestCategory("Mapping")]
        public void TestDetermineDatabaseColumnNames()
        {
            string strErrorMsg_ = null;
            List<string> columnsNames;
            bool result = DbToolsCommon.DetermineDatabaseColumnNames("NotCustomer", new List<string> { "FirstName", "LastName" }, out columnsNames, out strErrorMsg_);

            Assert.IsFalse(result);

            columnsNames.Clear();

            result = DbToolsCommon.DetermineDatabaseColumnNames("Customer", new List<string> { "FirstName", "LastName" }, out columnsNames, out strErrorMsg_);

            Assert.IsTrue(result);
        }

        /// <summary>
        /// Test of DetermineDatabaseColumnName for a given mapping.
        /// On demande le mauvais nom de dictionnaire.
        /// </summary>
        [TestMethod]
        [TestCategory("Mapping")]
        [TestCategory("Parameter NOK")]
        public void TestDetermineDatabaseColumnNameWrongDictionaryName()
        {
            string columnName, strErrorMsg_;
            bool result = DbToolsCommon.DetermineDatabaseColumnName("NotACustomer", "LastName", out columnName, out strErrorMsg_);

            Assert.IsFalse(result);
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

            // TODO cette méthode est à modifier pour renvoyer ici true (ajouter un assert) #ORM-85

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
        public void TestDetermineDatabaseColumnNamesAndDataObjectPropertyNamesWrongDictionaryName()
        {
            List<string> lstPropertiesNames;
            List<string> lstDbColumnNames;
            DbToolsCommon.DetermineDatabaseColumnNamesAndDataObjectPropertyNames("NotACustomer", out lstDbColumnNames, out lstPropertiesNames);
            // TODO cette méthode est à modifier pour renvoyer ici false (ajouter un assert) #ORM-85
        }

        #endregion



    }
}
