using System.Drawing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FSFV.Gameplanner.Pdf;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddPdfServices(this IServiceCollection serviceCollection, IConfiguration configuration)
    {

        throw new NotImplementedException("We need a solution for generating PDFs on a linux server first");

        var pdfConfig = configuration.GetSection("PdfConfig").Get<PdfConfig>();
        // explicitly convert the dictionary of strings to dictionary of colors
        // since IConfigurationSections doesn't use custom json converters
        var pdfConigLeagueColors = configuration.GetSection("PdfConfig:LeagueColors").Get<Dictionary<string, string>>();

        // TODO colors came from windows, which are not available here.
        // pdfConfig.LeagueColors = pdfConigLeagueColors.ToDictionary(x => x.Key, x => Color.FromHex(x.Value));

        serviceCollection
            .AddSingleton(pdfConfig)
            .AddTransient<PdfGenerator>();

        return serviceCollection;
    }
}