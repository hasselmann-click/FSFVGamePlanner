using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text;

namespace FSFV.Gameplanner.Appworks.Mappings.File;

public class AppworksMappingFileImporter(ILogger<AppworksMappingFileImporter> logger, string filePath) : IAppworksMappingImporter
{
    private static readonly Encoding DefaultEncoding = Encoding.UTF8;

    public Task<AppworksIdMappings> ImportMappings(string tournament)
    {
        return ParseCsvToMappingsAsync(filePath, tournament);
    }

    public async Task<AppworksIdMappings> ParseCsvToMappingsAsync(string filePath, string tournament)
    {
        var divisions = new Dictionary<string, int>();
        var matchdays = new Dictionary<string, int>();
        var locations = new Dictionary<string, int>();
        var teams = new Dictionary<string, int>();

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = false,
            Delimiter = ",",
            IgnoreBlankLines = true,
            MissingFieldFound = null,
        };

        using var reader = new StreamReader(filePath, DefaultEncoding);
        using var csv = new CsvReader(reader, config);

        string? section = null;
        while (await csv.ReadAsync())
        {
            var firstColumn = csv[0];
            if (firstColumn is "Divisions" or "Matchdays" or "Locations" or "Teams")
            {
                section = firstColumn;
                await csv.ReadAsync(); // Skip header line
                continue;
            }

            if (string.IsNullOrEmpty(firstColumn) || section == null)
            {
                continue;
            }

            if(!int.TryParse(firstColumn, out int id)) { 
                logger.LogDebug("Skipping what appears to be a header row: {row}", string.Join(",", csv.Parser.RawRecord));
                continue;
            }

            var name = (!string.IsNullOrEmpty(csv[2]) ? csv[2] : csv[1])
                ?? throw new InvalidOperationException("Was not expecting empty Appworks name in section " + section);

            switch (section)
            {
                case "Divisions":
                    divisions[name] = id;
                    break;
                case "Matchdays":
                    matchdays[DateOnly.Parse(name).ToString(IAppworksMappingImporter.MatchdayDateFormat)] = id;
                    break;
                case "Locations":
                    locations[name] = id;
                    break;
                case "Teams":
                    teams[name] = id;
                    break;
            }
        }

        return new AppworksIdMappings(locations, teams, divisions, matchdays, tournament);
    }


}
