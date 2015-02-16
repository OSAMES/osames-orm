using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OsamesMicroOrm;
using OsamesMicroOrm.Configuration;
using OsamesMicroOrm.Utilities;
using Common = TestOsamesMicroOrm.Tools.Common;

namespace TestOsamesMicroOrm
{
    [TestClass]
    [
        // fichiers spécifiques au projet de test pour ces tests
        DeploymentItem(Common.CST_SQL_TEMPLATES_XML_TEST_DUPLICATE_SELECT, Common.CST_TEST_CONFIG),
        DeploymentItem(Common.CST_SQL_TEMPLATES_XML_TEST_NO_DELETE_SECTION, Common.CST_TEST_CONFIG),
        DeploymentItem(Common.CST_SQL_TEMPLATES_XML_TEST_NODE_CASE, Common.CST_TEST_CONFIG),
        DeploymentItem(Common.CST_SQL_TEMPLATES_XML_TEST_OTHER_SECTIONS_ORDER, Common.CST_TEST_CONFIG),
        DeploymentItem(Common.CST_SQL_TEMPLATES_XML_TEST_SAME_SELECT_INSERT, Common.CST_TEST_CONFIG),
        DeploymentItem(Common.CST_SQL_TEMPLATES_TEST_XML_WRONG_URI, Common.CST_TEST_CONFIG),
        DeploymentItem(Common.CST_SQL_TEMPLATES_XML_ONLINE_SCHEMA, Common.CST_TEST_CONFIG),
        DeploymentItem(Common.CST_SQL_TEMPLATES_XML_TEST_DUPLICATE_SELECT_ONLINE_SCHEMA, Common.CST_TEST_CONFIG),
        DeploymentItem(Common.CST_SQL_TEMPLATES_TEST_XML_WRONG_URI_ONLINE_SCHEMA, Common.CST_TEST_CONFIG),
        DeploymentItem(Common.CST_SQL_TEMPLATES_XML_TEST_OTHER_SECTIONS_ORDER_ONLINE_SCHEMA, Common.CST_TEST_CONFIG)
    ]
    [ExcludeFromCodeCoverage]
    public class TestXmlValidator : OsamesMicroOrmTest
    {
        private readonly string _mappingFileFullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Common.CST_SQL_MAPPING_XML);
        private readonly string _templatesFileFullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Common.CST_SQL_TEMPLATES_XML);
        private readonly string _mappingXsdFullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Common.CST_SQL_MAPPING_XSD);
        private readonly string _templatesXsdFullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Common.CST_SQL_TEMPLATES_XSD);

        private readonly string _templatesNoDeleteSectionXml = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Common.CST_SQL_TEMPLATES_XML_TEST_NO_DELETE_SECTION);
        private readonly string _templatesDuplicateSelectXml = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Common.CST_SQL_TEMPLATES_XML_TEST_DUPLICATE_SELECT);
        private readonly string _templatesOtherOrderXml = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Common.CST_SQL_TEMPLATES_XML_TEST_OTHER_SECTIONS_ORDER);
        private readonly string _templatesSameSelectInsertXml = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Common.CST_SQL_TEMPLATES_XML_TEST_SAME_SELECT_INSERT);
        private readonly string _templatesWrongUriXml = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Common.CST_SQL_TEMPLATES_TEST_XML_WRONG_URI);

        private readonly string _templatesFileFullPathOnlineSchema = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Common.CST_SQL_TEMPLATES_XML_ONLINE_SCHEMA);
        private readonly string _templatesDuplicateSelectXmlOnlineSchema = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Common.CST_SQL_TEMPLATES_XML_TEST_DUPLICATE_SELECT_ONLINE_SCHEMA);
        private readonly string _templatesWrongUriXmlOnlineSchema = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Common.CST_SQL_TEMPLATES_TEST_XML_WRONG_URI_ONLINE_SCHEMA);
        private readonly string _templatesOtherOrderXmlOnlineSchema = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Common.CST_SQL_TEMPLATES_XML_TEST_OTHER_SECTIONS_ORDER_ONLINE_SCHEMA);

        private XmlValidator _xmlValidatorWithSchemasAndNamespaces, _xmlValidatorWithSchemaButNullNamespaces, _xmlValidatorWithoutSchemasOrNamespaces;

        /// <summary>
        /// Initialization called once per test.
        /// </summary>
        [TestInitialize]
        [Owner("Benjamin Nolmans")]
        [ExcludeFromCodeCoverage]
        public override void Setup()
        {
            // Obligatoire car Resharper ne comprend pas qu'il faut initilaliser la classe mère.
            var tempo = ConfigurationLoader.Instance;
            // Pas de DB déployée donc ne pas appeler InitializeDbConnexion();
            // On passe tous les namespaces et fichiers de schémas locaux à utiliser
            _xmlValidatorWithSchemasAndNamespaces = new XmlValidator(new[] { "http://www.osames.org/osamesorm", "http://www.osames.org/osamesorm" }, new[] { _mappingXsdFullPath, _templatesXsdFullPath });

            // On passe des namespaces à null et tous les fichiers de schémas locaux à utiliser
            _xmlValidatorWithSchemaButNullNamespaces = new XmlValidator(new string[] { null, null }, new[] { _mappingXsdFullPath, _templatesXsdFullPath });

            // Pas d'initialisation, la résolution du schéma utilisera le schéma online défini dans le XML
            _xmlValidatorWithoutSchemasOrNamespaces = new XmlValidator();
        }

        /// <summary>
        /// Production similar XML test file. Should be accepted by xml schema validation.
        /// </summary>
        [TestMethod]
        [Owner("Barbara Post")]
        [ExcludeFromCodeCoverage]
        [TestCategory("XML")]
        [TestCategory("Local XML schema")]
        public void TestValidateOkMapping()
        {
            XmlValidator validator = _xmlValidatorWithSchemasAndNamespaces;
            try
            {
                validator.ValidateXml(_mappingFileFullPath);
            }
            finally
            {
                foreach (string error in validator.Errors)
                    Console.WriteLine("Error: {0}", error);
                foreach (string warning in validator.Warnings)
                    Console.WriteLine("Warning: {0}", warning);
                Assert.AreEqual(0, validator.Errors.Count, "Expected no validation errors");
                Assert.AreEqual(0, validator.Warnings.Count, "Expected no validation warnings");
            }

        }
        /// <summary>
        /// Production similar XML test file. Should be accepted by xml schema validation.
        /// </summary>
        [TestMethod]
        [Owner("Barbara Post")]
        [ExcludeFromCodeCoverage]
        [TestCategory("XML")]
        [TestCategory("Local XML schema")]
        public void TestValidateOkTemplates()
        {
            XmlValidator validator = _xmlValidatorWithSchemasAndNamespaces;
            try
            {
               validator.ValidateXml(_templatesFileFullPath);
            }
            finally
            {
                foreach (string error in validator.Errors)
                    Console.WriteLine("Error: {0}", error);
                foreach (string warning in validator.Warnings)
                    Console.WriteLine("Warning: {0}", warning);
                Assert.AreEqual(0, validator.Errors.Count, "Expected no validation errors");
                Assert.AreEqual(0, validator.Warnings.Count, "Expected no validation warnings");
            }
        }

        /// <summary>
        /// Production similar XML test file. Should be accepted by xml schema validation but we have incorrectly set up the XML validator: it has been fed the
        /// namespace and schema, thus throws an exception about global element being already declared, because resolving online schema also feeds the validator.
        /// </summary>
        [TestMethod]
        [Owner("Barbara Post")]
        [ExcludeFromCodeCoverage]
        [TestCategory("XML")]
        [TestCategory("Online XML schema")]
        [ExpectedException(typeof(OOrmHandledException))]
        public void TestValidateOkTemplatesOnlineSchemaValidatorWithNamespace()
        {
            XmlValidator validator = _xmlValidatorWithSchemasAndNamespaces;
            try
            {
                validator.ValidateXml(_templatesFileFullPathOnlineSchema);
            }
            finally
            {
                foreach (string error in validator.Errors)
                    Console.WriteLine("Error: {0}", error);
                foreach (string warning in validator.Warnings)
                    Console.WriteLine("Warning: {0}", warning);
                Assert.AreEqual(1, validator.Errors.Count, "Expected no validation errors");
                Assert.AreEqual(0, validator.Warnings.Count, "Expected no validation warnings");

                bool bEnglishMessageEqual = "The global element 'http://www.osames.org/osamesorm:QueryList' has already been declared." == validator.Errors[0];
                bool bFrenchMessageEqual = "L'élément global 'http://www.osames.org/osamesorm:QueryList' a déjà été déclaré." == validator.Errors[0];
                Assert.IsTrue(bEnglishMessageEqual || bFrenchMessageEqual, string.Format("Message d'erreur obtenu égal ni à celui en anglais ni à celui en français correspondant au contexte (élément global déjà déclaré). Obtenu:\n\n {0}", validator.Errors[0]));

            }
        }

        /// <summary>
        /// Production similar XML test file. Should be accepted by xml schema validation.
        /// </summary>
        [TestMethod]
        [Owner("Barbara Post")]
        [ExcludeFromCodeCoverage]
        [TestCategory("XML")]
        [TestCategory("Online XML schema")]
        public void TestValidateOkTemplatesOnlineSchemaValidatorWithoutSchemaOrNamespace()
        {
            XmlValidator validator = _xmlValidatorWithoutSchemasOrNamespaces;
            try
            {
                validator.ValidateXml(_templatesFileFullPathOnlineSchema);
            }
            finally
            {
                foreach (string error in validator.Errors)
                    Console.WriteLine("Error: {0}", error);
                foreach (string warning in validator.Warnings)
                    Console.WriteLine("Warning: {0}", warning);
                Assert.AreEqual(0, validator.Errors.Count, "Expected no validation errors");
                Assert.AreEqual(0, validator.Warnings.Count, "Expected no validation warnings");
            }
        }

        /// <summary>
        /// XML test file : no delete section. Should be accepted by xml schema validation.
        /// </summary>
        [TestMethod]
        [Owner("Barbara Post")]
        [ExcludeFromCodeCoverage]
        [TestCategory("XML")]
        [TestCategory("Local XML schema")]
        public void TestValidateTemplatesNoDeleteSection()
        {
            XmlValidator validator = _xmlValidatorWithSchemasAndNamespaces;
            try
            {
                validator.ValidateXml(_templatesNoDeleteSectionXml);
            }
            finally
            {
                foreach (string error in validator.Errors)
                    Console.WriteLine("Error: {0}", error);
                foreach (string warning in validator.Warnings)
                    Console.WriteLine("Warning: {0}", warning);
                Assert.AreEqual(0, validator.Errors.Count, "Expected no validation errors");
                Assert.AreEqual(0, validator.Warnings.Count, "Expected no validation warnings");
            }
        }

        /// <summary>
        /// XML test file : two select items have same name. Should be rejected by xml schema validation.
        /// TEST IGNORED: at this stage the "xs:unique" constraints isn't kept into account by our XML validation code, so we just gave up on this.
        /// </summary>
        [TestMethod]
        [Owner("Barbara Post")]
        [ExcludeFromCodeCoverage]
        [ExpectedException(typeof(OOrmHandledException))]
        [TestCategory("XML")]
        [TestCategory("Local XML schema")]
        [Ignore]
        public void TestValidateTemplatesDuplicateSelect()
        {
            XmlValidator validator = _xmlValidatorWithSchemasAndNamespaces;
            try
            {
                validator.ValidateXml(_templatesDuplicateSelectXml);
            }
            catch (OOrmHandledException ex)
            {
               Common.AssertOnHresultAndWriteToConsole(HResultEnum.E_XMLVALIDATIONERRORS, ex);
                throw;
            }
            finally
            {
                foreach (string error in validator.Errors)
                    Console.WriteLine("Error: {0}", error);
                foreach (string warning in validator.Warnings)
                    Console.WriteLine("Warning: {0}", warning);
                Assert.AreEqual(1, validator.Errors.Count, "Expected 1 validation errors");
                Assert.AreEqual(0, validator.Warnings.Count, "Expected no validation warnings");
            }
        }

        /// <summary>
        /// XML test file : two select items have same name. Should be rejected by xml schema validation.
        /// TEST IGNORED: at this stage the "xs:unique" constraints isn't kept into account by our XML validation code, so we just gave up on this.
        /// </summary>
        [TestMethod]
        [Owner("Barbara Post")]
        [ExcludeFromCodeCoverage]
        [ExpectedException(typeof(OOrmHandledException))]
        [TestCategory("XML")]
        [TestCategory("Online XML schema")]
        [Ignore]
        public void TestValidateTemplatesDuplicateSelectOnlineSchema()
        {
            XmlValidator validator = _xmlValidatorWithoutSchemasOrNamespaces;
            try
            {
                validator.ValidateXml(_templatesDuplicateSelectXmlOnlineSchema);
            }
            catch (OOrmHandledException ex)
            {
                Common.AssertOnHresultAndWriteToConsole(HResultEnum.E_XMLVALIDATIONERRORS, ex);
                throw;
            }
            finally
            {
                foreach (string error in validator.Errors)
                    Console.WriteLine("Error: {0}", error);
                foreach (string warning in validator.Warnings)
                    Console.WriteLine("Warning: {0}", warning);
                Assert.AreEqual(1, validator.Errors.Count, "Expected 1 validation errors");
                Assert.AreEqual(0, validator.Warnings.Count, "Expected no validation warnings");
            }
        }

        /// <summary>
        /// XML test file : interverted sections order. Should be rejected by xml schema validation.
        /// </summary>
        [TestMethod]
        [Owner("Barbara Post")]
        [ExcludeFromCodeCoverage]
        [ExpectedException(typeof(OOrmHandledException))]
        [TestCategory("XML")]
        [TestCategory("Local XML schema")]
        public void TestValidateTemplatesOtherOrder()
        {
            XmlValidator validator = _xmlValidatorWithSchemasAndNamespaces;
            try
            {
                validator.ValidateXml(_templatesOtherOrderXml);
            }
            catch (OOrmHandledException ex)
            {
                Common.AssertOnHresultAndWriteToConsole(HResultEnum.E_XMLVALIDATIONERRORS, ex);
                throw;
            }
            finally
            {
                foreach (string error in validator.Errors)
                    Console.WriteLine("Error: {0}", error);
                foreach (string warning in validator.Warnings)
                    Console.WriteLine("Warning: {0}", warning);
                Assert.AreEqual(1, validator.Errors.Count, "Expected 1 validation errors");
                bool bEnglishMessageEqual = "The element 'QueryList' in namespace 'http://www.osames.org/osamesorm' has invalid child element 'Inserts' in namespace 'http://www.osames.org/osamesorm'. List of possible elements expected: 'Selects, Deletes, ProviderSpecific' in namespace 'http://www.osames.org/osamesorm'." == validator.Errors[0];
                bool bFrenchMessageEqual = "L'élément 'QueryList' dans l'espace de noms 'http://www.osames.org/osamesorm' a un élément enfant non valide 'Inserts' dans l'espace de noms 'http://www.osames.org/osamesorm'. Liste d'éléments possibles attendue : 'Selects, Deletes, ProviderSpecific' dans l'espace de noms 'http://www.osames.org/osamesorm'." == validator.Errors[0];
                Assert.IsTrue(bEnglishMessageEqual || bFrenchMessageEqual, string.Format("Message d'erreur obtenu égal ni à celui en anglais ni à celui en français correspondant au contexte (élément enfant non valide). Obtenu {0}", validator.Errors[0]));

                Assert.AreEqual(0, validator.Warnings.Count, "Expected no validation warnings");
            }
        }

        /// <summary>
        /// XML test file : interverted sections order. Should be rejected by xml schema validation.
        /// </summary>
        [TestMethod]
        [Owner("Barbara Post")]
        [ExcludeFromCodeCoverage]
        [ExpectedException(typeof(OOrmHandledException))]
        [TestCategory("XML")]
        [TestCategory("Online XML schema")]
        public void TestValidateTemplatesOtherOrderOnlineSchema()
        {
            XmlValidator validator = _xmlValidatorWithoutSchemasOrNamespaces;
            try
            {
                validator.ValidateXml(_templatesOtherOrderXmlOnlineSchema);
            }
            catch (OOrmHandledException ex)
            {
                Common.AssertOnHresultAndWriteToConsole(HResultEnum.E_XMLVALIDATIONERRORS, ex);
                throw;
            }
            finally
            {
                foreach (string error in validator.Errors)
                    Console.WriteLine("Error: {0}", error);
                foreach (string warning in validator.Warnings)
                    Console.WriteLine("Warning: {0}", warning);
                Assert.AreEqual(1, validator.Errors.Count, "Expected 1 validation errors");
                bool bEnglishMessageEqual = "The element 'QueryList' in namespace 'http://www.osames.org/osamesorm' has invalid child element 'Inserts' in namespace 'http://www.osames.org/osamesorm'. List of possible elements expected: 'Selects, Deletes, ProviderSpecific' in namespace 'http://www.osames.org/osamesorm'." == validator.Errors[0];
                bool bFrenchMessageEqual = "L'élément 'QueryList' dans l'espace de noms 'http://www.osames.org/osamesorm' a un élément enfant non valide 'Inserts' dans l'espace de noms 'http://www.osames.org/osamesorm'. Liste d'éléments possibles attendue : 'Selects, Deletes, ProviderSpecific' dans l'espace de noms 'http://www.osames.org/osamesorm'." == validator.Errors[0];
                Assert.IsTrue(bEnglishMessageEqual || bFrenchMessageEqual, string.Format("Message d'erreur obtenu égal ni à celui en anglais ni à celui en français correspondant au contexte (élément enfant non valide). Obtenu {0}", validator.Errors[0]));

                Assert.AreEqual(0, validator.Warnings.Count, "Expected no validation warnings");
            }
        }

        /// <summary>
        /// XML test file : interverted sections order, twice. Should be rejected by xml schema validation.
        /// </summary>
        [TestMethod]
        [Owner("Barbara Post")]
        [ExcludeFromCodeCoverage]
        [ExpectedException(typeof(OOrmHandledException))]
        [TestCategory("XML")]
        [TestCategory("Local XML schema")]
        public void TestValidateTwoErroneousXmlAtOnce()
        {
            XmlValidator validator = _xmlValidatorWithSchemasAndNamespaces;
            try
            {
                validator.ValidateXml(new[] { _templatesOtherOrderXml, _templatesOtherOrderXml });
            }
            catch (OOrmHandledException ex)
            {
                Common.AssertOnHresultAndWriteToConsole(HResultEnum.E_XMLVALIDATIONERRORS, ex);
                throw;
            }
            finally
            {
                foreach (string error in validator.Errors)
                    Console.WriteLine("Error: {0}", error);
                foreach (string warning in validator.Warnings)
                    Console.WriteLine("Warning: {0}", warning);
                Assert.AreEqual(2, validator.Errors.Count, "Expected 2 validation errors");
                Assert.AreEqual(0, validator.Warnings.Count, "Expected no validation warnings");
            }
        }
        /// <summary>
        /// XML test file : a select and an insert have same name. Should be accepted by xml schema validation.
        /// </summary>
        [TestMethod]
        [Owner("Barbara Post")]
        [ExcludeFromCodeCoverage]
        [TestCategory("XML")]
        [TestCategory("Local XML schema")]
        public void TestValidateTemplatesSameSelectInsert()
        {
            XmlValidator validator = _xmlValidatorWithSchemasAndNamespaces;
            try
            {
                validator.ValidateXml(_templatesSameSelectInsertXml);
            }
            finally
            {
                foreach (string error in validator.Errors)
                    Console.WriteLine("Error: {0}", error);
                foreach (string warning in validator.Warnings)
                    Console.WriteLine("Warning: {0}", warning);
                Assert.AreEqual(0, validator.Errors.Count, "Expected no validation errors");
                Assert.AreEqual(0, validator.Warnings.Count, "Expected no validation warnings");
            }
        }
        [TestMethod]
        [Owner("Barbara Post")]
        [ExcludeFromCodeCoverage]
        [TestCategory("XML")]
        public void TestXmlToolsGetRootTagInfos()
        {
            string xmlPrefix, xmlNamespace;
            /*XPathNavigator navigator = */
            XmlTools.GetRootTagInfos(_mappingFileFullPath, out xmlPrefix, out xmlNamespace);

            Console.WriteLine("Préfixe : " + xmlPrefix);
            Console.WriteLine("Namespace : " + xmlNamespace);
        }

        /// <summary>
        /// Production similar XML test file. Should be rejected by xml schema validation because .xsd path is wrong.
        /// </summary>
        [TestMethod]
        [Owner("Barbara Post")]
        [ExcludeFromCodeCoverage]
        [TestCategory("XML")]
        [TestCategory("Local XML schema")]
        [ExpectedException(typeof(OOrmHandledException))]
        public void TestValidateTemplatesWrongSchemaUri()
        {
            XmlValidator validator = _xmlValidatorWithSchemasAndNamespaces;
            try
            {
                validator.ValidateXml(_templatesWrongUriXml);
            }
            catch (OOrmHandledException ex)
            {
                Common.AssertOnHresultAndWriteToConsole(HResultEnum.E_XMLVALIDATIONERRORS, ex);
                throw;
            }
            finally
            {
                foreach (string error in validator.Errors)
                    Console.WriteLine("Error: {0}", error);
                foreach (string warning in validator.Warnings)
                    Console.WriteLine("Warning: {0}", warning);
                Assert.AreEqual(0, validator.Errors.Count, "Expected no validation errors");
                Assert.AreEqual(1, validator.Warnings.Count, "Expected validation warnings");
            }
        }

        /// <summary>
        /// Production similar XML test file. Should be rejected by xml schema validation because .xsd path is wrong.
        /// </summary>
        [TestMethod]
        [Owner("Barbara Post")]
        [ExcludeFromCodeCoverage]
        [TestCategory("XML")]
        [TestCategory("Online XML schema")]
        [ExpectedException(typeof(OOrmHandledException))]
        public void TestValidateTemplatesWrongSchemaUriOnlineSchema()
        {
            XmlValidator validator = _xmlValidatorWithoutSchemasOrNamespaces;
            try
            {
                validator.ValidateXml(_templatesWrongUriXmlOnlineSchema);
            }
            catch (OOrmHandledException ex)
            {
                Common.AssertOnHresultAndWriteToConsole(HResultEnum.E_XMLVALIDATIONERRORS, ex);
                throw;
            }
            finally
            {
                foreach (string error in validator.Errors)
                    Console.WriteLine("Error: {0}", error);
                foreach (string warning in validator.Warnings)
                    Console.WriteLine("Warning: {0}", warning);
                Assert.AreEqual(0, validator.Errors.Count, "Expected no validation errors");
                Assert.IsTrue(validator.Warnings.Count > 1, "Expected many validation warnings");
            }
        }
    }
}
