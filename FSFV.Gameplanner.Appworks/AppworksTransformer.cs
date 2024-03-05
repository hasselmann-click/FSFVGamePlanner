using FSFV.Gameplanner.Appworks.Mappings;
using FSFV.Gameplanner.Service.Serialization;

namespace FSFV.Gameplanner.Appworks;
public class AppworksTransformer(IAppworksMappingImporter importer)
{

    public async Task<IEnumerable<AppworksImportRecord>> Transform(List<FsfvCustomSerializerService.GameplanGameDto> gamePlan)
    {
        var tournaments = gamePlan.Select(x => x.League).Distinct().ToList();
        var recordsPerTournament = new List<List<AppworksImportRecord>>(tournaments.Count);
        foreach (var tournament in tournaments)
        {
            var mappings = await importer.ImportMappings(tournament);
            var games = gamePlan.Where(g => g.League == tournament).ToList();
            var records = new List<AppworksImportRecord>(games.Count);
            foreach (var game in games)
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
            recordsPerTournament.Add(records);
        }

        return recordsPerTournament.SelectMany(x => x);
    }

}
