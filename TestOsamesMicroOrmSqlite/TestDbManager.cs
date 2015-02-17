using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OsamesMicroOrm;
using TestOsamesMicroOrm.Tools;

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
        [TestCategory("SqLite")]
        [TestCategory("List Provider")]
        public void TestGetProvider()
        {
            DataTable providers = DbProviderFactories.GetFactoryClasses();
            Assert.AreNotEqual(0, providers.Rows.Count, "Aucun provider trouvé");
            ShowTable(providers);

            DbProviderFactory provider = DbProviderFactories.GetFactory("System.Data.SQLite");
            Assert.IsNotNull(provider);
        }

        /// <summary>
        /// test d'un cas absurde d'ouverture de transation sur connexion nulle (méthode internal testée).
        /// </summary>
        [TestMethod]
        [TestCategory("Transaction")]
        [ExpectedException(typeof(OOrmHandledException))]
        [Owner("Barbara Post")]
        public void OpenTransactionFailed()
        {
            try
            {
                DbManager.Instance.OpenTransaction(null);
            }
            catch (OOrmHandledException ex)
            {
                Common.AssertOnHresultAndWriteToConsole(HResultEnum.E_BEGINTRANSACTIONFAILED, ex);
                throw;
            }
        }

        /// <summary>
        /// Test de bas niveau du Select.
        /// </summary>
        [TestMethod]
        [ExcludeFromCodeCoverage]
        [Owner("Barbara Post")]
        [TestCategory("SqLite")]
        [TestCategory("ADO.NET Select")]
        public void TestSelectUsingSqlite()
        {
            // select * from clients where id_client = @p0
            List<OOrmDbParameter> parameters = new List<OOrmDbParameter> { new OOrmDbParameter("@customerid", 3) };

            object idCustomer = null, lastName = null;

            using (IDataReader reader = DbManager.Instance.ExecuteReader(_transaction, "SELECT * FROM Customer WHERE CustomerId = @customerid", parameters.ToArray()))
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

        /// <summary>
        /// Test de bas niveau du Insert.
        /// </summary>
        [TestMethod]
        [ExcludeFromCodeCoverage]
        [Owner("Benjamin Nolmans")]
        [TestCategory("SqLite")]
        [TestCategory("ADO.NET Insert")]
        public void TestInsertUsingSqlite()
        {
            List<OOrmDbParameter> parameters = new List<OOrmDbParameter>
                {
                    new OOrmDbParameter("@lastname", "Grey"),
                    new OOrmDbParameter("@firstname", "Paul"),
                    new OOrmDbParameter("@email", "Paul@Grey.com")
                };

            try
            {
                long lastInsertedRowId;
                int affectedRecordsCount = DbManager.Instance.ExecuteNonQuery(_transaction, CommandType.Text, "INSERT INTO Customer (LastName, FirstName, Email) VALUES (@lastname, @firstname, @email)", parameters.ToArray(), out lastInsertedRowId);

                Assert.AreEqual(1, affectedRecordsCount, "Expected 1 record affected by INSERT operation");
                Console.WriteLine("New record ID: {0}, expected number > 1", lastInsertedRowId);
                Assert.AreNotEqual(0, lastInsertedRowId);
            }
            catch (OOrmHandledException ex)
            {
                // On ne devrait pas avoir d'exception, mais si on en a une, c'est celle-ci qu'on devrait avoir
                Common.AssertOnHresultAndWriteToConsole(HResultEnum.E_EXECUTENONQUERYFAILED, ex);
                throw;
            }
        }

        

        /// <summary>
        /// From http://msdn.microsoft.com/en-us/library/system.data.datatable(v=vs.110).aspx
        /// Méthode qui liste dans le output du TU (et non dans le output de VS) tout les providers disponibles.
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
