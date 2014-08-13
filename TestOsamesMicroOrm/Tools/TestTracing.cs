using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestOsamesMicroOrm.Tools
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class TestTracing
    {
        [TestMethod]
        [ExcludeFromCodeCoverage]
        [TestCategory("Visual log")]
        public void TestTrace()
        {
            // Initialisation des deux trace source.
            TraceSource verboseTraceSource = new TraceSource("osamesOrmTraceSource");
            TraceSource errorTraceSource = new TraceSource("osamesOrmDetailedTraceSource");

            verboseTraceSource.TraceEvent(TraceEventType.Error, 0, "Erreur de test pour verbose trace source - doit être logguée");
            verboseTraceSource.TraceEvent(TraceEventType.Verbose, 1, "Verbose de test pour verbose trace source - doit être logguée");
            verboseTraceSource.TraceEvent(TraceEventType.Error, 2, "test exception riche pour verbose trace source, ne doit pas tracer la stack " + new Exception("test outer exception", new Exception("test inner exception")));
            
            errorTraceSource.TraceEvent(TraceEventType.Error, 0, "Erreur de test pour error trace source - doit être logguée");
            errorTraceSource.TraceEvent(TraceEventType.Verbose, 1, "Verbose de test pour error trace source - ne doit pas être logguée");
            errorTraceSource.TraceEvent(TraceEventType.Error, 2, "test exception riche pour error trace source, doit tracer la stack " + new Exception("test outer exception", new Exception("test inner exception")));
        }
    }
}
