namespace SampleInputApplication;

public class ClassWithoutLogger
{
    public bool AlwaysFalse()
    {
        WrappedLogger.Instance.LogInfo("Always false");
        return false;
    }
}