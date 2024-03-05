using System.Net.NetworkInformation;

namespace FSFV.Gameplanner.Appworks.Mappings;

public interface IAppworksMappingImporter
{
    Task<AppworksIdMappings> ImportMappings(string tournament);

}