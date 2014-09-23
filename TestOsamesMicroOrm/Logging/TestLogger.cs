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

using System;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OsamesMicroOrm.Configuration;
using OsamesMicroOrm.Logging;

namespace TestOsamesMicroOrm.Logging
{
    [TestClass]
    public class TestLogger
    {
        /// <summary>
        /// Test d'appel du logger.
        /// </summary>
        [TestMethod]
        [TestCategory("Configuration")]
        public void Test()
        {
            var temp = ConfigurationLoader.Instance;
            
            Logger.Log(TraceEventType.Information, "Information à tracer dans le log");
            Logger.Log(TraceEventType.Error, new NullReferenceException("Exception de test"));
        }

    }
}
