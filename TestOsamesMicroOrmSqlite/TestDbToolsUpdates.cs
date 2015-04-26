using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OsamesMicroOrm;
using OsamesMicroOrm.Configuration;
using OsamesMicroOrm.Configuration.Tweak;
using OsamesMicroOrm.DbTools;
using OsamesMicroOrm.Utilities;
using SampleDbEntities.Chinook;
using Common = TestOsamesMicroOrm.Tools.Common;

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
        /// Test of FormatSqlForUpdate<T> with a list of 2 properties.
        /// </summary>
        [TestMethod]
        [TestCategory("SqLite")]
        [TestCategory("Mapping")]
        [TestCategory("Sql formatting for Update")]
        public void TestFormatSqlForUpdate()
        {

            // FormatSqlForUpdate<T>(T databaseEntityObject_, string mappingDictionariesContainerKey_, List<string> lstDataObjectPropertyName_, string primaryKeyPropertyName_, 
            //                        out string sqlCommand_, out List<KeyValuePair<string, object>> adoParameters_)

            string sqlCommand;
            List<KeyValuePair<string, object>> adoParams;
            Employee employee = new Employee { LastName = "Doe", FirstName = "John" };
            DbToolsUpdates.FormatSqlForUpdate(employee, "BaseUpdateOne", "Employee", new List<string> { "LastName", "FirstName" }, new List<string> { "EmployeeId", "#" }, new List<object> { 2 }, out sqlCommand, out adoParams);

            Assert.AreEqual("UPDATE [Employee] SET [LastName] = @lastname, [FirstName] = @firstname WHERE [EmployeeId] = @p0;", sqlCommand);
            Assert.AreEqual(3, adoParams.Count, "no parameters generated");
            Assert.AreEqual("@lastname", adoParams[0].Key);
            Assert.AreEqual(employee.LastName, adoParams[0].Value);
            Assert.AreEqual("@firstname", adoParams[1].Key);
            Assert.AreEqual(employee.FirstName, adoParams[1].Value);
            Assert.AreEqual("@p0", adoParams[2].Key);
            Assert.AreEqual(2, adoParams[2].Value);
        }

        /// <summary>
        /// Update d'un seul objet.
        /// </summary>
        [TestMethod]
        [TestCategory("SqLite")]
        [TestCategory("Update")]
        [Owner("Benjamin Nolmans")]
        public void TestUpdateSingleSqlite()
        {

            const uint testCustomerId = 3;

            _config = ConfigurationLoader.Instance;

            // Lecture initiale
            Customer customer = DbToolsSelects.SelectSingleAllColumns<Customer>("BaseReadAllWhere", "Customer", new List<string> { "IdCustomer", "#" }, new List<object> { testCustomerId }, _transaction);

            string nomInitial = customer.LastName;
            string prenomInitial = customer.FirstName;

            Console.WriteLine("En début de test : Nom : " + nomInitial + " prénom : " + prenomInitial);

            Assert.AreNotEqual("Nolmans", nomInitial, "Données de début de test pas dans la bonne version en base de données");
            Assert.AreNotEqual("Benjamin", prenomInitial, "Données de début de test pas dans la bonne version en base de données");

            customer.FirstName = "Benjamin";
            customer.LastName = "Nolmans";

            // Partie where : "propriété IdCustomer = @xxx", donc paramètres "IdCustomer" et "#" pour paramètre dynamique
            uint testing = DbToolsUpdates.Update(customer, "BaseUpdateOne", "Customer",
                new List<string> { "FirstName", "LastName" }, new List<string> { "IdCustomer", "#" }, new List<object> { customer.IdCustomer }, _transaction);

            // il faut caster car sinon "1" est de type int.
            Assert.AreEqual((uint) 1, testing);

            // Refaire un select, on lit la nouvelle valeur
            Customer reReadcustomer = DbToolsSelects.SelectSingleAllColumns<Customer>("BaseReadAllWhere", "Customer", new List<string> { "IdCustomer", "#" }, new List<object> { testCustomerId }, _transaction);

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

            Customer reReadInitialcustomer = DbToolsSelects.SelectSingleAllColumns<Customer>("BaseReadAllWhere", "Customer", new List<string> { "IdCustomer", "#" }, new List<object> { testCustomerId }, _transaction);

            string nomNonModifie = reReadInitialcustomer.LastName;
            string prenomNonModifie = reReadInitialcustomer.FirstName;

            Console.WriteLine("Avec une autre transaction, après rollback de la première transaction : Nom : " + nomNonModifie + " prénom : " + prenomNonModifie);

            Assert.AreNotEqual("Nolmans", nomNonModifie, "Données de fin de test après rollback pas dans la bonne version en base de données");
            Assert.AreNotEqual("Benjamin", prenomNonModifie, "Données de fin de test après rollback pas dans la bonne version en base de données");


        }

        /// <summary>
        /// Update d'un seul objet en n'ayant pas de ligne mise à jour.
        /// </summary>
        [TestMethod]
        [TestCategory("SqLite")]
        [TestCategory("Update")]
        [TestCategory("Visual log")]
        [Owner("Benjamin Nolmans")]
        public void TestUpdateSingleNoMatchSqlite()
        {

            const uint testCustomerId = 3;

            _config = ConfigurationLoader.Instance;

            // Lecture initiale
            Customer customer = DbToolsSelects.SelectSingleAllColumns<Customer>("BaseReadAllWhere", "Customer", new List<string> {"IdCustomer", "#"}, new List<object> {testCustomerId}, _transaction);

            // Partie where : "propriété LastName = @p0", donc paramètres "LastName" et "#" pour paramètre dynamique
            // Ici on met pour valeur de "@p0" une valeur qui fera qu'on ne trouve pas de ligne correspondante.
            uint testing = DbToolsUpdates.Update(customer, "BaseUpdateOne", "Customer",
                new List<string> {"FirstName", "LastName"}, new List<string> {"LastName", "#"}, new List<object> {"???" + customer.LastName + "??"}, _transaction);

            // il faut caster car sinon "0" est de type int.
            Assert.AreEqual((uint) 0, testing);
        }

        /// <summary>
        /// Update d'un seul objet en ne précisant aucune donnée à mettre mise à jour.
        /// </summary>
        [TestMethod]
        [TestCategory("SqLite")]
        [TestCategory("Update")]
        [TestCategory("Visual log")]
        [Owner("Benjamin Nolmans")]
        public void TestUpdateSingleNoDataToUpdateSqlite()
        {

            const uint testCustomerId = 3;

            _config = ConfigurationLoader.Instance;

            // Lecture initiale
            Customer customer = DbToolsSelects.SelectSingleAllColumns<Customer>("BaseReadAllWhere", "Customer", new List<string> { "IdCustomer", "#" }, new List<object> { testCustomerId }, _transaction);
 
            // Partie where : "propriété LastName = @p0", donc paramètres "LastName" et "#" pour paramètre dynamique
            // Ici on met pour valeur de "@p0" une valeur qui fera qu'on ne trouve pas de ligne correspondante.
            uint testing = DbToolsUpdates.Update(customer, "BaseUpdateOne", "Customer",
                new List<string>(), new List<string> { "LastName", "#" }, new List<object> { "???" + customer.LastName + "??" }, _transaction);

            // il faut caster car sinon "0" est de type int.
            Assert.AreEqual((uint)0, testing);
        }

        /// <summary>
        /// Update d'un seul objet.
        /// </summary>
        [TestMethod]
        [TestCategory("SqLite")]
        [TestCategory("Update")]
        [TestCategory("No Transaction")]
        [Owner("Benjamin Nolmans")]
        public void TestUpdateSingleSqliteWithoutTransaction()
        {
            const uint testCustomerId = 1;

            _config = ConfigurationLoader.Instance;

            // Lecture initiale
            Customer customer = DbToolsSelects.SelectSingleAllColumns<Customer>("BaseReadAllWhere", "Customer", new List<string> { "IdCustomer", "#" }, new List<object> { testCustomerId });

            string nomInitial = customer.LastName;
            string prenomInitial = customer.FirstName;

            Console.WriteLine("En début de test : Nom : " + nomInitial + " prénom : " + prenomInitial);

            customer.FirstName = "Benjamin";
            customer.LastName = "Nolmans";

            // Partie where : "propriété IdCustomer = @xxx", donc paramètres "IdCustomer" et "#" pour paramètre dynamique
            uint testing = DbToolsUpdates.Update(customer, "BaseUpdateOne", "Customer",
                new List<string> { "FirstName", "LastName" }, new List<string> { "IdCustomer", "#" }, new List<object> { customer.IdCustomer });

            // il faut caster car sinon "1" est de type int.
            Assert.AreEqual((uint)1, testing);

        }

        /// <summary>
        /// Update d'un seul objet en omettant des valeurs obligatoires.
        /// </summary>
        [TestMethod]
        [TestCategory("SqLite")]
        [TestCategory("Update")]
        [Owner("Barbara Post")]
        [ExpectedException(typeof(OOrmHandledException))]
        public void TestUpdateSingleErrorWithoutMandatoryValuesSqlite()
        {
            const uint testCustomerId = 3;

            _config = ConfigurationLoader.Instance;

            // Lecture initiale
            Customer customer = DbToolsSelects.SelectSingleAllColumns<Customer>("BaseReadAllWhere", "Customer", new List<string> { "IdCustomer", "#" }, new List<object> { testCustomerId }, _transaction);

            string nomInitial = customer.LastName;
            string prenomInitial = customer.FirstName;

            Console.WriteLine("En début de test : Nom : " + nomInitial + " prénom : " + prenomInitial);

            Assert.IsFalse(string.IsNullOrWhiteSpace(nomInitial), "Données de début de test pas dans la bonne version en base de données");
            Assert.IsFalse(string.IsNullOrWhiteSpace(prenomInitial), "Données de début de test pas dans la bonne version en base de données");

            customer.FirstName = null;
            customer.LastName = null;

            try
            {
                // Partie where : "propriété IdCustomer = @xxx", donc paramètres "IdCustomer" et "#" pour paramètre dynamique
                DbToolsUpdates.Update(customer, "BaseUpdateOne", "Customer",
                    new List<string> { "FirstName", "LastName" }, new List<string> { "IdCustomer", "#" }, new List<object> { customer.IdCustomer }, _transaction);
            }
            catch (OOrmHandledException ex)
            {
                Common.AssertOnHresultAndWriteToConsole(HResultEnum.E_EXECUTENONQUERYFAILED, ex);
                throw;
            }

        }

        /// <summary>
        /// Update d'un seul objet en utilisant un template qui donne lieu à une commande SQL incohérente.
        /// </summary>
        [TestMethod]
        [TestCategory("SqLite")]
        [TestCategory("Update")]
        [Owner("Barbara Post")]
        [ExpectedException(typeof(OOrmHandledException))]
        public void TestUpdateSingleErrorIncorrectTemplateSqlite()
        {
            try
            {
                const uint testCustomerId = 3;
                Customizer.ConfigurationManagerSetKeyValue(Customizer.AppSettingsKeys.sqlTemplatesFileName.ToString(), "incorrect-sqltemplates.xml");
                _config = ConfigurationLoader.Instance;

                // Lecture initiale
                Customer customer = DbToolsSelects.SelectSingleAllColumns<Customer>("BaseReadAllWhere", "Customer", new List<string> { "IdCustomer", "#" }, new List<object> { testCustomerId }, _transaction);

                string nomInitial = customer.LastName;
                string prenomInitial = customer.FirstName;

                Console.WriteLine("En début de test : Nom : " + nomInitial + " prénom : " + prenomInitial);

                Assert.IsFalse(string.IsNullOrWhiteSpace(nomInitial), "Données de début de test pas dans la bonne version en base de données");
                Assert.IsFalse(string.IsNullOrWhiteSpace(prenomInitial), "Données de début de test pas dans la bonne version en base de données");

                customer.FirstName = "Test 1";
                customer.LastName = "Test 2";


                // Partie where : "propriété IdCustomer = @xxx", donc paramètres "IdCustomer" et "#" pour paramètre dynamique
                DbToolsUpdates.Update(customer, "IncorrectUpdate", "Customer",
                    new List<string> { "FirstName", "LastName" }, new List<string> { "IdCustomer", "#" }, new List<object> { customer.IdCustomer }, _transaction);
            }
            catch (OOrmHandledException ex)
            {
                Common.AssertOnHresultAndWriteToConsole(HResultEnum.E_EXECUTENONQUERYFAILED, ex);
                throw;
            }
            finally
            {
                Customizer.ConfigurationManagerRestoreKey(Customizer.AppSettingsKeys.sqlTemplatesFileName.ToString());
            }

        }

        /// <summary>
        /// Update de deux objets à la fois.
        /// </summary>
        [TestMethod]
        [TestCategory("SqLite")]
        [TestCategory("Update")]
        [Owner("Barbara Post")]
        public void TestUpdateTwoObjectsSqlite()
        {
            const uint testCustomerId = 3;
            const uint testOtherCustomerId = 4;

            _config = ConfigurationLoader.Instance;

            // Lecture initiale
            Customer customer = DbToolsSelects.SelectSingleAllColumns<Customer>("BaseReadAllWhere", "Customer", new List<string> { "IdCustomer", "#" }, new List<object> { testCustomerId }, _transaction);
            Customer otherCustomer = DbToolsSelects.SelectSingleAllColumns<Customer>("BaseReadAllWhere", "Customer", new List<string> { "IdCustomer", "#" }, new List<object> { testOtherCustomerId }, _transaction);

            string nomInitial = customer.LastName;
            string prenomInitial = customer.FirstName;

            Console.WriteLine("En début de test : Nom : " + nomInitial + " prénom : " + prenomInitial);

            customer.FirstName = "Jane";
            customer.LastName = "Birkin";

            string otherNomInitial = otherCustomer.LastName;
            string otherPrenomInitial = otherCustomer.FirstName;

            Console.WriteLine("En début de test : Nom : " + otherNomInitial + " prénom : " + otherPrenomInitial);

            otherCustomer.FirstName = "Pietro";
            otherCustomer.LastName = "Lavazza";

            // Partie where : "propriété IdCustomer = @xxx", donc paramètres "IdCustomer" et "#" pour paramètre dynamique
            uint updated = DbToolsUpdates.Update(new List<Customer> { customer, otherCustomer }, "BaseUpdateOne", "Customer",
                new List<string> { "FirstName", "LastName" }, new List<string> { "IdCustomer", "#" }, new List<List<object>> { new List<object> { customer.IdCustomer }, new List<object> { otherCustomer.IdCustomer } }, _transaction);

            // il faut caster car sinon "1" est de type int.
            Assert.AreEqual((uint)2, updated);

            // Relecture
            // "BaseReadAllWhereBetween" : SELECT * FROM {0} WHERE {1} between {2} and {3};
            List<Customer> customers = DbToolsSelects.SelectAllColumns<Customer>("BaseReadAllWhereBetween", "Customer", new List<string> {"%CustomerId", "#", "#"}, new List<object> {"CustomerId", 3, 4});
            Console.WriteLine("En fin de test [0] : ID " + customers[0].IdCustomer + " Nom : " + customers[0].LastName + " prénom : " + customers[0].FirstName);
            Console.WriteLine("En fin de test [1] : ID " + customers[1].IdCustomer + " Nom : " + customers[1].LastName + " prénom : " + customers[1].FirstName);

            Assert.AreEqual(3, customers[0].IdCustomer);
            Assert.AreEqual("Jane", customers[0].FirstName);
            Assert.AreEqual("Birkin", customers[0].LastName);

            Assert.AreEqual(4, customers[1].IdCustomer);
            Assert.AreEqual("Pietro", customers[1].FirstName);
            Assert.AreEqual("Lavazza", customers[1].LastName);

        }

    }
}
