using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FSFV.Gameplanner.Service.Serialization;
using QuestPDF.Infrastructure;

namespace FSFV.Gameplanner.Pdf;

public static class ServiceCollectionExtension
{
    private const string PdfConfigSection = "PdfConfig";
    private const string LeagueColorsSection = "PdfConfig:LeagueColors";
    private const string DefaultLeagueColorKey = "Default";

    public static IServiceCollection AddPdfServices(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        PdfConfig? pdfConfig = configuration.GetSection(PdfConfigSection).Get<PdfConfig>();
        if (pdfConfig is null)
        {
            throw new InvalidOperationException("Missing required PdfConfig configuration section.");
        }

        pdfConfig.LeagueColors = ReadLeagueColors(configuration);

        serviceCollection
            .AddTransient<CsvSerializerService>()
            .AddSingleton(pdfConfig)
            .AddTransient<PdfGenerator>();

        return serviceCollection;
    }

    private static Dictionary<string, Color> ReadLeagueColors(IConfiguration configuration)
    {
        var rawColors = configuration.GetSection(LeagueColorsSection).Get<Dictionary<string, string>>()
            ?? new Dictionary<string, string>();

        var parsedColors = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase);
        foreach (var (league, hexColor) in rawColors)
        {
            if (string.IsNullOrWhiteSpace(league) || string.IsNullOrWhiteSpace(hexColor))
            {
                continue;
            }

            try
            {
                parsedColors[league] = Color.FromHex(hexColor);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Invalid hex color '{hexColor}' for league '{league}' in PdfConfig.", ex);
            }
        }

        if (!parsedColors.ContainsKey(DefaultLeagueColorKey))
        {
            parsedColors[DefaultLeagueColorKey] = Color.FromHex("#ECA64F");
        }

        return parsedColors;
    }
}