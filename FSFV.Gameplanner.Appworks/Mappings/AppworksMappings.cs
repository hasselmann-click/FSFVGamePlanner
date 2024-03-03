namespace FSFV.Gameplanner.Appworks.Mappings;

public class AppworksMappings(
    // basically static    
    Dictionary<string, int> locations, Dictionary<string, int> teams,
    // depending on tournament
    Dictionary<string, int> divisions, Dictionary<string, int> matchDays)
{
    public Dictionary<string, int> Locations { get; } = locations;
    public Dictionary<string, int> Teams { get; } = teams;
    public Dictionary<string, int> Divisions { get; } = divisions;
    public Dictionary<string, int> MatchDays { get; } = matchDays;
}

