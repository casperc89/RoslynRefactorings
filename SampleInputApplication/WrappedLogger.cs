using log4net;

namespace SampleInputApplication;

public class WrappedLogger
{
    private static Lazy<WrappedLogger> _instance = new(() => new WrappedLogger());

    public static WrappedLogger Instance => _instance.Value;

    private ILog _logger;
    public WrappedLogger()
    {
        _logger = LogManager.GetLogger(typeof(WrappedLogger));
    }

    public void LogInfo(string message)
    {
        _logger.Info(message);
    }
}