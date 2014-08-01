/*
This file is part of OSAMES Micro ORM.
Copyright 2014 OSAMES

OSAMES Micro ORM is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

OSAMES Micro ORM is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with OSAMES Micro ORM.  If not, see <http://www.gnu.org/licenses/>.
*/

using System.Data.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OsamesMicroOrm;
using OsamesMicroOrm.Configuration;
using OsamesMicroOrm.Configuration.Tweak;

namespace TestOsamesMicroOrmSqlite
{
    /// <summary>
    /// Base class of tests.
    /// Useful for centralizing deployment items declarations.
    /// Additions to TestOsamesMicroOrm deployment (configuration and logs folders).
    /// </summary>
    [
        DeploymentItem("DB", "DB"),
        DeploymentItem("x64", "x64"),
        DeploymentItem("x86", "x86"),
        DeploymentItem("System.Data.SQLite.dll")
    ]
    [TestClass]
    public abstract class OsamesMicroOrmSqliteTest : TestOsamesMicroOrm.OsamesMicroOrmTest
    {
        protected static ConfigurationLoader _config;

        /// <summary>
        /// Every test uses a transaction.
        /// </summary>
        protected static DbTransaction _transaction;

        protected static DbConnection _connection;

        [TestInitialize]
        public void Setup()
        {
            Customizer.ConfigurationManagerSetKeyValue(Customizer.AppSettingsKeys.activeDbConnection.ToString(), "OsamesMicroORM.Sqlite");

            _config = ConfigurationLoader.Instance;

            _connection = DbManager.Instance.CreateConnection();
            _transaction = DbManager.Instance.OpenTransaction(_connection);

        }


        [TestCleanup]
        public void TestCleanup()
        {
            DbManager.Instance.RollbackTransaction(_transaction);
            _connection.Close();
            _connection.Dispose();

        }
    }
}
