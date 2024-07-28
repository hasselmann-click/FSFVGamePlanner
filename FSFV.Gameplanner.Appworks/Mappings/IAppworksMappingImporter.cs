using System.Net.NetworkInformation;

namespace FSFV.Gameplanner.Appworks.Mappings;

public interface IAppworksMappingImporter
{

    public const string MatchdayDateFormat = "dd.MM.";
    
    Task<AppworksIdMappings> ImportMappings(string tournament);

}