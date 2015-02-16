using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OsamesMicroOrm.Configuration;
using OsamesMicroOrm.Logging;
using OsamesMicroOrm.Utilities;
using SampleDbEntities.Chinook;
using TestOsamesMicroOrm.TestDbEntities;
using OsamesMicroOrm;
using Common = TestOsamesMicroOrm.Tools.Common;

namespace TestOsamesMicroOrm
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class TestMappingTools : OsamesMicroOrmTest
    {
        [TestInitialize]
        public override void Setup()
        {
            var tempo = ConfigurationLoader.Instance;
            // Pas de DB déployée donc ne pas appeler InitializeDbConnexion();
        }

        /// <summary>
        /// Case OK.
        /// </summary>
        [ExcludeFromCodeCoverage]
        [TestCategory("Mapping")]
        [Owner("Barbara Post")]
        [TestMethod]
        public void TestGetDbEntityDictionnaryMappingKeyOk()
        {
            Employee entityEmployee = new Employee();
            Customer entityCustomer = new Customer();
            Invoice entityInvoice = new Invoice();
            InvoiceLine entityInvoiceLineI = new InvoiceLine();
            Track entityTrack = new Track();

            Assert.AreEqual("Employee", MappingTools.GetDbEntityDictionnaryMappingKey(entityEmployee));
            Assert.AreEqual("Customer", MappingTools.GetDbEntityDictionnaryMappingKey(entityCustomer));
            Assert.AreEqual("Invoice", MappingTools.GetDbEntityDictionnaryMappingKey(entityInvoice));
            Assert.AreEqual("InvoiceLine", MappingTools.GetDbEntityDictionnaryMappingKey(entityInvoiceLineI));
            Assert.AreEqual("Track", MappingTools.GetDbEntityDictionnaryMappingKey(entityTrack));
        }

        /// <summary>
        /// Case NOK, no mapping attribute on this class.
        /// </summary>
        [ExcludeFromCodeCoverage]
        [TestCategory("Mapping")]
        [Owner("Barbara Post")]
        [TestMethod]
        [ExpectedException(typeof(OOrmHandledException))]
        public void TestGetDbEntityDictionnaryMappingKeyNOkMissingAttribute()
        {
            TestUnmappedEntity entity = new TestUnmappedEntity();
            try
            {
                string test = MappingTools.GetDbEntityDictionnaryMappingKey(entity);
            }
            catch (OOrmHandledException ex)
            {
                Common.AssertOnHresultAndWriteToConsole(HResultEnum.E_TYPENOTDEFINEDBMAPPINGATTRIBUTE, ex);
                throw;
            }

        }

        /// <summary>
        /// Case NOK, empty mapping attribute on this class.
        /// </summary>
        [ExcludeFromCodeCoverage]
        [TestCategory("Mapping")]
        [Owner("Barbara Post")]
        [TestMethod]
        [ExpectedException(typeof(OOrmHandledException))]
        public void TestGetDbEntityDictionnaryMappingKeyNOkEmptyAttribute()
        {
            TestEmptyMappingEntity entity = new TestEmptyMappingEntity();
            try
            {
                string test = MappingTools.GetDbEntityDictionnaryMappingKey(entity);
                Assert.Fail("Test didn't fail");
            }
            catch (OOrmHandledException ex)
            {
                Common.AssertOnHresultAndWriteToConsole(HResultEnum.E_TYPEDEFINESEMPTYDBMAPPINGATTRIBUTE, ex);
                throw;
            }

        }

        /// <summary>
        /// Case NOK, mapping attribute on this class value doesn't match a key in mapping dictionary.
        /// </summary>
        [ExcludeFromCodeCoverage]
        [TestCategory("Mapping")]
        [Owner("Barbara Post")]
        [TestMethod]
        [ExpectedException(typeof(OOrmHandledException))]
        public void TestGetDbEntityDictionnaryMappingKeyWrongValueMappingAttribute()
        {
            TestWrongMappingEntity entity = new TestWrongMappingEntity();
            try
            {
                string test = MappingTools.GetDbEntityDictionnaryMappingKey(entity);
            }
            catch (OOrmHandledException ex)
            {
                Common.AssertOnHresultAndWriteToConsole(HResultEnum.E_NOMAPPINGKEY, ex);
                throw;
            }
        }
    }
}
