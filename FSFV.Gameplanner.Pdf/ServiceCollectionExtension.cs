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

    public static IServiceCollection AddPdfServices(this IServiceCollection serviceCollection)
    {
        return serviceCollection
            .AddTransient<CsvSerializerService>()
            .AddTransient<PdfGenerator>();
    }

    public static IServiceCollection AddPdfServices(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        PdfConfig? pdfConfig = configuration.GetSection(PdfConfigSection).Get<PdfConfig>();
        if (pdfConfig is null)
        {
            throw new InvalidOperationException("Missing required PdfConfig configuration section.");
        }

        serviceCollection
            .AddTransient<CsvSerializerService>()
            .AddSingleton(pdfConfig)
            .AddTransient<PdfGenerator>();

        return serviceCollection;
    }

}