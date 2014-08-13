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
    }
}
