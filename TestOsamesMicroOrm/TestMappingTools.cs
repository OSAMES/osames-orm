using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Xml.XPath;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OsamesMicroOrm;
using OsamesMicroOrm.Configuration;
using OsamesMicroOrm.Utilities;
using SampleDbEntities.Chinook;
using TestOsamesMicroOrm.TestDbEntities;
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
        public void TestGetDbEntityTableNameOk()
        {
            Employee entityEmployee = new Employee();
            Customer entityCustomer = new Customer();
            Invoice entityInvoice = new Invoice();
            InvoiceLine entityInvoiceLineI = new InvoiceLine();
            Track entityTrack = new Track();

            Assert.AreEqual("Employee", MappingTools.GetDbEntityTableName(entityEmployee));
            Assert.AreEqual("Customer", MappingTools.GetDbEntityTableName(entityCustomer));
            Assert.AreEqual("Invoice", MappingTools.GetDbEntityTableName(entityInvoice));
            Assert.AreEqual("InvoiceLine", MappingTools.GetDbEntityTableName(entityInvoiceLineI));
            Assert.AreEqual("Track", MappingTools.GetDbEntityTableName(entityTrack));
        }

        /// <summary>
        /// Case NOK, no mapping attribute on this class.
        /// </summary>
        [ExcludeFromCodeCoverage]
        [TestCategory("Mapping")]
        [Owner("Barbara Post")]
        [TestMethod]
        [ExpectedException(typeof(OOrmHandledException))]
        public void TestGetDbEntityTableNameNOkMissingAttribute()
        {
            TestUnmappedEntity entity = new TestUnmappedEntity();
            try
            {
                string test = MappingTools.GetDbEntityTableName(entity);
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
        public void TestGetDbEntityTableNameNOkEmptyAttribute()
        {
            TestEmptyMappingEntity entity = new TestEmptyMappingEntity();
            try
            {
                string test = MappingTools.GetDbEntityTableName(entity);
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
        public void TestGetDbEntityTableNameWrongValueMappingAttribute()
        {
            TestWrongMappingEntity entity = new TestWrongMappingEntity();
            try
            {
                string test = MappingTools.GetDbEntityTableName(entity);
            }
            catch (OOrmHandledException ex)
            {
                Common.AssertOnHresultAndWriteToConsole(HResultEnum.E_NOMAPPINGKEY, ex);
                throw;
            }
        }

        #region cas de test de GetDbColumnNameFromMappingDictionary()
        /// <summary>
        /// ConfigurationLoader internal dictionary is populated. Test of GetDbColumnNameFromMappingDictionary : case where mapping is found.
        /// </summary>
        [TestMethod]
        [ExcludeFromCodeCoverage]
        [Owner("Barbara Post")]
        [TestCategory("XML")]
        [TestCategory("Configuration")]
        [TestCategory("Mapping")]
        public void TestGetMappingDbColumnName()
        {
            ConfigurationLoader.FillMappingDictionary(new XPathDocument(_mappingFileFullPath).CreateNavigator(), "orm", "http://www.osames.org/osamesorm");
            Assert.IsTrue(ConfigurationLoader.MappingDictionnary.ContainsKey("Customer"), "Expected to find 'Customer' key");

            // Inspect detail for a specific case
            Dictionary<string, string> mappings = ConfigurationLoader.MappingDictionnary["Customer"];
            Assert.IsTrue(mappings.ContainsKey("Email"), "Expected to find 'Email' key");
            Assert.AreEqual("Email", mappings["Email"], "Expected column 'Email' for property 'Email'");
            Assert.AreEqual("Email", MappingTools.GetDbColumnNameFromMappingDictionary("Customer", "Email"));
        }

        /// <summary>
        /// ConfigurationLoader internal dictionary is populated. Test of GetDbColumnNameFromMappingDictionary : case where mapping is found.
        /// </summary>
        [TestMethod]
        [ExcludeFromCodeCoverage]
        [Owner("Barbara Post")]
        [TestCategory("XML")]
        [TestCategory("Configuration")]
        [TestCategory("Mapping")]
        public void TestGeDbColumnName()
        {
            ConfigurationLoader.FillMappingDictionary(new XPathDocument(_mappingFileFullPath).CreateNavigator(), "orm", "http://www.osames.org/osamesorm");
            Assert.AreEqual("CustomerId", MappingTools.GetDbColumnNameFromDbEntity(new Customer().GetType().GetProperty("IdCustomer")));
            Assert.IsNull(MappingTools.GetDbColumnNameFromDbEntity(new Customer().GetType().GetProperty("ThisPropertyDoesntExist")));
            Assert.IsNull(MappingTools.GetDbColumnNameFromDbEntity(new TestUnmappedEntity().GetType().GetProperty("Id")));
        }

        /// <summary>
        /// ConfigurationLoader internal dictionary is populated. Test of GetDbColumnNameFromMappingDictionary : case where mapping is not found (key).
        /// </summary>
        [TestMethod]
        [ExcludeFromCodeCoverage]
        [Owner("Barbara Post")]
        [TestCategory("XML")]
        [TestCategory("Configuration")]
        [TestCategory("Mapping")]
        [ExpectedException(typeof(OOrmHandledException))]
        public void TestGetMappingDbColumnNameWrongKey()
        {
            ConfigurationLoader.FillMappingDictionary(new XPathDocument(_mappingFileFullPath).CreateNavigator(), "orm", "http://www.osames.org/osamesorm");
            Assert.IsFalse(ConfigurationLoader.MappingDictionnary.ContainsKey("foobar"), "Expected not to find 'foobar' key");

            try
            {
                MappingTools.GetDbColumnNameFromMappingDictionary("foobar", "Email");
            }
            catch (OOrmHandledException ex)
            {
                Common.AssertOnHresultAndWriteToConsole(HResultEnum.E_NOMAPPINGKEY, ex);
                throw;
            }

        }

        /// <summary>
        /// ConfigurationLoader internal dictionary is populated. Test of GetDbColumnNameFromMappingDictionary : case where mapping is not found (property name).
        /// </summary>
        [TestMethod]
        [ExcludeFromCodeCoverage]
        [Owner("Barbara Post")]
        [TestCategory("XML")]
        [TestCategory("Configuration")]
        [TestCategory("Mapping")]
        [ExpectedException(typeof(OOrmHandledException))]
        public void TestGetMappingDbColumnNameWrongPropertyName()
        {
            ConfigurationLoader.FillMappingDictionary(new XPathDocument(_mappingFileFullPath).CreateNavigator(), "orm", "http://www.osames.org/osamesorm");
            Assert.IsTrue(ConfigurationLoader.MappingDictionnary.ContainsKey("Customer"), "Expected to find 'customer' key");
            Assert.IsFalse(ConfigurationLoader.MappingDictionnary["Customer"].ContainsKey("foobar"), "Expected not to find 'foobar' key");
            try
            {
                MappingTools.GetDbColumnNameFromMappingDictionary("Customer", "foobar");
            }
            catch (OOrmHandledException ex)
            {
                Common.AssertOnHresultAndWriteToConsole(HResultEnum.E_NOMAPPINGKEYANDPROPERTY, ex);
                throw;
            }
        }

        #endregion

        #region cas de test de GetDbEntityPropertyNameFromMappingDictionary()
        /// <summary>
        /// ConfigurationLoader internal dictionary is populated. Test of GetDbEntityPropertyNameFromMappingDictionary : case where mapping is found.
        /// </summary>
        [TestMethod]
        [ExcludeFromCodeCoverage]
        [Owner("Barbara Post")]
        [TestCategory("XML")]
        [TestCategory("Configuration")]
        [TestCategory("Mapping")]
        public void TestGetDbEntityPropertyName()
        {
            ConfigurationLoader.FillMappingDictionary(new XPathDocument(_mappingFileFullPath).CreateNavigator(), "orm", "http://www.osames.org/osamesorm");
            Assert.IsTrue(ConfigurationLoader.MappingDictionnary.ContainsKey("Customer"), "Expected to find 'Customer' key");

            // Inspect detail for a specific case
            Dictionary<string, string> mappings = ConfigurationLoader.MappingDictionnary["Customer"];
            Assert.IsTrue(mappings.ContainsValue("Email"), "Expected to find 'Email' value");
            Assert.AreEqual("Email", mappings["Email"], "Expected column 'Email' for property 'Email'");
            Assert.AreEqual("Email", MappingTools.GetDbEntityPropertyNameFromMappingDictionary("Customer", "Email"));
        }

        /// <summary>
        /// ConfigurationLoader internal dictionary is populated. Test of GetDbEntityPropertyNameFromMappingDictionary : case where mapping is not found (key).
        /// </summary>
        [TestMethod]
        [ExcludeFromCodeCoverage]
        [Owner("Barbara Post")]
        [TestCategory("XML")]
        [TestCategory("Configuration")]
        [TestCategory("Mapping")]
        [ExpectedException(typeof(OOrmHandledException))]
        public void TestGetDbEntityPropertyNameWrongKey()
        {
            ConfigurationLoader.FillMappingDictionary(new XPathDocument(_mappingFileFullPath).CreateNavigator(), "orm", "http://www.osames.org/osamesorm");
            Assert.IsFalse(ConfigurationLoader.MappingDictionnary.ContainsKey("foobar"), "Expected not to find 'foobar' key");
            try
            {
                MappingTools.GetDbEntityPropertyNameFromMappingDictionary("foobar", "Email");
            }
            catch (OOrmHandledException ex)
            {
                Common.AssertOnHresultAndWriteToConsole(HResultEnum.E_NOMAPPINGKEY, ex);
                throw;
            }
        }

        /// <summary>
        /// ConfigurationLoader internal dictionary is populated. Test of GetDbEntityPropertyNameFromMappingDictionary : case where mapping is not found (column name).
        /// </summary>
        [TestMethod]
        [ExcludeFromCodeCoverage]
        [Owner("Barbara Post")]
        [TestCategory("XML")]
        [TestCategory("Configuration")]
        [TestCategory("Mapping")]
        [ExpectedException(typeof(OOrmHandledException))]
        public void TestGetDbEntityPropertyNameWrongColumnName()
        {
            ConfigurationLoader.FillMappingDictionary(new XPathDocument(_mappingFileFullPath).CreateNavigator(), "orm", "http://www.osames.org/osamesorm");
            Assert.IsTrue(ConfigurationLoader.MappingDictionnary.ContainsKey("Customer"), "Expected to find 'customer' key");
            Assert.IsFalse(ConfigurationLoader.MappingDictionnary["Customer"].ContainsValue("foobar"), "Expected not to find 'foobar' value");
            try
            {
                MappingTools.GetDbEntityPropertyNameFromMappingDictionary("Customer", "foobar");
            }
            catch (OOrmHandledException ex)
            {
                Common.AssertOnHresultAndWriteToConsole(HResultEnum.E_NOMAPPINGKEYANDCOLUMN, ex);
                throw;
            }
        }

        #endregion
    }
}
