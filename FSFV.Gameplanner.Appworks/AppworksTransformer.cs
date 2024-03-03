using FSFV.Gameplanner.Appworks.Mappings;
using FSFV.Gameplanner.Service.Serialization;

namespace FSFV.Gameplanner.Appworks;
public class AppworksTransformer(IAppworksMappingImporter importer)
{

    public async Task<List<AppworksImportRecord>> Transform(List<FsfvCustomSerializerService.GameplanGameDto> gamePlan)
    {
        var mappings = await importer.ImportMappings();

        return [];
    }

}
