using SampleInputApplication;


var withLogger = new ClassWithLogger().AlwaysTrue();
var withoutLogger = new ClassWithoutLogger().AlwaysFalse();

WrappedLogger.Instance.LogInfo("Bla");

