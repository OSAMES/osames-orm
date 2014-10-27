using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OsamesMicroOrm;
using OsamesMicroOrm.Configuration;
using OsamesMicroOrm.DbTools;
using SampleDbEntities.Chinook;

namespace TestOsamesMicroOrmSqlite
{
    /// <summary>
    /// Tests unitaires de haut niveau des méthodes d'exécution d'instructions SQL UPDATE, avec formatage de la requête et des paramètres ADO.NET.
    /// But : test complet de l'ORM.
    /// </summary>
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class TestDbToolsUpdate : OsamesMicroOrmSqliteTest
    {
        /// <summary>
        /// Update d'un seul objet.
        /// </summary>
        [TestMethod]
        [TestCategory("SqLite")]
        [TestCategory("Update")]
        [Owner("Benjamin Nolmans")]
        public void TestUpdateSingle()
        {

            const int testCustomerId = 3;

            _config = ConfigurationLoader.Instance;

            // Lecture initiale
            Customer customer = DbToolsSelects.SelectSingleAllColumns<Customer>("BaseReadAllWhere", "Customer", new List<string> {"IdCustomer", "#"}, new List<object> {testCustomerId}, _transaction);

            string nomInitial = customer.LastName;
            string prenomInitial = customer.FirstName;

            Console.WriteLine("Nom : " + nomInitial + " prénom : " + prenomInitial);

            Assert.AreNotEqual("Nolmans", nomInitial, "Données de début de test pas dans la bonne version en base de données");
            Assert.AreNotEqual("Benjamin", prenomInitial, "Données de début de test pas dans la bonne version en base de données");

            customer.FirstName = "Benjamin";
            customer.LastName = "Nolmans";
            
            string errorMsg;
            
            // Partie where : "propriété IdCustomer = @xxx", donc paramètres "IdCustomer" et "#" pour paramètre dynamique
            int testing = DbToolsUpdates.Update(customer, "BaseUpdateOne", "Customer", 
                new List<string> { "FirstName", "LastName" }, new List<string> { "IdCustomer", "#" }, new List<object> { customer.IdCustomer }, 
                out errorMsg, _transaction);

            Assert.AreEqual(1, testing);
            Assert.AreEqual("", errorMsg ?? "", "Attendu pas d'erreur");

            // Refaire un select, on lit la nouvelle valeur
            Customer reReadcustomer = DbToolsSelects.SelectSingleAllColumns<Customer>("BaseReadAllWhere", "Customer", new List<string> { "IdCustomer", "#" }, new List<object> { testCustomerId }, _transaction);

            string nomUpdated = reReadcustomer.LastName;
            string prenomUpdated = reReadcustomer.FirstName;

            Console.WriteLine("Nom : " + nomUpdated + " prénom : " + prenomUpdated);

            Assert.AreEqual("Nolmans", nomUpdated, "Les données relues doivent correspondre à ce qui a été mis à jour (on est dans la transaction)");
            Assert.AreEqual("Benjamin", prenomUpdated, "Les données relues doivent correspondre à ce qui a été mis à jour (on est dans la transaction)");

            // Maintenant on rollback la transaction pour annuler les modifs et on relit.
            DbManager.Instance.RollbackTransaction(_transaction);

            // Refaire un select, on lit l'ancienne valeur
            _transaction = DbManager.Instance.OpenTransaction(_connection);

            Customer reReadInitialcustomer = DbToolsSelects.SelectSingleAllColumns<Customer>("BaseReadAllWhere", "Customer", new List<string> { "IdCustomer", "#" }, new List<object> { testCustomerId }, _transaction);

            string nomNonModifie = reReadInitialcustomer.LastName;
            string prenomNonModifie = reReadInitialcustomer.FirstName;

            Console.WriteLine("Nom : " + nomNonModifie + " prénom : " + prenomNonModifie);

            Assert.AreNotEqual("Nolmans", nomNonModifie, "Données de fin de test après rollback pas dans la bonne version en base de données");
            Assert.AreNotEqual("Benjamin", prenomNonModifie, "Données de fin de test après rollback pas dans la bonne version en base de données");


        }

    }
}
