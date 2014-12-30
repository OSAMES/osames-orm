using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OsamesMicroOrm;

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
            Assert.AreNotEqual(0, dicErrors.Keys, "le dictionnaire ne doit pas être vide !");
            string result = OsamesMicroOrm.Utilities.OOrmErrorsHandler.FindHResultByCode(HResultEnum.E_COLUMNDOESNOTEXIST);
            Console.WriteLine(result);
            Assert.IsNotNull(result);
        }

        [TestMethod]
        [TestCategory("Error handling")]
        [Owner("Benjamin Nolmans)")]
        public void TestFormatCustomerError()
        {
            Dictionary<string, KeyValuePair<string, string>> dicErrors = OsamesMicroOrm.Utilities.OOrmErrorsHandler.HResultCode;
            Assert.AreNotEqual(0, dicErrors.Keys, "le dictionnaire ne doit pas être vide !");
            string result = OsamesMicroOrm.Utilities.OOrmErrorsHandler.FormatCustomerError(OsamesMicroOrm.Utilities.OOrmErrorsHandler.FindHResultByCode(HResultEnum.E_COMMITTRANSACTIONFAILED), "Custom message");
            Console.WriteLine(result);
            Assert.IsNotNull(result);
        }

        [TestMethod]
        [TestCategory("Error handling")]
        [Owner("Benjamin Nolmans)")]
        public void TestProcessOrmException()
        {
            Dictionary<string, KeyValuePair<string, string>> dicErrors = OsamesMicroOrm.Utilities.OOrmErrorsHandler.HResultCode;
            Assert.AreNotEqual(0, dicErrors.Keys, "le dictionnaire ne doit pas être vide !");
            string result = OsamesMicroOrm.Utilities.OOrmErrorsHandler.ProcessOrmException(HResultEnum.E_NOACTIVECONNECTIONDEFINED, "Custom message");
            Console.WriteLine(result);
            Assert.IsNotNull(result);
        }
    }
}
