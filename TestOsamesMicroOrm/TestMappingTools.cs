using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OsamesMicroOrm.Configuration;
using OsamesMicroOrm.Utilities;
using TestOsamesMicroOrm.TestDbEntities;

namespace TestOsamesMicroOrm
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class TestMappingTools : OsamesMicroOrmTest
    {
        /// <summary>
        /// Case OK.
        /// </summary>
        [ExcludeFromCodeCoverage]
        [TestCategory("Mapping")]
        [Owner("Barbara Post")]
        [TestMethod]
        public void TestGetDbEntityDictionnaryMappingKeyOk()
        {
            TestAdresse entity = new TestAdresse();
            Assert.AreEqual("adresses", MappingTools.GetDbEntityDictionnaryMappingKey(entity));
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
