using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OsamesMicroOrm;
using TestOsamesMicroOrm;
using DbConnection = OsamesMicroOrm.OOrmDbConnectionWrapper;

namespace TestOsamesMicroOrmMsSql
{
    /// <summary>
    /// Test de ADO.NET sans instancier une transaction et une connexion de base pour les tests unitaires.
    /// </summary>
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class TestAdoNetAlone : OsamesMicroOrmTest
    {
        /// <summary>
        /// Test de comportement du provider factory MsSQL en cas de demande et ouverture de moins de connexions que le pool peut en donner.
        /// Travail avec DbConnection de ADO.NET, pas DbConnectionWrapper de l'ORM.
        /// </summary>
        [TestMethod]
        [TestCategory("MsSql")]
        [TestCategory("ADO.NET pooling")]
        public void TestNotExhaustingPoolMsSql()
        {
            List<System.Data.Common.DbConnection> lstConnections = new List<System.Data.Common.DbConnection>();
            try
            {
                // changement de la connexion string, réduction du timeout à 5s.
                string cs = DbManager.ConnectionString;
                ConfigurePool(ref cs, 10, 5);
                for (int i = 0; i < 10; i++)
                {
                    lstConnections.Add(DbManager.Instance.DbProviderFactory.CreateConnection());
                    lstConnections[i].ConnectionString = cs;
                    lstConnections[i].Open();

                    using (System.Data.Common.DbCommand command = DbManager.Instance.DbProviderFactory.CreateCommand())
                    {
                        Assert.IsNotNull(command, "Commande non créée");
                        command.Connection = lstConnections[i];
                        command.CommandText = "select count(*) from Customer";
                        command.CommandType = CommandType.Text;
                        command.ExecuteScalar();
                    }
                    Console.WriteLine("pool number " + i.ToString());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("On est dans le catch");
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
        /// Test de comportement du provider factory MsSQL en cas de demande et ouverture de plus de connexions que le pool peut en donner.
        /// Travail avec DbConnection de ADO.NET, pas DbConnectionWrapper de l'ORM.
        /// </summary>
        [TestMethod]
        [TestCategory("MsSql")]
        [TestCategory("ADO.NET pooling")]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestExhaustingPoolMsSql()
        {
            List<System.Data.Common.DbConnection> lstConnections = new List<System.Data.Common.DbConnection>();
            try
            {
                // changement de la connexion string
                // en plus, réduction du temps d'exécution du TU par un petit timeout de 5s
                string cs = DbManager.ConnectionString;
                ConfigurePool(ref cs, 10, 5);
                for (int i = 0; i < 11; i++)
                {
                    // On a une exception dès que i atteint 10 avec un pool de 10 : 10 connexions (i = 0 à 9).
                    // En effet l'initialisation des tests unitaires a créé une transaction donc une première connexion.
                    // En tout on essaie donc bien d'en obtenir 1 + 10 = 11 soit une de plus que le pool.
                    lstConnections.Add(DbManager.Instance.DbProviderFactory.CreateConnection());
                    lstConnections[i].ConnectionString = cs;
                    lstConnections[i].Open();

                    using (System.Data.Common.DbCommand command = DbManager.Instance.DbProviderFactory.CreateCommand())
                    {
                        Assert.IsNotNull(command, "Commande non créée");
                        command.Connection = lstConnections[i];
                        command.CommandText = "select count(*) from Customer";
                        command.CommandType = CommandType.Text;
                        command.ExecuteScalar();
                    }
                    Console.WriteLine("pool number " + i.ToString());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("On est dans le catch");
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
        /// Test de comportement de DbManager en cas de demande et ouverture de plus de connexions que le pool peut en donner.
        /// Pool de connexions de 10 connexions.
        /// On doit tomber sur la connexion de secours et l'utiliser.
        /// 
        /// /!\ TU OK mais pas toujours (lancé seul ou à plusieurs) et ignoré car met 15s à s'exécuter.
        /// </summary>
        [TestMethod]
        [TestCategory("MsSql")]
        [TestCategory("ADO.NET pooling")]
        [Ignore]
        public void TestGettingSafeConnectionFromPoolOfTen()
        {
            // Pool de 10 : donne 9 connexions puis la connexion de secours.
            TestGettingOneMoreOrBackupConnection(10, 9);
        }

        /// <summary>
        /// Test de comportement de DbManager en cas de demande et ouverture de plus de connexions que le pool peut en donner.
        /// Pool de connexions de 1 connexion.
        /// On doit tomber sur la connexion de secours et l'utiliser.
        /// 
        /// /!\ TU OK (lancé seul ou à plusieurs) mais ignoré car met 12s à s'exécuter.
        /// </summary>
        [TestMethod]
        [TestCategory("MsSql")]
        [TestCategory("ADO.NET pooling")]
        [Ignore]
        public void TestGettingSafeConnectionFromPoolOfOne()
        {
            // Le pool de 1 retourne la connexion de secours uniquement.
            TestGettingOneMoreOrBackupConnection(1, 0);
        }

        /// <summary>
        /// Code commun aux TU pour demander des connexions au pool et vérifier le résultat obtenu.
        /// </summary>
        /// <param name="testPoolSize_"></param>
        /// <param name="backupConnexionExpectedIndex_"></param>
        private void TestGettingOneMoreOrBackupConnection(int testPoolSize_, int backupConnexionExpectedIndex_)
        {
            List<DbConnection> lstConnections = new List<DbConnection>();
            try
            {
                // Pool de N connexions.
                // en plus, réduction du temps d'exécution du TU par un petit timeout de 3s
                string cs = DbManager.ConnectionString;
                ConfigurePool(ref cs, testPoolSize_, 3);
                // On réassigne à DbManager.
                DbManager.ConnectionString = cs;
                const int iAdditionalConnectionsAskedFor = 3;
                for (int i = 0; i < testPoolSize_ + iAdditionalConnectionsAskedFor; i++)
                {
                    // 1ère boucle : ouverture de toutes les connexions
                    lstConnections.Add(DbManager.Instance.CreateConnection());

                }
                for (int i = 0; i < testPoolSize_ + iAdditionalConnectionsAskedFor; i++)
                {
                    Console.WriteLine("[" + i + "]" + lstConnections[i]);
                    // 2e boucle : vérification du booléen sur chaque connexion
                    if (i < backupConnexionExpectedIndex_)
                    {
                        // on obtient une connexion du pool
                        Assert.IsFalse(lstConnections[i].IsBackup, "on s'attend à ce que la connexion d'index " + i + " ne soit pas celle de secours");
                    }
                    else
                    {
                        // C'est la connexion de secours qui est retournée
                        Assert.IsTrue(lstConnections[i].IsBackup, "on s'attend à ce que la connexion d'index " + i + " soit celle de secours");
                    }

                    DbManager.Instance.ExecuteScalar(lstConnections[i], "select count(*) from Customer", (object[,])null);
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
                foreach (OOrmDbConnectionWrapper connection in lstConnections)
                {
                    connection.AdoDbConnection.Close();
                    connection.Dispose();
                }
            }
        }

        /// <summary>
        /// Test de bas niveau du Insert.
        /// </summary>
        [TestMethod]
        [ExcludeFromCodeCoverage]
        [Owner("Benjamin Nolmans")]
        [TestCategory("MsSql")]
        [TestCategory("ADO.NET Insert")]
        [TestCategory("No Transaction")]
        public void TestInsertMsSqlWithoutTransaction()
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
                        // Ce TU ne passe pas tant que le correctif d'utiliser une seule commande n'est pas appliqué à la méthode avec paramètres sous la forme d'un tableau do'objets Parameter.
                        int affectedRecordsCount = DbManager.Instance.ExecuteNonQuery(conn2, CommandType.Text, "INSERT INTO Customer (LastName, FirstName, Email) VALUES (@lastname, @firstname, @email)", parameters.ToArray(), out lastInsertedRowId);
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
        public void TestInsertMsSqlPureAdoNetWithoutTransaction()
        {
           try
            {
                using (var conn1 = DbManager.Instance.DbProviderFactory.CreateConnection())
                {
                    conn1.ConnectionString = DbManager.ConnectionString;
                    conn1.Open();
                    using (DbCommand comm = DbManager.Instance.DbProviderFactory.CreateCommand())
                    {
                        comm.CommandText = "INSERT INTO Customer (LastName, FirstName, Email) VALUES (@lastname, @firstname, @email); select scope_identity()";
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

        /// <summary>
        /// Utilitaire de modification de valeurs dans une connection string.
        /// </summary>
        /// <param name="connectionString_"></param>
        /// <param name="maxPoolSize_"></param>
        /// <param name="connectionTimeout_"></param>
        private void ConfigurePool(ref string connectionString_, int maxPoolSize_, int connectionTimeout_)
        {
            DbConnectionStringBuilder tool = new DbConnectionStringBuilder { ConnectionString = connectionString_ };
            tool["Pooling"] = "True";
            tool["Max Pool Size"] = maxPoolSize_;
            tool["Connection Timeout"] = connectionTimeout_;

            connectionString_ = tool.ConnectionString;

        }
    }
}
