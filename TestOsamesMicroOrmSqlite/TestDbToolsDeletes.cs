using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OsamesMicroOrm.DbTools;

namespace TestOsamesMicroOrmSqlite
{
    /// <summary>
    /// Tests unitaires de haut niveau des méthodes d'exécution d'instructions SQL DELETE, avec formatage de la requête et des paramètres ADO.NET.
    /// But : test complet de l'ORM.
    /// </summary>
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class TestDbToolsDeletes : OsamesMicroOrmSqliteTest
    {
        /// <summary>
        /// Delete d'un objet existant.
        /// Attention, ce test efface sans être bloqué par une FK. Ce qui n'est pas le cas sous MSSql : DB Sqlite pas ok ?
        /// </summary>
        [TestMethod]
        [TestCategory("SqLite")]
        [TestCategory("Delete")]
        public void TestDelete()
        {
            long nb = DbToolsDeletes.Delete("BaseDeleteWhere", "Customer", new List<string> {"IdCustomer", "#"}, new List<object> {3}, _transaction);
            Assert.AreEqual(1, nb, "1 ligne doit être effacée");
        }

        /// <summary>
        /// Delete d'un objet inexistant.
        /// </summary>
        [TestMethod]
        [TestCategory("SqLite")]
        [TestCategory("Delete")]
        public void TestDeleteNoMatch()
        {
            long nb = DbToolsDeletes.Delete("BaseDeleteWhere", "Customer", new List<string> { "IdCustomer", "#" }, new List<object> { -1 }, _transaction);
            Assert.AreEqual(0, nb, "0 ligne doit être effacée");
        }


    }
}
