using System.Drawing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FSFV.Gameplanner.Service.Serialization;

namespace FSFV.Gameplanner.Pdf;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddPdfServices(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        PdfConfig? pdfConfig = configuration.GetSection("PdfConfig").Get<PdfConfig>();
        if (pdfConfig is null)
        {
            throw new InvalidOperationException("Missing required PdfConfig configuration section.");
        }

        // explicitly convert the dictionary of strings to dictionary of colors
        // since IConfigurationSections doesn't use custom json converters
        var pdfConigLeagueColors = configuration.GetSection("PdfConfig:LeagueColors").Get<Dictionary<string, string>>();

        // TODO colors came from windows, which are not available here.
        // pdfConfig.LeagueColors = pdfConigLeagueColors.ToDictionary(x => x.Key, x => Color.FromHex(x.Value));

        serviceCollection
            .AddTransient<CsvSerializerService>()
            .AddSingleton(pdfConfig)
            .AddTransient<PdfGenerator>();

        return serviceCollection;
    }
}