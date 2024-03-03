namespace FSFV.Gameplanner.Appworks.Mappings;

public class AppworksMappingFileImporter(string filePath) : IAppworksMappingImporter
{
    public AppworksMappings ImportMappings()
    {
        var mappings = new AppworksMappings([], [], [], []);

        // read from file
        // fill the dictionaries

        return mappings;
    }
}
