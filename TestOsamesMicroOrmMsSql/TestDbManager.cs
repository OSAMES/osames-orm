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
        /// </summary>
        [TestMethod]
        [TestCategory("MsSql")]
        [TestCategory("ADO.NET pooling")]
        public void TestNotExhaustingPoolMsSql()
        {
            List<DbConnection> lstConnections = new List<DbConnection>();
            try
            {
                // changement de la connexion string
                DbManager.ConnectionString += @"Pooling=True;Max Pool Size=10";
                for (int i = 0; i < 9; i++)
                {
                    // On a une exception dès qu'on demande 9 connexions, on en demande 8 ici. 
                    // Il faut prendre en compte celle d'initialisation de l'ORM qui est oujours ouverte et peut-être une autre ??
                    lstConnections.Add(DbManager.Instance.CreateConnection());
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
                foreach (DbConnection connection in lstConnections)
                {
                    connection.Close();
                    connection.Dispose();
                }
            }
        }

        /// <summary>
        /// Test de comportement du provider factory MsSQL en cas de demande et ouverture de plus de connexions que le pool peut en donner.
        /// </summary>
        [TestMethod]
        [TestCategory("MsSql")]
        [TestCategory("ADO.NET pooling")]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestExhaustingPoolMsSql()
        {
            List<DbConnection> lstConnections = new List<DbConnection>();
            try
            {
                // changement de la connexion string
                DbManager.ConnectionString += @"Pooling=True;Max Pool Size=10";
                for (int i = 0; i < 10; i++)
                {
                    // On a une exception dès qu'on demande 9 connexions, 
                    // Il faut prendre en compte celle d'initialisation de l'ORM qui est oujours ouverte et peut-être une autre ??
                    lstConnections.Add(DbManager.Instance.CreateConnection());
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
                foreach (DbConnection connection in lstConnections)
                {
                    connection.Close();
                    connection.Dispose();
                }
            }
        }
    }
}
