using Microsoft.Extensions.Logging;

namespace FSFV.Gameplanner.UI.Logging;
public class UILogMessage
{
    public string Message { get; set; }
    public LogLevel Level { get; set; }
}