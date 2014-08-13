using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
     ]
    public abstract class OsamesMicroOrmTest
    {
        /// <summary>
        /// Initialisation des TUs.
        /// <para>Initialise l'ORM en lisant les fichiers de configuration</para>
        /// </summary>
        [TestInitialize]
        public virtual void Setup()
        {
            ConfigurationLoader.Clear();
            var _config = ConfigurationLoader.Instance;
        }

        /// <summary>
        /// Initialisation de la connexion et ouverture d'une transaction.
        /// </summary>
        public virtual void InitializeDbConnexion()
        {
           var  _connection = DbManager.Instance.CreateConnection();
            var _transaction = DbManager.Instance.OpenTransaction(_connection);
        }
    }
}
