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
    public class TestDbToolsUpdates : OsamesMicroOrmTest
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

        /// <summary>
        /// Test of FormatSqlForUpdate<T> with a list of 2 properties. Test OK.
        /// </summary>
        [TestMethod]
        [TestCategory("Mapping")]
        [TestCategory("Sql formatting for Update")]
        public void TestFormatSqlForUpdate()
        {
            // FormatSqlForUpdate<T>(T databaseEntityObject_, string mappingDictionariesContainerKey_, List<string> lstDataObjectPropertyName_, string primaryKeyPropertyName_, 
            //                        out string sqlCommand_, out List<KeyValuePair<string, object>> adoParameters_)

            List<KeyValuePair<string, object>> adoParams;
            string sqlCommand;
            DbToolsUpdates.FormatSqlForUpdate(Employee, "BaseUpdateOne", "Employee", new List<string> { "LastName", "FirstName" }, new List<string> { "EmployeeId", "#" }, new List<object> { 2 }, out sqlCommand, out adoParams);

            Assert.IsNotNull(sqlCommand);

        }

        /// <summary>
        /// Nom de template incorrect !
        /// Test of FormatSqlForUpdate<T> with a list of 2 properties.
        /// </summary>
        [TestMethod]
        [TestCategory("Mapping")]
        [TestCategory("Sql formatting for Update")]
        [TestCategory("Parameter NOK")]
        [ExpectedException(typeof(OOrmHandledException))]
        public void TestFormatSqlForUpdateIncorrectTemplateName()
        {
            try
            {

                // FormatSqlForUpdate<T>(T databaseEntityObject_, string mappingDictionariesContainerKey_, List<string> lstDataObjectPropertyName_, string primaryKeyPropertyName_, 
                //                        out string sqlCommand_, out List<KeyValuePair<string, object>> adoParameters_)

                List<KeyValuePair<string, object>> adoParams;
                string sqlCommand;
                DbToolsUpdates.FormatSqlForUpdate(Employee, "ThisTemplateDoesntExist", "Employee", new List<string> { "LastName", "FirstName" }, new List<string> { "EmployeeId", "#" }, new List<object> { 2 }, out sqlCommand, out adoParams);

            }
            catch (OOrmHandledException ex)
            {
                Common.AssertOnHresultAndWriteToConsole(HResultEnum.E_NOTEMPLATE, ex);
                throw;
            }

        }

        /// <summary>
        /// Ici on a oublié de passer "#" pour le paramètre dynamique dans le 5e paramètre : liste des meta names.
        /// La chaîne SQL ne peut être générée car il n'y a pas assez de valeurs pour les placeholders.
        /// </summary>
        [TestMethod]
        [TestCategory("Mapping")]
        [TestCategory("Sql formatting for Update")]
        [TestCategory("Parameter NOK")]
        [ExpectedException(typeof(OOrmHandledException))]
        public void TestFormatSqlForUpdateIncorrectParameters()
        {
            try
            {

                // FormatSqlForUpdate<T>(T databaseEntityObject_, string mappingDictionariesContainerKey_, List<string> lstDataObjectPropertyName_, string primaryKeyPropertyName_, 
                //                        out string sqlCommand_, out List<KeyValuePair<string, object>> adoParameters_)

                List<KeyValuePair<string, object>> adoParams;
                string sqlCommand;
                DbToolsUpdates.FormatSqlForUpdate(Employee, "BaseUpdateOne", "Employee", new List<string> { "LastName", "FirstName" }, new List<string> { "EmployeeId" }, new List<object> { 2 }, out sqlCommand, out adoParams);

            }
            catch (OOrmHandledException ex)
            {
                Common.AssertOnHresultAndWriteToConsole(HResultEnum.E_PLACEHOLDERSVALUESCOUNTMISMATCH, ex);
                throw;
            }

        }

        /// <summary>
        /// Ici on a passé "Test" au lieu de "#" pour le paramètre dynamique dans le 5e paramètre : liste des meta names.
        /// Le donnée n'est pas trouvée dans le mapping.
        /// </summary>
        [TestMethod]
        [TestCategory("Mapping")]
        [TestCategory("Sql formatting for Update")]
        [TestCategory("Parameter NOK")]
        [ExpectedException(typeof(OOrmHandledException))]
        public void TestFormatSqlForUpdateIncorrectParameters2()
        {
            try
            {

                // FormatSqlForUpdate<T>(T databaseEntityObject_, string mappingDictionariesContainerKey_, List<string> lstDataObjectPropertyName_, string primaryKeyPropertyName_, 
                //                        out string sqlCommand_, out List<KeyValuePair<string, object>> adoParameters_)

                List<KeyValuePair<string, object>> adoParams;
                string sqlCommand;
                DbToolsUpdates.FormatSqlForUpdate(Employee, "BaseUpdateOne", "Employee", new List<string> { "LastName", "FirstName" }, new List<string> { "EmployeeId", "Test" }, new List<object> { 2 }, out sqlCommand, out adoParams);

            }
            catch (OOrmHandledException ex)
            {
                Common.AssertOnHresultAndWriteToConsole(HResultEnum.E_NOMAPPINGKEYANDPROPERTY, ex);
                throw;
            }

        }

        /// <summary>
        /// Test of FormatSqlForUpdate<T> with a list of 2 properties. Demande d'une propriété qui n'existe pas dans la classe Employee.
        /// </summary>
        [TestMethod]
        [TestCategory("Mapping")]
        [TestCategory("Sql formatting for Update")]
        [TestCategory("Parameter NOK")]
        [ExpectedException(typeof(OOrmHandledException))]
        public void TestFormatSqlForUpdateIncorrectPropertyForClass()
        {
            try
            {
                // FormatSqlForUpdate<T>(T databaseEntityObject_, string mappingDictionariesContainerKey_, List<string> lstDataObjectPropertyName_, string primaryKeyPropertyName_, 
                //                        out string sqlCommand_, out List<KeyValuePair<string, object>> adoParameters_)

                List<KeyValuePair<string, object>> adoParams;
                string sqlCommand;
                DbToolsUpdates.FormatSqlForUpdate(Employee, "BaseUpdateOne", "Employee", new List<string> { "LastName", "NotAFirstName" }, new List<string> { "EmployeeId", "#" }, new List<object> { 2 }, out sqlCommand, out adoParams);

            }
            catch (OOrmHandledException ex)
            {
                Common.AssertOnHresultAndWriteToConsole(HResultEnum.E_TYPEDOESNTDEFINEPROPERTY, ex);
                throw;
            }

        }

    }
}
