using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text;

namespace FSFV.Gameplanner.Appworks.Mappings;

public class AppworksMappingStreamImporter(ILogger<AppworksMappingStreamImporter> logger, Stream mappingStream) : IAppworksMappingImporter
{
    private static readonly Encoding DefaultEncoding = Encoding.UTF8;
    private AppworksIdMappings? cachedMappings;

    public async Task<AppworksIdMappings> ImportMappings(string tournament)
    {
        if (cachedMappings is null)
        {
            cachedMappings = await ParseCsvToMappingsAsync(mappingStream, tournament);
        }

        return cachedMappings with { Tournament = tournament };
    }

    private async Task<AppworksIdMappings> ParseCsvToMappingsAsync(Stream stream, string tournament)
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

        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        using var reader = new StreamReader(stream, DefaultEncoding, leaveOpen: true);
        using var csv = new CsvReader(reader, config);

        string? section = null;
        while (await csv.ReadAsync())
        {
            var firstColumn = csv[0];
            if (firstColumn is "Divisions" or "Matchdays" or "Locations" or "Teams")
            {
                section = firstColumn;
                await csv.ReadAsync();
                continue;
            }

            if (string.IsNullOrEmpty(firstColumn) || section == null)
            {
                continue;
            }

            if (!int.TryParse(firstColumn, out var id))
            {
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
                    // Matchday names only carry day and month (e.g. "06.09."), no year.
                    matchdays[DateOnly.ParseExact(name, IAppworksMappingImporter.MatchdayDateFormat, CultureInfo.InvariantCulture)
                        .ToString(IAppworksMappingImporter.MatchdayDateFormat)] = id;
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
