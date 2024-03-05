namespace FSFV.Gameplanner.Appworks.Mappings;

/// <summary>
/// Mappings from the names used in the generated "matchplan.csv" to the website's IDs
/// E.g. "R2" -> ("Hardhof 2" ->) 123
/// </summary>
public record AppworksIdMappings(
    // basically static    
    Dictionary<string, int> Locations, Dictionary<string, int> Teams,
    // depending on tournament
    Dictionary<string, int> Divisions, 
    // Key in date format "dd.MM."
    Dictionary<string, int> Matchdays,
    // which tournament the divisions and matchdays are for
    string Tournament)
{
}

