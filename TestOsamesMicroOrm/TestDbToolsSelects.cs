﻿using System.Collections.Generic;
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
    public class TestDbToolsSelects : OsamesMicroOrmTest
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
        /// Select avec clause where retournant un enregistrement.
        /// Ici le paramètre dynamique est représenté par "#".
        /// </summary>
        [TestMethod]
        [TestCategory("Mapping")]
        [TestCategory("Sql formatting for Select")]
        public void TestFormatSqlForSelect()
        {
            List<string> lstDbColumnNames;

            InternalPreparedStatement statement = DbToolsSelects.FormatSqlForSelect("BaseReadWhere", "Employee", new List<string> { "LastName", "FirstName", "Address" }, new List<string> { "EmployeeId", "#" }, new List<object> { 5 }, out lstDbColumnNames);

            Assert.AreEqual("SELECT [LastName], [FirstName], [Address] FROM [Employee] WHERE [EmployeeId] = @p0;", statement.PreparedStatement.PreparedSqlCommand);
            Assert.AreEqual(1, statement.AdoParameters.Count);
            Assert.AreEqual("@p0", statement.AdoParameters[0].Key);
            Assert.AreEqual(5, statement.AdoParameters[0].Value);
            Assert.AreEqual(3, lstDbColumnNames.Count);

        }

        /// <summary>
        /// Select avec clause where retournant un enregistrement. Auto-détermination des champs à sélectionner d'après le mapping utilisé.
        /// Ici le paramètre dynamique est représenté par "#".
        /// </summary>
        [TestMethod]
        [TestCategory("Mapping")]
        [TestCategory("Sql formatting for Select")]
        public void TestFormatSqlForSelectAutoDetermineSelectedFields()
        {
            List<string> lstDbEntityPropertyNames;
            List<string> lstDbColumnNames;
            InternalPreparedStatement statement = DbToolsSelects.FormatSqlForSelectAutoDetermineSelectedFields("BaseReadAllWhere", "Employee", new List<string> { "EmployeeId", "#" }, new List<object> { 5 }, out lstDbEntityPropertyNames, out lstDbColumnNames);

            Assert.AreEqual("SELECT * FROM [Employee] WHERE [EmployeeId] = @p0;", statement.PreparedStatement.PreparedSqlCommand);
            Assert.AreEqual(1, statement.AdoParameters.Count);
            Assert.AreEqual("@p0", statement.AdoParameters[0].Key);
            Assert.AreEqual(5, statement.AdoParameters[0].Value);
            Assert.AreEqual(15, lstDbColumnNames.Count, "Epected number of public properties of Employee C# class");

        }

        /// <summary>
        /// Mauvais template utilisé !
        /// Select avec clause where retournant un enregistrement.
        /// Ici le paramètre dynamique est représenté par "#".
        /// </summary>
        [TestMethod]
        [TestCategory("Mapping")]
        [TestCategory("Sql formatting for Select")]
        [TestCategory("Parameter NOK")]
        [ExpectedException(typeof(OOrmHandledException))]
        public void TestFormatSqlForSelectIncorrectTemplateName()
        {
            try
            {
                List<string> lstDbColumnNames;

                InternalPreparedStatement statement = DbToolsSelects.FormatSqlForSelect("ThisTemplateDoesntExist", "Employee", new List<string> { "LastName", "FirstName", "Address" }, new List<string> { "EmployeeId", "#" }, new List<object> { 5 }, 
                    out lstDbColumnNames);
            }
            catch (OOrmHandledException ex)
            {
                Common.AssertOnHresultAndWriteToConsole(HResultEnum.E_NOTEMPLATE, ex);
                throw;
            }

        }

        /// <summary>
        /// Mauvais template utilisé !
        /// Select avec clause where retournant un enregistrement. Auto-détermination des champs à sélectionner d'après le mapping utilisé.
        /// Ici le paramètre dynamique est représenté par "#".
        /// </summary>
        [TestMethod]
        [TestCategory("Mapping")]
        [TestCategory("Sql formatting for Select")]
        [TestCategory("Parameter NOK")]
        [ExpectedException(typeof(OOrmHandledException))]
        public void TestFormatSqlForSelectAutoDetermineSelectedFieldsIncorrectTemplateName()
        {
            try
            {
                List<string> lstDbColumnNames;
                List<string> lstDbEntityPropertyNames;
                InternalPreparedStatement statement = DbToolsSelects.FormatSqlForSelectAutoDetermineSelectedFields("ThisTemplateDoesntExist", "Employee", new List<string> { "EmployeeId", "#" }, new List<object> { 5 }, out lstDbEntityPropertyNames,
                    out lstDbColumnNames);

            }
            catch (OOrmHandledException ex)
            {
                Common.AssertOnHresultAndWriteToConsole(HResultEnum.E_NOTEMPLATE, ex);
                throw;
            }

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
            List<string> lstDbColumnNames;
            InternalPreparedStatement statement = DbToolsSelects.FormatSqlForSelect("BaseReadWhere", "Employee", new List<string> { "LastName", "FirstName", "Address" }, new List<string> { "EmployeeId", "@employeeId" }, new List<object> { 5 }, out lstDbColumnNames);

            Assert.AreEqual("SELECT [LastName], [FirstName], [Address] FROM [Employee] WHERE [EmployeeId] = @employeeid;", statement.PreparedStatement.PreparedSqlCommand);
            Assert.AreEqual(1, statement.AdoParameters.Count);
            Assert.AreEqual("@employeeid", statement.AdoParameters[0].Key);
            Assert.AreEqual(5, statement.AdoParameters[0].Value);
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
            List<string> lstDbColumnNames;
            InternalPreparedStatement statement = DbToolsSelects.FormatSqlForSelect("BaseRead", "Employee", new List<string> { "LastName", "FirstName", "Address" }, null, null, out lstDbColumnNames);

            Assert.AreEqual("SELECT [LastName], [FirstName], [Address] FROM [Employee];", statement.PreparedStatement.PreparedSqlCommand);
            Assert.AreEqual(0, statement.AdoParameters.Count);
            Assert.AreEqual(3, lstDbColumnNames.Count);

        }

        #endregion
    }
}
