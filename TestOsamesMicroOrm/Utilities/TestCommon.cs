using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OsamesMicroOrm.Utilities;

namespace TestOsamesMicroOrm.Utilities
{
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class TestCommon : OsamesMicroOrmTest
    {
        [ExcludeFromCodeCoverage]
        [TestInitialize]
        public override void Setup()
        {
 	        InitializeDbConnexion();
        }

        [ExcludeFromCodeCoverage]
        [TestMethod]
        public void TestCountPlaceholders()
        {
            // Cas de chaîne correcte : 0 placeholder
            Assert.AreEqual(0, Common.CountPlaceholders("Chaîne"));

            // Cas de chaîne correcte : 1 placeholder
            Assert.AreEqual(1, Common.CountPlaceholders("Chaîne {0}"));

            // Cas de chaîne correcte : 2 placeholders
            Assert.AreEqual(2, Common.CountPlaceholders("{0} Chaîne {1} chaîne"));

            // Cas de chaîne correcte : 3 placeholders
            Assert.AreEqual(3, Common.CountPlaceholders(" {0} Chaîne {1} chaîne {2}"));

            // Cas de chaîne bizarre : placeholders mal formatés, un seul doit être trouvé. C'est OK.
            Assert.AreEqual(1, Common.CountPlaceholders("} Chaîne {0}"));

            // Cas de chaîne bizarre : 3 placeholders mais mal numérotés, le string.Format échouera. Le nombre est correct cependant.
            Assert.AreEqual(3, Common.CountPlaceholders(" {0} Chaîne {1} chaîne {3}"));


        }

        [ExcludeFromCodeCoverage]
        [TestMethod]
        public void TestCheckPlaceholdersAndParametersNumbers()
        {
            // Cas de chaîne correcte : 0 placeholder
            Assert.IsTrue(Common.CheckPlaceholdersAndParametersNumbers("Chaîne", new List<string>()));

            // Cas de chaîne correcte : 1 placeholder
            Assert.IsTrue(Common.CheckPlaceholdersAndParametersNumbers("Chaîne {0}", new List<string> { "a" }));

            // Cas de chaîne correcte : 2 placeholders
            Assert.IsTrue(Common.CheckPlaceholdersAndParametersNumbers("{0} Chaîne {1} chaîne", new List<string> { "a", "b" }));

            // Cas de chaîne correcte : 3 placeholders
            Assert.IsTrue(Common.CheckPlaceholdersAndParametersNumbers(" {0} Chaîne {1} chaîne {2}", new List<string> { "a", "b", "c" }));

            // Cas de chaîne incorrecte : 0 placeholder dans la chaîne, un dans la liste
            Assert.IsFalse(Common.CheckPlaceholdersAndParametersNumbers("Chaîne", new List<string> { "a" }));

            // Cas de chaîne incorrecte : 1 placeholder dans la chaîne, 0 dans la liste
            Assert.IsFalse(Common.CheckPlaceholdersAndParametersNumbers("Chaîne {0}", new List<string>()));

            // Cas de chaîne bizarre : placeholders mal formatés, un seul doit être trouvé. C'est OK.
            Assert.IsTrue(Common.CheckPlaceholdersAndParametersNumbers("} Chaîne {0}", new List<string> { "a" }));

            // Cas de chaîne bizarre : 3 placeholders mais mal numérotés, le string.Format échouera. Le nombre est correct cependant.
            Assert.IsTrue(Common.CheckPlaceholdersAndParametersNumbers(" {0} Chaîne {1} chaîne {3}", new List<string> { "a", "b", "c" }));


        }
    }
}
