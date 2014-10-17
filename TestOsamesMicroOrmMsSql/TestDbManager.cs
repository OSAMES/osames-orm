using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OsamesMicroOrm;

namespace TestOsamesMicroOrmMsSql
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class SqliteTestDbManager : OsamesMicroOrmMsSqlTest
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
                // changement de la connexion string
                DbManager.ConnectionString += @"Pooling=True;Max Pool Size=10";
                for (int i = 0; i < 9; i++)
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
                // en plus, réduction du temps d'exécution du TU par un petit timeout
                DbManager.ConnectionString += @"Pooling=True;Max Pool Size=10;Connection Timeout = 1;";
                for (int i = 0; i < 11; i++)
                {
                    // On a une exception dès qu'on demande 10 connexions, 
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
    }
}
