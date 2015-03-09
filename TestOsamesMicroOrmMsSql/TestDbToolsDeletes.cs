using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OsamesMicroOrm;
using OsamesMicroOrm.DbTools;
using SampleDbEntities.Chinook;
using TestOsamesMicroOrm.Tools;

namespace TestOsamesMicroOrmMsSql
{
    /// <summary>
    /// Tests unitaires de haut niveau des méthodes d'exécution d'instructions SQL DELETE, avec formatage de la requête et des paramètres ADO.NET.
    /// But : test complet de l'ORM.
    /// </summary>
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class TestDbToolsDeletes : OsamesMicroOrmMsSqlTest
    {

        /// <summary>
        /// Delete d'un objet inexistant.
        /// </summary>
        [TestMethod]
        [TestCategory("MsSql")]
        [TestCategory("Delete")]
        public void TestDelete()
        {
            Customer toto = new Customer {FirstName = "toto", LastName = "toto", Email = "hop@test.com"};
            DbToolsInserts.Insert(toto, "BaseInsert", "Customer", new List<string> {"FirstName", "LastName", "Email"}, _transaction);
            long nb = DbToolsDeletes.Delete("BaseDeleteWhere", "Customer", new List<string> { "FirstName", "#" }, new List<object> { "toto" }, _transaction);
            Assert.AreEqual(1, nb, "1 ligne doit être effacée");
        }

        /// <summary>
        /// Delete d'un objet existant. Blocage par une FK.
        /// </summary>
        [TestMethod]
        [TestCategory("MsSql")]
        [TestCategory("Delete")]
        [ExpectedException(typeof(OOrmHandledException))]
        public void TestDeleteForbidden()
        {
            try
            {
                long nb = DbToolsDeletes.Delete("BaseDeleteWhere", "Customer", new List<string> {"IdCustomer", "#"}, new List<object> {3}, _transaction);
                Assert.AreEqual(1, nb, "1 ligne doit être effacée");
            }
            catch (OOrmHandledException ex)
            {
                Common.AssertOnHresultAndWriteToConsole(HResultEnum.E_EXECUTENONQUERYFAILED, ex);
                throw;
            }
        }


        /// <summary>
        /// Delete d'un objet inexistant.
        /// </summary>
        [TestMethod]
        [TestCategory("MsSql")]
        [TestCategory("Delete")]
        public void TestDeleteNoMatch()
        {
            long nb = DbToolsDeletes.Delete("BaseDeleteWhere", "Customer", new List<string> { "IdCustomer", "#" }, new List<object> { -1 }, _transaction);
            Assert.AreEqual(0, nb, "0 ligne doit être effacée");
        }


    }
}
