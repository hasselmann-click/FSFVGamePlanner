using FSFV.Gameplanner.Appworks.Mappings;
using FSFV.Gameplanner.Service.Serialization;
using Microsoft.Extensions.Logging;

namespace FSFV.Gameplanner.Appworks;
public class AppworksTransformer(ILogger<AppworksTransformer> logger, IAppworksMappingImporter importer)
{

    public async Task<Dictionary<string, List<AppworksImportRecord>>> Transform(List<FsfvCustomSerializerService.GameplanGameDto> gamePlan)
    {
        var tournaments = gamePlan.Select(x => x.League).Distinct().ToList();
        logger.LogDebug("Found {TournamentCount} tournaments", tournaments.Count);

        var recordsPerTournament = new Dictionary<string, List<AppworksImportRecord>>(tournaments.Count);
        foreach (var tournament in tournaments)
        {
            var mappings = await importer.ImportMappings(tournament);
            UpdateTeamMappings(mappings, gamePlan, tournament);

            var games = gamePlan.Where(g => g.League == tournament).ToList();
            var records = new List<AppworksImportRecord>(games.Count);

            bool hasError = false;
            foreach (var game in games)
            {
                try
                {
                    var homeId = mappings.Teams[game.Home];
                    var awayId = mappings.Teams[game.Away];
                    var refereeId = mappings.Teams[game.Referee];
                    var matchdayId = mappings.Matchdays[game.Date.ToString(IAppworksMappingImporter.MatchdayDateFormat)];
                    var divisionId = mappings.Divisions[game.Group];
                    var locationId = mappings.Locations[game.Pitch];
                    var record = new AppworksImportRecord(matchdayId, divisionId, locationId, homeId, awayId, game.StartTime, refereeId);
                    records.Add(record);
                }
                catch (KeyNotFoundException e)
                {
                    logger.LogError(e, "Could not find mapping");
                    hasError = true;
                }
            }
            if (hasError)
            {
                throw new KeyNotFoundException("Could not find mappings. Is the input file in UTF-8 encoding?");
            }

            recordsPerTournament.Add(tournament, records);
        }

        return recordsPerTournament;
    }

    /// <summary>
    /// Updates the team mappings with the closest match if the team is not found.
    /// This is a simple utility to be able to not have the exact same team names in the mappings as in the gameplan.
    /// </summary>
    /// <param name="origMappings"></param>
    /// <param name="gamePlan"></param>
    /// <param name="tournament"></param>
    private void UpdateTeamMappings(AppworksIdMappings origMappings, List<FsfvCustomSerializerService.GameplanGameDto> gamePlan, string tournament)
    {
        var teams = gamePlan.Where(g => g.League == tournament).SelectMany(x => new[] { x.Home, x.Away, x.Referee }).Distinct().ToList();
        foreach (var team in teams)
        {
            if (origMappings.Teams.ContainsKey(team))
            {
                continue;
            }

            var closestMatch = origMappings.Teams.Keys.OrderBy(x => LevenshteinDistance(x, team)).First();
            logger.LogWarning("Could not find team {Team}. Using closest match {ClosestMatch}", team, closestMatch);
            origMappings.Teams.Add(team, origMappings.Teams[closestMatch]);
        }
    }

    public int LevenshteinDistance(string a, string b)
    {
        if (string.IsNullOrEmpty(a))
        {
            if (!string.IsNullOrEmpty(b))
            {
                return b.Length;
            }
            return 0;
        }

        if (string.IsNullOrEmpty(b))
        {
            if (!string.IsNullOrEmpty(a))
            {
                return a.Length;
            }
            return 0;
        }

        int lengthA = a.Length;
        int lengthB = b.Length;
        var distances = new int[lengthA + 1, lengthB + 1];
        for (int i = 0; i <= lengthA; distances[i, 0] = i++) ;
        for (int j = 0; j <= lengthB; distances[0, j] = j++) ;

        for (int i = 1; i <= lengthA; i++)
        {
            for (int j = 1; j <= lengthB; j++)
            {
                int cost = b[j - 1] == a[i - 1] ? 0 : 1;
                distances[i, j] = Math.Min(
                    Math.Min(distances[i - 1, j] + 1, distances[i, j - 1] + 1),
                    distances[i - 1, j - 1] + cost);
            }
        }
        return distances[lengthA, lengthB];
    }

}
