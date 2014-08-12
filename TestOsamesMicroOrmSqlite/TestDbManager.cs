using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OsamesMicroOrm;

namespace TestOsamesMicroOrmSqlite
{
    /// <summary>
    /// IMPORTANT : on doit utiliser la transaction définie par la classe mère des tests unitaires.
    /// Celle-ci porte la connexion à utiliser.
    /// Le test unitaire, en fin (test cleanup), fera un rollback sur la transaction.
    /// </summary>
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class SqliteTestDbManager : OsamesMicroOrmSqliteTest
    {
 
        [TestMethod]
        [ExcludeFromCodeCoverage]
        [Owner("Barbara Post")]
        [TestCategory("Sql")]
        [TestCategory("SqLite")]
        public void TestGetProvider()
        {
            DataTable providers = DbProviderFactories.GetFactoryClasses();
            ShowTable(providers);

            DbProviderFactory provider = DbProviderFactories.GetFactory("System.Data.SQLite");
            Assert.IsNotNull(provider);
        }

        [TestMethod]
        [ExcludeFromCodeCoverage]
        [Owner("Barbara Post")]
        [TestCategory("Sql")]
        [TestCategory("SqLite")]
        [TestCategory("Sql Select")]
        [TestCategory("Sql execution")]
        public void TestSelectUsingSqlite()
        {
            // select * from clients where id_client = @p0
            List<DbManager.Parameter> parameters = new List<DbManager.Parameter> { new DbManager.Parameter("@customerid", 3) };

            object idCustomer =  null, lastName = null;

            using (IDataReader reader = DbManager.Instance.ExecuteReader("SELECT * FROM Customer WHERE CustomerId = @customerid", parameters.ToArray()))
            {
                if (reader.Read())
                {
                    idCustomer = reader["CustomerId"];
                    Console.WriteLine("Customer ID: {0}", idCustomer);
                    lastName = reader["LastName"];
                    Console.WriteLine("Last name: {0}", lastName);
                }
            }
            Assert.IsNotNull(idCustomer);
            Assert.AreEqual(3, int.Parse(idCustomer.ToString()));
            Assert.IsNotNull(lastName);
            Assert.AreEqual("Tremblay", lastName.ToString());
        }

        [TestMethod]
        [ExcludeFromCodeCoverage]
        [Owner("Benjamin Nolmans")]
        [TestCategory("Sql")]
        [TestCategory("SqLite")]
        [TestCategory("Sql Insert")]
        [TestCategory("Sql execution")]
        public void TestInsertUsingSqlite()
        {
            List<DbManager.Parameter> parameters = new List<DbManager.Parameter> { new DbManager.Parameter("@lastname", "test customer") };

            try
            {
                long lastInsertedRowId;
                int affectedRecordsCount = DbManager.Instance.ExecuteNonQuery("INSERT INTO Customer (LastName) VALUES (@lastname)", parameters.ToArray(), out lastInsertedRowId, CommandType.Text, _transaction);

                Assert.AreEqual(1, affectedRecordsCount, "Expected 1 record affected by INSERT operation");
                Console.WriteLine("New record ID: {0}, expected number > 1", lastInsertedRowId);
                Assert.AreNotEqual(0, lastInsertedRowId);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                // relancer l'exception telle que catchée comme ça le test est correctement en erreur
                throw;
            }
        }
      
        /// <summary>
         /// From http://msdn.microsoft.com/en-us/library/system.data.datatable(v=vs.110).aspx
        /// </summary>
        /// <param name="table_"></param>
         private static void ShowTable(DataTable table_)
         {
             foreach (DataColumn col in table_.Columns)
             {
                 Console.Write("{0,-14}", col.ColumnName);
             }
             Console.WriteLine();

             foreach (DataRow row in table_.Rows)
             {
                 foreach (DataColumn col in table_.Columns)
                 {
                     if (col.DataType == typeof(DateTime))
                         Console.Write("{0,-14:d}", row[col]);
                     else if (col.DataType == typeof(Decimal))
                         Console.Write("{0,-14:C}", row[col]);
                     else
                         Console.Write("{0,-14}", row[col]);

                     Console.Write("   |   ");
                 }
                 Console.WriteLine();
             }
             Console.WriteLine();
         }
    }
}
