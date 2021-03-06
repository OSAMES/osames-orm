﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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

            Assert.AreEqual("[Employee]", MappingTools.GetTableName(entityEmployee));
            Assert.AreEqual("[Customer]", MappingTools.GetTableName(entityCustomer));
            Assert.AreEqual("[Invoice]", MappingTools.GetTableName(entityInvoice));
            Assert.AreEqual("[InvoiceLine]", MappingTools.GetTableName(entityInvoiceLineI));
            Assert.AreEqual("[Track]", MappingTools.GetTableName(entityTrack));
        }

        /// <summary>
        /// Case OK.
        /// </summary>
        [ExcludeFromCodeCoverage]
        [TestCategory("Mapping")]
        [Owner("Barbara Post")]
        [TestMethod]
        public void TestGetDbEntityUnprotectedTableNameOk()
        {
            Employee entityEmployee = new Employee();
            Assert.AreEqual("Employee", MappingTools.GetUnprotectedTableName(entityEmployee));
           
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
                string test = MappingTools.GetTableName(entity);
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
                string test = MappingTools.GetTableName(entity);
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
                string test = MappingTools.GetTableName(entity);
            }
            catch (OOrmHandledException ex)
            {
                Common.AssertOnHresultAndWriteToConsole(HResultEnum.E_NOMAPPINGKEY, ex);
                throw;
            }
        }

        /// <summary>
        /// ConfigurationLoader internal dictionary is populated. Test of GetMappingDefinitionsFor.
        /// Obtention d'un dictionnaire avec [nomDeTable].[nomDeColonne].
        /// </summary>
        [TestMethod]
        [ExcludeFromCodeCoverage]
        [Owner("Barbara Post")]
        [TestCategory("XML")]
        [TestCategory("Configuration")]
        [TestCategory("Mapping")]
        public void TestGetMappingDefinitionsFor()
        {
            ConfigurationLoader.FillMappingDictionary(new XPathDocument(_mappingFileFullPath).CreateNavigator(), "orm", "http://www.osames.org/osamesorm");
            Dictionary<string, string> dictionary = MappingTools.GetMappingDefinitionsFor(new Customer());

            Assert.IsNotNull(dictionary);
            Assert.AreNotEqual(0, dictionary.Keys);
            Assert.IsTrue(dictionary.ContainsKey("IdCustomer"));
            Assert.AreEqual("[Customer].[CustomerId]", dictionary["IdCustomer"]);
            Assert.AreEqual("[Customer].[FirstName]", dictionary["FirstName"]);
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
        public void TestGetDbColumnName()
        {
            ConfigurationLoader.FillMappingDictionary(new XPathDocument(_mappingFileFullPath).CreateNavigator(), "orm", "http://www.osames.org/osamesorm");
            // mesure du temps d'accès
            Stopwatch watch = new Stopwatch();
            watch.Start();
            Assert.AreEqual("[CustomerId]", MappingTools.GetDbColumnName(new Customer(), "IdCustomer"));
            watch.Stop();
            Console.WriteLine("Première détermination de la valeur : " + watch.ElapsedMilliseconds + " ms");
            watch.Reset();
            watch.Start();
            Assert.AreEqual("[CustomerId]", MappingTools.GetDbColumnName(new Customer(), "IdCustomer"));
            watch.Stop();
            Console.WriteLine("Deuxième détermination de la valeur : " + watch.ElapsedMilliseconds + " ms");
        }

        /// <summary>
        /// ConfigurationLoader internal dictionary is populated. Test of GetDbColumnNameFromMappingDictionary.
        /// La propriété demandée n'existe pas sur l'objet C#.
        /// </summary>
        [TestMethod]
        [ExcludeFromCodeCoverage]
        [Owner("Barbara Post")]
        [TestCategory("XML")]
        [TestCategory("Configuration")]
        [TestCategory("Mapping")]
        [TestCategory("Parameter NOK")]
        [ExpectedException(typeof(OOrmHandledException))]
        public void TestGetDbColumnNamePropertyDoesntExist()
        {
            try
            {
                ConfigurationLoader.FillMappingDictionary(new XPathDocument(_mappingFileFullPath).CreateNavigator(), "orm", "http://www.osames.org/osamesorm");
                Assert.IsNull(MappingTools.GetDbColumnName(new Customer(), "ThisPropertyDoesntExist"));
            }
            catch (OOrmHandledException ex)
            {
                Common.AssertOnHresultAndWriteToConsole(HResultEnum.E_TYPEDOESNTDEFINEPROPERTY, ex);
                throw;
            }
        }

        /// <summary>
        /// ConfigurationLoader internal dictionary is populated. Test of GetDbColumnNameFromMappingDictionary.
        /// L'objet C# ne comporte pas de DatabaseMappingAttribute.
        /// </summary>
        [TestMethod]
        [ExcludeFromCodeCoverage]
        [Owner("Barbara Post")]
        [TestCategory("XML")]
        [TestCategory("Configuration")]
        [TestCategory("Mapping")]
        [TestCategory("Parameter NOK")]
        [ExpectedException(typeof(OOrmHandledException))]
        public void TestGetDbColumnNameNoDatabaseAttributeDecoration()
        {
            try
            {
                ConfigurationLoader.FillMappingDictionary(new XPathDocument(_mappingFileFullPath).CreateNavigator(), "orm", "http://www.osames.org/osamesorm");
                Assert.IsNull(MappingTools.GetDbColumnName(new TestUnmappedEntity(), "Id"));
            }
            catch (OOrmHandledException ex)
            {
                Common.AssertOnHresultAndWriteToConsole(HResultEnum.E_TYPENOTDEFINEDBMAPPINGATTRIBUTE, ex);
                throw;
            }


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
        public void TestGetDbTableAndColumnName()
        {
            ConfigurationLoader.FillMappingDictionary(new XPathDocument(_mappingFileFullPath).CreateNavigator(), "orm", "http://www.osames.org/osamesorm");
            // mesure du temps d'accès
            Stopwatch watch = new Stopwatch();
            watch.Start();
            Assert.AreEqual("[Customer].[CustomerId]", MappingTools.GetDbTableAndColumnName(new Customer(), "IdCustomer"));
            watch.Stop();
            Console.WriteLine("Première détermination de la valeur : " + watch.ElapsedMilliseconds + " ms");
            watch.Reset();
            watch.Start();
            Assert.AreEqual("[Customer].[CustomerId]", MappingTools.GetDbTableAndColumnName(new Customer(), "IdCustomer"));
            watch.Stop();
            Console.WriteLine("Deuxième détermination de la valeur : " + watch.ElapsedMilliseconds + " ms");
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

    }
}
