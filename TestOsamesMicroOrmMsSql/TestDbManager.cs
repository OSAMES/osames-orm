using System;
using System.Collections.Generic;
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
                    // On a une exception dès qu'on demande 9 connexions, on en demande 8 ici. 
                    // Il faut prendre en compte celle d'initialisation de l'ORM qui est oujours ouverte et peut-être une autre ??
                    lstConnections.Add(DbManager.Instance.DbProviderFactory.CreateConnection());
                    DbManager.Instance.ExecuteScalar("select count(*) from Customer");
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
                DbManager.ConnectionString += @"Pooling=True;Max Pool Size=10";
                for (int i = 0; i < 12; i++)
                {
                    // On a une exception dès qu'on demande 11 connexions, 
                    lstConnections.Add(DbManager.Instance.DbProviderFactory.CreateConnection());

                     using (System.Data.Common.DbCommand command = DbManager.Instance.DbProviderFactory.CreateCommand())
                     {
                         // TODO ORM-94 continuer ici à utiliser uniquement les objets ADO.NET et mettre à jour dans les 3 autres TUs de ce type
                        // command (positionner...)
                         command.ExecuteScalar();
                     }
                    //DbManager.Instance.ExecuteScalar("select count(*) from Customer");
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
