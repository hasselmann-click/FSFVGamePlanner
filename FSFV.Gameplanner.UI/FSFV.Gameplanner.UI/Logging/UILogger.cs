using Microsoft.Extensions.Logging;
using System;

namespace FSFV.Gameplanner.UI.Logging;
public class UILogger : ILogger
{
    // This event will be raised whenever a new log message is generated
    public static event Action<UILogMessage> OnMessageLogged;

    public IDisposable BeginScope<TState>(TState state) => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        var message = formatter(state, exception);
        if (exception != null)
        {
            message = $"{message}{Environment.NewLine}Exception: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
        }
        OnMessageLogged?.Invoke(new UILogMessage { Level = logLevel, Message = message });
    }
}
