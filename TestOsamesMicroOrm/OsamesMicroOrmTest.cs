﻿using System;
using System.Data.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics.CodeAnalysis;
using OsamesMicroOrm;
using OsamesMicroOrm.Configuration;
using TestOsamesMicroOrm.Tools;
using DbConnection = OsamesMicroOrm.OOrmDbConnectionWrapper;
using DbTransaction = OsamesMicroOrm.OOrmDbTransactionWrapper;

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
        protected static OOrmDbTransactionWrapper _transaction;

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

        /// <summary>
        /// Cleanup.
        /// Si on avait une transaction (ce qui est le cas si on effectue un appel à la base de données, d'après la façon dont les TUs sont organisés),
        /// libération explicite des ressources :
        /// - rollback de la transaction
        /// - fermeture de la connexion
        /// </summary>
        [TestCleanup]
        public virtual void TestCleanup()
        {
            if(_transaction == null)
                return;

            // Connexion associée
            //OOrmDbConnectionWrapper connection = _transaction.ConnectionWrapper;
            // Libération des ressources
            DbManager.Instance.RollbackTransaction(_transaction);
            //DbManager.Instance.DisposeConnection(connection);
            _transaction = null;
        }

        /// <summary>
        /// Initialisation de la connexion et ouverture d'une transaction.
        /// Ne doit pas être appelé si la base de données n'est pas copiée en post-deployment item.
        /// </summary>
        public virtual void InitializeDbConnexion()
        {
            _transaction = DbManager.Instance.BeginTransaction();
        }
    }
}
