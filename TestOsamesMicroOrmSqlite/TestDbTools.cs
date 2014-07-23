using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OsamesMicroOrm;
using OsamesMicroOrm.Configuration;
using OsamesMicroOrm.Configuration.Tweak;
using SampleDbEntities.Chinook;
using TestOsamesMicroOrm.Tools;
using TestOsamesMicroOrmSqlite.Tools;

namespace TestOsamesMicroOrmSqlite
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class TestDbTools : OsamesMicroOrmSqliteTest
    {
        private readonly string _incorrectMappingFileFullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CommonSqlite.CST_INCORRECT_MAPPING_CUSTOMER);

       // TODO les différents tests seront à réintégrer ici.

        /// <summary>
        /// Test ORM-37. Configuration incorrecte du mapping : exception attendue.
        /// </summary>
        [TestMethod]
        // Copy test mapping file to configuration standard location
        [DeploymentItem(CommonSqlite.CST_TEST_CONFIG_SQLITE, Common.CST_CONFIG)]
        [TestCategory("Sql")]
        [TestCategory("SqLite")]
        [TestCategory("ReadSql")]
        public void TestExecuteReaderIncorrectMapping()
        {
            // Customization
            Customizer.ConfigurationManagerSetKeyValue("mappingFileName", _incorrectMappingFileFullPath);
            // Reload modified configuration
            ConfigurationLoader.Clear();
            _config = ConfigurationLoader.Instance;

            try
            {
                DbTools.SelectSingleAllColumns<Customer>("BaseReadAll", "Customer", new List<string> {"City"}, new List<object> {"Paris"});
            } catch(Exception ex)
            {
                Console.WriteLine("{0} {1}", ex.Message, ex.StackTrace);
            }
        }
    }
}
