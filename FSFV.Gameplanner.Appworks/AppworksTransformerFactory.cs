using FSFV.Gameplanner.Appworks.Mappings;
using FSFV.Gameplanner.Appworks.Mappings.File;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSFV.Gameplanner.Appworks;
public class AppworksTransformerFactory(IServiceProvider serviceProvider)
{
    public AppworksTransformer CreateTransformer(Stream mappingStream)
    {
        var importLogger = serviceProvider.GetRequiredService<ILogger<AppworksMappingStreamImporter>>();
        var importer = new AppworksMappingStreamImporter(importLogger, mappingStream);

        var transformLogger = serviceProvider.GetRequiredService<ILogger<AppworksTransformer>>();
        return new AppworksTransformer(transformLogger, importer);
    }

    public AppworksTransformer CreateTransformer(string filePath)
    {
        var importLogger = serviceProvider.GetRequiredService<ILogger<AppworksMappingFileImporter>>();
        var importer = new AppworksMappingFileImporter(importLogger, filePath);

        var transformLogger = serviceProvider.GetRequiredService<ILogger<AppworksTransformer>>();
        return new AppworksTransformer(transformLogger, importer);
    }

}
