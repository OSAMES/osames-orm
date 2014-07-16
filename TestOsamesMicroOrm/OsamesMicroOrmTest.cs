using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestOsamesMicroOrm.Tools;

namespace TestOsamesMicroOrm
{
    /// <summary>
    /// Base class of tests.
    /// Useful for centralizing deployment items declarations
    /// </summary>
    [
        // répertoire copié depuis le projet testé
        DeploymentItem(Common.CST_CONFIG, Common.CST_CONFIG),
        DeploymentItem("Logs", "Logs"),
     ]
    public abstract class OsamesMicroOrmTest
    {
    }
}
