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
                    var matchdayId = mappings.Matchdays[game.Date.ToString("dd.MM.")];
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

}
