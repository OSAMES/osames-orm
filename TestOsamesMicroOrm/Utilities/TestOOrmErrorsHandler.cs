using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestOsamesMicroOrm.Utilities
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class TestOOrmErrorsHandler
    {
        [TestMethod]
        [TestCategory("Error handling")]
        [Owner("Barbara Post")]
        public void TestConstructor()
        {
            Dictionary<string, KeyValuePair<string, string>> dicErrors = OsamesMicroOrm.Utilities.OOrmErrorsHandler.HResultCode;
            Assert.AreNotEqual(0, dicErrors.Keys.Count);
        }

        [TestMethod]
        [TestCategory("Error handling")]
        [Owner("Benjamin Nolmans)")]
        public void TestFindHResultByCode()
        {
            Dictionary<string, KeyValuePair<string, string>> dicErrors = OsamesMicroOrm.Utilities.OOrmErrorsHandler.HResultCode;
            Assert.IsNotNull(OsamesMicroOrm.Utilities.OOrmErrorsHandler.FindHResultByCode("E_CreateConnectionFailed"));
        }
    }
}
