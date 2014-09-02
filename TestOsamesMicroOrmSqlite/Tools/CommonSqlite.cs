namespace TestOsamesMicroOrmSqlite.Tools
{
    public static class CommonSqlite
    {

        #region test files  and directories copied from current project at deployment time

        /// <summary>
        /// Directory where to find specific configuration items to deploy.
        /// </summary>
        public const string CST_TEST_CONFIG_SQLITE = @"TestConfigSqlite";

        /// <summary>
        /// Relative path for runtime deployed configuration.
        /// </summary>
        public const string CST_INCORRECT_MAPPING_CUSTOMER = @"Config\incorrect-mapping.xml";

        /// <summary>
        /// Relative path for runtime deployed configuration.
        /// </summary>
        public const string CST_POTENTIAL_SQL_INJECTION_MAPPING_CUSTOMER = @"Config\mapping-potential-sql-injection.xml";
   
        
        #endregion

    }
}
