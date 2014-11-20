using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OsamesMicroOrm;
using TestOsamesMicroOrm;
using DbConnection = OsamesMicroOrm.DbConnectionWrapper;

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
        /// Travail avec DbConnection de ADO.NET, pas celui de l'ORM.
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
        /// Travail avec DbConnection de ADO.NET, pas celui de l'ORM.
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
                    // On a une exception dès qu'on demande 11 connexions avec un pool de 10
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
        /// /!\ TU OK (lancé seul ou à plusieurs) mais ignoré car met 30s à s'exécuter.
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
        /// /!\ Ce TU met 10 secondes pour s'exécuter et ne fonctionne pas si on lance tous les TUs, dans ce cas une connexion de secours a déjà 
        /// été initialisée avant d'arriver à ce TU. Pourquoi a t-on une exception mal gérée ??
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
                // en plus, réduction du temps d'exécution du TU par un petit timeout de 5s
                string cs = DbManager.ConnectionString;
                ConfigurePool(ref cs, testPoolSize_, 5);
                // On réassigne à DbManager.
                DbManager.ConnectionString = cs;
                for (int i = 0; i < testPoolSize_ + 5; i++)
                {
                    // 1ère boucle : ouverture de toutes les connexions
                    lstConnections.Add(DbManager.Instance.CreateConnection());

                }
                for (int i = 0; i < testPoolSize_ + 5; i++)
                {
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
                foreach (DbConnectionWrapper connection in lstConnections)
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
