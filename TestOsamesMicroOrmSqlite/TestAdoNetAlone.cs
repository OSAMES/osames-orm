﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OsamesMicroOrm;
using TestOsamesMicroOrm;

namespace TestOsamesMicroOrmSqlite
{
    /// <summary>
    /// Test de ADO.NET sans instancier une transaction et une connexion de base pour les tests unitaires.
    /// </summary>

      [
        DeploymentItem("x64", "x64"),
        DeploymentItem("x86", "x86"),
        DeploymentItem("System.Data.SQLite.dll")
    ]

    [TestClass]
    [ExcludeFromCodeCoverage]
    public class TestAdoNetAlone : OsamesMicroOrmTest
    {
        /// <summary>
        /// Test de comportement du provider factory SQLite en cas de demande et ouverture de plus de connexions que le pool peut en donner.
        /// Travail avec DbConnection de ADO.NET, pas celui de l'ORM.
        /// /!\ Il n'y a pas d'exception renvoyée.
        /// </summary>
        [TestMethod]
        [TestCategory("SqLite")]
        [TestCategory("ADO.NET pooling")]
        [ExpectedException(typeof(InvalidOperationException))]
        [Ignore]
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
                        command.CommandText = "select count(*) from Customer;";
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

        /// <summary>
        /// Test de bas niveau du Insert.
        /// </summary>
        [TestMethod]
        [ExcludeFromCodeCoverage]
        [Owner("Benjamin Nolmans")]
        [TestCategory("SqLite")]
        [TestCategory("ADO.NET Insert")]
        [TestCategory("No Transaction")]
        public void TestInsertSqliteWithoutTransaction()
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
                using (OOrmDbConnectionWrapper conn1 = new OOrmDbConnectionWrapper(DbManager.Instance.DbProviderFactory.CreateConnection(), true))
                {
                    conn1.AdoDbConnection.ConnectionString = DbManager.ConnectionString;
                    conn1.AdoDbConnection.Open();
                    using (OOrmDbConnectionWrapper conn2 = new OOrmDbConnectionWrapper(DbManager.Instance.DbProviderFactory.CreateConnection(), false))
                    {
                        conn2.AdoDbConnection.ConnectionString = DbManager.ConnectionString;
                        conn2.AdoDbConnection.Open();
                        byte affectedRecordsCount = DbManager.Instance.ExecuteNonQuery(conn2, CommandType.Text, "INSERT INTO Customer (LastName, FirstName, Email) VALUES (@lastname, @firstname, @email);", parameters.ToArray(), out lastInsertedRowId);
                        Assert.AreEqual(1, affectedRecordsCount, "Expected 1 record affected by INSERT operation");
                        Console.WriteLine("New record ID: {0}, expected number > 1", lastInsertedRowId);
                        Assert.AreNotEqual(0, lastInsertedRowId);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                // relancer l'exception telle que catchée comme ça le test est correctement en erreur
                throw;
            }
        }

        /// <summary>
        /// Test de bas niveau du Insert n'utilisant que ADO.NET.
        /// </summary>
        [TestMethod]
        [ExcludeFromCodeCoverage]
        [Owner("Benjamin Nolmans")]
        [TestCategory("MsSql")]
        [TestCategory("ADO.NET Insert")]
        [TestCategory("No Transaction")]
        public void TestInsertSqLitePureAdoNetWithoutTransaction()
        {
            try
            {
                using (var conn1 = DbManager.Instance.DbProviderFactory.CreateConnection())
                {
                    conn1.ConnectionString = DbManager.ConnectionString;
                    conn1.Open();
                    using (DbCommand comm = DbManager.Instance.DbProviderFactory.CreateCommand())
                    {
                        comm.CommandText = "INSERT INTO Customer (LastName, FirstName, Email) VALUES (@lastname, @firstname, @email); select last_insert_rowid();";
                        comm.CommandType = CommandType.Text;
                        comm.Connection = conn1;

                        DbParameter param1 = comm.CreateParameter();
                        param1.Direction = ParameterDirection.Input;
                        param1.ParameterName = "@lastname";
                        param1.Value = "Grey";
                        comm.Parameters.Add(param1);

                        DbParameter param2 = comm.CreateParameter();
                        param2.Direction = ParameterDirection.Input;
                        param2.ParameterName = "@firstname";
                        param2.Value = "Paul";
                        comm.Parameters.Add(param2);

                        DbParameter param3 = comm.CreateParameter();
                        param3.Direction = ParameterDirection.Input;
                        param3.ParameterName = "@email";
                        param3.Value = "Paul@Grey.com";
                        comm.Parameters.Add(param3);

                        object lastInsertedRowId = comm.ExecuteScalar();

                        Console.WriteLine("New record ID: {0}, expected number > 1", lastInsertedRowId);
                        Assert.AreNotEqual("", lastInsertedRowId.ToString());
                        Assert.AreNotEqual("0", lastInsertedRowId.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                // relancer l'exception telle que catchée comme ça le test est correctement en erreur
                throw;
            }
        }
    }
}
