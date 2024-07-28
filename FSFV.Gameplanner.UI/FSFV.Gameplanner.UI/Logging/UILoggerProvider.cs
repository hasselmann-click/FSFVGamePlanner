using Microsoft.Extensions.Logging;

namespace FSFV.Gameplanner.UI.Logging;

public class UILoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new UILogger();

    public void Dispose() { }
}
