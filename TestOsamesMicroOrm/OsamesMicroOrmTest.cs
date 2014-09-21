using System;
using System.Data.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics.CodeAnalysis;
using OsamesMicroOrm;
using OsamesMicroOrm.Configuration;
using TestOsamesMicroOrm.Tools;

namespace TestOsamesMicroOrm
{
    /// <summary>
    /// Base class of tests.
    /// Useful for centralizing deployment items declarations
    /// </summary>
    [
        // répertoire copié depuis le projet testé
        DeploymentItem(Common.CST_CONFIG, Common.CST_CONFIG),
        DeploymentItem("Logs", "Logs"),
        // Copier la DB car l'ORM cherche à valider une connexion DB au démarrage
        DeploymentItem("DB", "DB")
     ]
    [ExcludeFromCodeCoverage]
    public abstract class OsamesMicroOrmTest
    {
        protected static ConfigurationLoader _config;

        /// <summary>
        /// Every test uses a transaction.
        /// </summary>
        protected static DbTransaction _transaction;

        protected static DbConnection _connection;

        /// <summary>
        /// Initialisation des TUs.
        /// <para>Initialise l'ORM en lisant les fichiers de configuration</para>
        /// </summary>
        [TestInitialize]
        public virtual void Setup()
        {
            ConfigurationLoader.Clear();
            _config = ConfigurationLoader.Instance;
        }

        [TestCleanup]
        public virtual void TestCleanup()
        {
            if (_transaction != null)
                DbManager.Instance.RollbackTransaction(_transaction);

            DbManager.Instance.DisposeConnection(ref _connection);
        }

        /// <summary>
        /// Initialisation de la connexion et ouverture d'une transaction.
        /// Ne doit pas être appelé si la base de données n'est pas copiée en post-deployment item.
        /// </summary>
        public virtual void InitializeDbConnexion()
        {
            _connection = DbManager.Instance.CreateConnection();
            _transaction = DbManager.Instance.OpenTransaction(_connection);
        }
    }
}
