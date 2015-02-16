using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OsamesMicroOrm;
using OsamesMicroOrm.Utilities;
using TestOsamesMicroOrm.Utilities;

namespace TestOsamesMicroOrm.Tools
{
    public static class Common
    {
        #region Common files and directories copied from OsamesMicroOrm at deployment time
        public const string CST_CONFIG = @"Config";
        
        public const string CST_SQL_TEMPLATES_XML = @"Config\sqltemplates.xml";

        public const string CST_SQL_TEMPLATES_XSD = @"Config\xml_schemas\sqlTemplates.xsd";

        public const string CST_SQL_MAPPING_XML = @"Config\omo-mapping.xml";

        public const string CST_SQL_MAPPING_XSD = @"Config\xml_schemas\omo-mapping.xsd";

        #endregion

        #region test files  and directories copied from current project at deployment time

        public const string CST_TEST_CONFIG = @"TestConfig";
        public const string CST_SQL_TEMPLATES_XML_TEST_NODE_CASE = @"TestConfig\sqltemplates-test-node-case.xml";
        public const string CST_SQL_TEMPLATES_XML_TEST_NO_DELETE_SECTION = @"TestConfig\sqltemplates-test-no-delete-section.xml";
        public const string CST_SQL_TEMPLATES_XML_TEST_OTHER_SECTIONS_ORDER = @"TestConfig\sqltemplates-test-other-sections-order.xml";
        public const string CST_SQL_TEMPLATES_XML_TEST_DUPLICATE_SELECT = @"TestConfig\sqltemplates-test-duplicate-select.xml";
        public const string CST_SQL_TEMPLATES_XML_TEST_SAME_SELECT_INSERT = @"TestConfig\sqltemplates-test-same-select-insert.xml";
        public const string CST_SQL_TEMPLATES_TEST_XML_WRONG_URI = @"TestConfig\sqltemplates-test-wrong-uri.xml";

        // Some files are the same ones but with online schema referenced
        public const string CST_SQL_TEMPLATES_XML_TEST_DUPLICATE_SELECT_ONLINE_SCHEMA = @"TestConfig\sqltemplates-test-duplicate-select-online-schema.xml";
        public const string CST_SQL_TEMPLATES_TEST_XML_WRONG_URI_ONLINE_SCHEMA = @"TestConfig\sqltemplates-test-wrong-uri-online-schema.xml";
        public const string CST_SQL_TEMPLATES_XML_ONLINE_SCHEMA = @"TestConfig\sqltemplates-online-schema.xml";
        public const string CST_SQL_TEMPLATES_XML_TEST_OTHER_SECTIONS_ORDER_ONLINE_SCHEMA = @"TestConfig\sqltemplates-test-other-sections-order-online-schema.xml";

        #endregion

        /// <summary>
        /// Méthode utilitaire
        /// </summary>
        /// <param name="expectedCode_">Valeur de l'enum HResultEnum qu'on s'attend à avoir</param>
        /// <param name="ex_">Exception OormHandledException catchée dans le test</param>
        public static void AssertOnHresultAndWriteToConsole(HResultEnum expectedCode_, OOrmHandledException ex_)
        {
            Console.WriteLine(ex_.Message + (ex_.InnerException != null ? ex_.InnerException.Message : ""));

            // convert int code to hexa string
            string hexaCode = "0X" + ex_.HResult.ToString("X").ToUpperInvariant();
            string exceptionCode = "?";
            foreach (string key in OOrmErrorsHandler.HResultCode.Keys)
            {
                var value = OOrmErrorsHandler.HResultCode[key];
                if (value.Key.ToUpperInvariant() == hexaCode)
                {
                    exceptionCode = key;
                    break;
                }
            }
            Assert.AreEqual(expectedCode_.ToString().ToUpperInvariant(), exceptionCode.ToUpperInvariant());
        }
    }
}
