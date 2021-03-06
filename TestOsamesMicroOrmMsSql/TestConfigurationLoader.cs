﻿/*
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

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OsamesMicroOrm;
using OsamesMicroOrm.Configuration;
using OsamesMicroOrm.Utilities;
using Common = TestOsamesMicroOrm.Tools.Common;

namespace TestOsamesMicroOrmMsSql
{
    [TestClass]
   public class TestConfigurationLoader : OsamesMicroOrmMsSqlTest
    {

        /// <summary>
        /// Pour ce projet de TU il y a seulement un provider MsSql définis dans App.Config.
        /// </summary>
        [TestMethod]
        [ExcludeFromCodeCoverage]
        [Owner("Barbara Post")]
        [TestCategory("Configuration")]
        [TestCategory("Sql provider search")]
        public void TestFindInProviderFactoryClasses()
        {
            ConfigurationLoader tempo = ConfigurationLoader.Instance;
            try
            {
                ConfigurationLoader.FindInProviderFactoryClasses("System.Data.SqlClient");
            }
            catch (OOrmHandledException ex)
            {
                // Si jamais on a une erreur c'est celle-ci qui doit sortir, cependant on ne doit pas avoir d'erreur
                Common.AssertOnHresultAndWriteToConsole(HResultEnum.E_PROVIDERNOTINSTALLED, ex);
                throw;
            }

        }
    }
}
