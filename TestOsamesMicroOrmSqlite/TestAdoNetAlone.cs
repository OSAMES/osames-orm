using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OsamesMicroOrm;
using TestOsamesMicroOrm;

namespace TestOsamesMicroOrmSqlite
{
    /// <summary>
    /// Test de ADO.NET sans instancier une transaction et une connexion de base pour les tests unitaires.
    /// </summary>
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
