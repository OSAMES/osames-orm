using System;
using System.CodeDom;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OsamesMicroOrm.Configuration;
using OsamesMicroOrm.Utilities;
using SampleDbEntities.Chinook;
using TestOsamesMicroOrm.TestDbEntities;

namespace TestOsamesMicroOrm
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class TestMappingTools : OsamesMicroOrmTest
    {
        [TestInitialize]
        public override void Setup()
        {
            InitializeDbConnexion();
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
            var init = ConfigurationLoader.Instance;

            Employee entityEmployee = new Employee();
            Customer entityCustomer = new Customer();
            Invoice entityInvoice = new Invoice();
            InvoiceLine entityInvoiceLineI = new InvoiceLine();
            Track entityTrack = new Track();

            Assert.AreEqual("employee", MappingTools.GetDbEntityDictionnaryMappingKey(entityEmployee));
            Assert.AreEqual("customer", MappingTools.GetDbEntityDictionnaryMappingKey(entityCustomer));
            Assert.AreEqual("invoice", MappingTools.GetDbEntityDictionnaryMappingKey(entityInvoice));
            Assert.AreEqual("invoiceline", MappingTools.GetDbEntityDictionnaryMappingKey(entityInvoiceLineI));
            Assert.AreEqual("track", MappingTools.GetDbEntityDictionnaryMappingKey(entityTrack));
        }

        /// <summary>
        /// Case NOK, no mapping attribute on this class.
        /// </summary>
        [ExcludeFromCodeCoverage]
        [TestCategory("Mapping")]
        [Owner("Barbara Post")]
        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void TestGetDbEntityDictionnaryMappingKeyNOkMissingAttribute()
        {
            var init = ConfigurationLoader.Instance;

            TestUnmappedEntity entity = new TestUnmappedEntity();
            try
            {
                string test = MappingTools.GetDbEntityDictionnaryMappingKey(entity);
                Assert.Fail("Test didn't fail");
            }
            catch (Exception ex)
            {
                ConfigurationLoader._loggerTraceSource.TraceEvent(TraceEventType.Information, 0, ex.Message);
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
        [ExpectedException(typeof(Exception))]
        public void TestGetDbEntityDictionnaryMappingKeyNOkEmptyAttribute()
        {
            var init = ConfigurationLoader.Instance;

            TestEmptyMappingEntity entity = new TestEmptyMappingEntity();
            try
            {
                string test = MappingTools.GetDbEntityDictionnaryMappingKey(entity);
                Assert.Fail("Test didn't fail");
            }
            catch (Exception ex)
            {
                ConfigurationLoader._loggerTraceSource.TraceEvent(TraceEventType.Information, 0, ex.Message);
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
        [ExpectedException(typeof(Exception))]
        public void TestGetDbEntityDictionnaryMappingKeyWrongValueMappingAttribute()
        {
            var init = ConfigurationLoader.Instance;

            TestWrongMappingEntity entity = new TestWrongMappingEntity();
            try
            {
                string test = MappingTools.GetDbEntityDictionnaryMappingKey(entity);
                Assert.Fail("Test didn't fail");
            }
            catch (Exception ex)
            {
                ConfigurationLoader._loggerTraceSource.TraceEvent(TraceEventType.Information, 0, ex.Message);
                throw;
            }

        }
    }
}
