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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestOsamesMicroOrm.Tools;
using TestOsamesMicroOrmMsSql.Tools;

namespace TestOsamesMicroOrmMsSql
{
    /// <summary>
    /// Base class of tests.
    /// Useful for centralizing deployment items declarations.
    /// Additions to TestOsamesMicroOrm deployment (configuration and logs folders).
    /// </summary>
    [
        // Configuration for MsSql
        DeploymentItem(CommonMsSql.CST_TEST_CONFIG_MSSQL, Common.CST_CONFIG)
    ]
    [TestClass]
    public abstract class OsamesMicroOrmMsSqlTest : TestOsamesMicroOrm.OsamesMicroOrmTest
    {
        /// <summary>
        /// Initialisation d'une connexion et sa transaction, pour chaque test de la classe.
        /// </summary>
        [TestInitialize]
        public void SetupTest()
        {

        }
        
        [TestCleanup]
        public override void TestCleanup()
        {

        }
    }
}
