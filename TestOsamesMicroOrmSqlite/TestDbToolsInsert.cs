using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OsamesMicroOrm.Configuration;
using OsamesMicroOrm.DbTools;
using SampleDbEntities.Chinook;

namespace TestOsamesMicroOrmSqlite
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class TestDbToolsInsert : OsamesMicroOrmSqliteTest
    {
        [TestMethod]
        [TestCategory("SqLite")]
        [TestCategory("Insert")]
        [Owner("Benjamin Nolmans")]
        public void TestInsertWithTransaction()
        {
            _config = ConfigurationLoader.Instance;

            string error;
            
            Customer testCustomer = new Customer();

            testCustomer.Address = "rue du Rond-Point 218";
            testCustomer.City = "Gilly";
            testCustomer.FirstName = "Benjamin";
            testCustomer.LastName = "Nolmans";
            testCustomer.Email = "pas@mail";

            long newRecordId = DbToolsInserts.Insert<Customer>(testCustomer, "BaseInsert", "Customer", new List<string> { "Address", "City", "FirstName", "LastName", "Email" }, out error, _transaction);

            Console.WriteLine(newRecordId);

        }

        [TestMethod]
        [TestCategory("SqLite")]
        [TestCategory("Insert")]
        [TestCategory("No Transaction")]
        [Ignore]
        [Owner("Benjamin Nolmans")]
        public void TestInsertWithoutTransaction()
        {
            _config = ConfigurationLoader.Instance;

            string error;

            Customer testCustomer = new Customer();

            testCustomer.Address = "rue du Rond-Point 218";
            testCustomer.City = "Gilly";
            testCustomer.FirstName = "Benjamin";
            testCustomer.LastName = "Nolmans";
            testCustomer.Email = "pas@mail";

            long newRecordId = DbToolsInserts.Insert<Customer>(testCustomer, "BaseInsert", "Customer", new List<string> { "Address", "City", "FirstName", "LastName", "Email" }, out error);

            Console.WriteLine(newRecordId);

        }
    }
}
