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
        [TestCategory("SqLite")]
        public void TestGetProvider()
        {
            DataTable providers = DbProviderFactories.GetFactoryClasses();
            Assert.AreNotEqual(0, providers.Rows.Count, "Aucun provider trouvé");
            ShowTable(providers);

            DbProviderFactory provider = DbProviderFactories.GetFactory("System.Data.SQLite");
            Assert.IsNotNull(provider);
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
            List<DbManager.Parameter> parameters = new List<DbManager.Parameter>
                {
                    new DbManager.Parameter("@lastname", "Grey"),
                    new DbManager.Parameter("@firstname", "Paul"),
                    new DbManager.Parameter("@email", "Paul@Grey.com")
                };

            try
            {
                long lastInsertedRowId;
                int affectedRecordsCount = DbManager.Instance.ExecuteNonQuery("INSERT INTO Customer (LastName, FirstName, Email) VALUES (@lastname, @firstname, @email)", parameters.ToArray(), out lastInsertedRowId, CommandType.Text, _transaction);

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

        /// <summary>
        /// Test de comportement du provider factory SQLite en cas de demande et ouverture de plus de connexions que le pool peut en donner.
        /// Travail avec DbConnection de ADO.NET, pas celui de l'ORM.
        /// /!\ Il n'y a pas d'exception renvoyée.
        /// </summary>
        [TestMethod]
        [TestCategory("SqLite")]
        [TestCategory("ADO.NET pooling")]
        [ExpectedException(typeof(InvalidOperationException))]
        //[Ignore]
        public void TestExhaustingPoolSqlite()
        {
            List<System.Data.Common.DbConnection> lstConnections = new List<System.Data.Common.DbConnection>();
            try
            {
                // changement de la connexion string
                string cs = DbManager.ConnectionString;
                ConfigurePool(ref cs, 10);
                for (int i = 0; i < 20; i++)
                {
                    lstConnections.Add(DbManager.Instance.DbProviderFactory.CreateConnection());
                    lstConnections[i].ConnectionString = DbManager.ConnectionString;
                    lstConnections[i].Open();

                    using (System.Data.Common.DbCommand command = DbManager.Instance.DbProviderFactory.CreateCommand())
                    {
                        Assert.IsNotNull(command, "Commande non créée");
                        command.Connection = lstConnections[i];
                        command.CommandText = "select count(*) from Customer";
                        command.CommandType = CommandType.Text;
                        command.ExecuteScalar();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + " " + ex.StackTrace);
                throw;
            }
            finally
            {
                // cleanup
                foreach (System.Data.Common.DbConnection connection in lstConnections)
                {
                    connection.Close();
                    connection.Dispose();
                }
            }
        }

        /// <summary>
        /// Utilitaire de modification de valeurs dans une connection string.
        /// </summary>
        /// <param name="connectionString_"></param>
        /// <param name="maxPoolSize_"></param>
        private void ConfigurePool(ref string connectionString_, int maxPoolSize_)
        {
            DbConnectionStringBuilder tool = new DbConnectionStringBuilder { ConnectionString = connectionString_ };
            tool["Pooling"] = "True";
            tool["Max Pool Size"] = maxPoolSize_;
            
            connectionString_ = tool.ConnectionString;

        }
    }
}
