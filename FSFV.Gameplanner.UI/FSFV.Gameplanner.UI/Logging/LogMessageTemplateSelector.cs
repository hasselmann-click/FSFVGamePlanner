using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FSFV.Gameplanner.UI.Logging;
public class LogMessageTemplateSelector : DataTemplateSelector
{
    public DataTemplate ErrorTemplate { get; set; }
    public DataTemplate WarningTemplate { get; set; }
    public DataTemplate InformationTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        if (item is not UILogMessage logMessage)
        {
            return base.SelectTemplateCore(item, container);
        }

        return logMessage.Level switch
        {
            LogLevel.Error => ErrorTemplate,
            LogLevel.Warning => WarningTemplate,
            _ => InformationTemplate,
        };
    }
}
