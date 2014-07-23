using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestOsamesMicroOrmSqlite
{
    /// <summary>
    /// IMPORTANT : on doit utiliser la transaction définie par la classe mère des tests unitaires.
    /// Celle-ci porte la connexion à utiliser.
    /// Le test unitaire, en fin (test cleanup), fera un rollback sur la transaction.
    /// </summary>
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class SqliteTestDbManager : OsamesMicroOrmSqliteTest
    {
 
        [TestMethod]
        [ExcludeFromCodeCoverage]
        [Owner("Barbara Post")]
        [TestCategory("Sql")]
        [TestCategory("SqLite")]
        public void TestGetProvider()
        {
            DataTable providers = DbProviderFactories.GetFactoryClasses();
            ShowTable(providers);

            DbProviderFactory provider = DbProviderFactories.GetFactory("System.Data.SQLite");
            Assert.IsNotNull(provider);
        }

        // TODO les différents tests seront à réintégrer ici.
      
        /// <summary>
         /// From http://msdn.microsoft.com/en-us/library/system.data.datatable(v=vs.110).aspx
        /// </summary>
        /// <param name="table_"></param>
         private static void ShowTable(DataTable table_)
         {
             foreach (DataColumn col in table_.Columns)
             {
                 Console.Write("{0,-14}", col.ColumnName);
             }
             Console.WriteLine();

             foreach (DataRow row in table_.Rows)
             {
                 foreach (DataColumn col in table_.Columns)
                 {
                     if (col.DataType == typeof(DateTime))
                         Console.Write("{0,-14:d}", row[col]);
                     else if (col.DataType == typeof(Decimal))
                         Console.Write("{0,-14:C}", row[col]);
                     else
                         Console.Write("{0,-14}", row[col]);

                     Console.Write("   |   ");
                 }
                 Console.WriteLine();
             }
             Console.WriteLine();
         }
    }
}
