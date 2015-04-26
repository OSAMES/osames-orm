using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OsamesMicroOrm;
using OsamesMicroOrm.Configuration;
using OsamesMicroOrm.DbTools;
using SampleDbEntities.Chinook;

namespace TestOsamesMicroOrmMsSql
{
    /// <summary>
    /// Tests unitaires de haut niveau des méthodes d'exécution d'instructions SQL UPDATE, avec formatage de la requête et des paramètres ADO.NET.
    /// But : test complet de l'ORM.
    /// </summary>
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class TestDbToolsUpdate : OsamesMicroOrmMsSqlTest
    {
        /// <summary>
        /// Update d'un seul objet.
        /// </summary>
        [TestMethod]
        [TestCategory("MsSql")]
        [TestCategory("Update")]
        [Owner("Benjamin Nolmans")]
        public void TestUpdateSingleMsSql()
        {
            // MS SQL n'aime pas les uint, seulement des int...
            const int testCustomerId = 3;

            _config = ConfigurationLoader.Instance;

            // Lecture initiale
            Customer customer = DbToolsSelects.SelectSingleAllColumns<Customer>("BaseReadAllWhere",  new List<string> {"IdCustomer", "#"}, new List<object> {testCustomerId}, _transaction);

            string nomInitial = customer.LastName;
            string prenomInitial = customer.FirstName;

            Console.WriteLine("En début de test : Nom : " + nomInitial + " prénom : " + prenomInitial);

            Assert.AreNotEqual("Nolmans", nomInitial, "Données de début de test pas dans la bonne version en base de données");
            Assert.AreNotEqual("Benjamin", prenomInitial, "Données de début de test pas dans la bonne version en base de données");

            customer.FirstName = "Benjamin";
            customer.LastName = "Nolmans";
                       
            // Partie where : "propriété IdCustomer = @xxx", donc paramètres "IdCustomer" et "#" pour paramètre dynamique
            uint testing = DbToolsUpdates.Update(customer, "BaseUpdateOne",  
                new List<string> { "FirstName", "LastName" }, new List<string> { "IdCustomer", "#" }, new List<object> { customer.IdCustomer }, _transaction);

            // il faut caster car sinon "1" est de type int.
            Assert.AreEqual((uint)1, testing);

            // Refaire un select, on lit la nouvelle valeur
            Customer reReadcustomer = DbToolsSelects.SelectSingleAllColumns<Customer>("BaseReadAllWhere",  new List<string> { "IdCustomer", "#" }, new List<object> { testCustomerId }, _transaction);

            string nomUpdated = reReadcustomer.LastName;
            string prenomUpdated = reReadcustomer.FirstName;

            Console.WriteLine("Après l'update, dans la transaction : Nom : " + nomUpdated + " prénom : " + prenomUpdated);

            Assert.AreEqual("Nolmans", nomUpdated, "Les données relues doivent correspondre à ce qui a été mis à jour (on est dans la transaction)");
            Assert.AreEqual("Benjamin", prenomUpdated, "Les données relues doivent correspondre à ce qui a été mis à jour (on est dans la transaction)");

            // Maintenant on rollback la transaction pour annuler les modifs et on relit.
            DbManager.Instance.RollbackTransaction(_transaction);

            // Refaire un select, on lit l'ancienne valeur après le rollback.
            // On réutilise la connexion et on rouvre une transaction
            // teste de robustesse des wrapper en réutilisant expres la connexion
            // (chose que l'on ne peut faire car cette methode est internal)
            _transaction = DbManager.Instance.OpenTransaction(_transaction.ConnectionWrapper);

            Customer reReadInitialcustomer = DbToolsSelects.SelectSingleAllColumns<Customer>("BaseReadAllWhere",  new List<string> { "IdCustomer", "#" }, new List<object> { testCustomerId }, _transaction);

            string nomNonModifie = reReadInitialcustomer.LastName;
            string prenomNonModifie = reReadInitialcustomer.FirstName;

            Console.WriteLine("Avec une autre transaction, après rollback de la première transaction : Nom : " + nomNonModifie + " prénom : " + prenomNonModifie);

            Assert.AreNotEqual("Nolmans", nomNonModifie, "Données de fin de test après rollback pas dans la bonne version en base de données");
            Assert.AreNotEqual("Benjamin", prenomNonModifie, "Données de fin de test après rollback pas dans la bonne version en base de données");


        }

    }
}
