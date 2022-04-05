using CsvHelper;
using CsvHelper.Configuration;
using FSFV.Gameplanner.Common;
using FSFV.Gameplanner.Common.Dto;
using FSFV.Gameplanner.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSFV.Gameplanner.ConsoleRunner;

public class Runner
{

    private const string MatchPlanCsv = "MatchPlan.csv";
    //private const string MatchPlanJson = "MatchPlan.json";

    private const string PitchesFileSearchPattern = "pitches.csv";
    private const string FixtureFilesSearchPattern = "fixtures_*_*.csv";

    private readonly IConfiguration configuration;
    private readonly ILogger<Runner> logger;
    private readonly SlotService slotService;

    public Runner(IConfiguration configuration, ILogger<Runner> logger,
        SlotService slotService)
    {
        this.configuration = configuration;
        this.logger = logger;
        this.slotService = slotService;
    }

    public async Task Run(string[] args)
    {

        if (args.Length < 1)
            throw new ArgumentException("Missing arguments");

        var workDirPath = args[0];
        if (!Directory.Exists(workDirPath))
            throw new ArgumentException($"Directory not valid: {workDirPath}");

        var pitchesFile = Directory.GetFiles(workDirPath, PitchesFileSearchPattern).FirstOrDefault();
        if (string.IsNullOrEmpty(pitchesFile))
            throw new ArgumentException($"Could not find pitches file: {PitchesFileSearchPattern}");

        var fixtureFiles = Directory.GetFiles(workDirPath, FixtureFilesSearchPattern);
        if (fixtureFiles == null || fixtureFiles.Length == 0)
            throw new ArgumentException($"Could not find any fixture files with pattern:" +
                $" {FixtureFilesSearchPattern}");

        Dictionary<string, GroupTypeDto> groupTypes = ParseGroupTypes(configuration);
        List<Game> games = await ParseFixturesAsync(groupTypes, fixtureFiles);
        List<Pitch> pitches = await ParsePitchesAsync(logger, pitchesFile);

        // TODO skip certain gamedays for certain leagues
        // e.g. for cup days. Or integrate cup fixtures in fixture file?

        // slot by game day
        var pitchesOrdered = pitches.GroupBy(p => p.GameDay).OrderBy(g => g.Key);
        List<GameDay> gameDays = new(pitchesOrdered.Count());
        foreach (var gameDayPitches in pitchesOrdered)
        {
            var slottedPitches = slotService.SlotGameDay(gameDayPitches.ToList(),
                games.Where(g => g.GameDay == gameDayPitches.Key).ToList());
            gameDays.Add(new GameDay
            {
                GameDayID = gameDayPitches.Key,
                Pitches = slottedPitches
            });
        }

        // write meta json
        //await File.WriteAllTextAsync(Path.Combine(workDirPath, MatchPlanJson),
        //    JsonSerializer.Serialize(gameDays, new JsonSerializerOptions
        //    {
        //        WriteIndented = true
        //    }), Encoding.UTF8);

        await WriteCsv(workDirPath, gameDays);
    }

    private static async Task WriteCsv(string workDirPath, List<GameDay> gameDays)
    {
        // write csv
        using var csvStream = File.OpenWrite(Path.Combine(workDirPath, MatchPlanCsv));
        using var csvWriter = new StreamWriter(csvStream, Encoding.UTF8);
        await csvWriter.WriteLineAsync(string.Join(",", new string[]
        {
                    "GameDay",
                    "Pitch",
                    "StartTime",
                    "EndTime",
                    "Home",
                    "Away",
                    "Referee",
                    "Group",
                    "League"
        }));
        foreach (var gameDay in gameDays)
        {
            var slots = gameDay.Pitches
                .SelectMany(p => p.Slots
                    .Select(s => new
                    {
                        GameDay = s.Game.GameDay,
                        Pitch = p.Name,
                        s.StartTime,
                        s.EndTime,
                        s.Game.Home,
                        s.Game.Away,
                        s.Game.Referee,
                        Group = s.Game.Group.Name,
                        League = s.Game.Group.Type.Name
                    }))
                .OrderBy(a => a.StartTime);
            foreach (var slot in slots)
            {
                await csvWriter.WriteLineAsync(string.Join(",", new string[]
                {
                    slot.GameDay.ToString(),
                    slot.Pitch,
                    slot.StartTime.ToShortTimeString(),
                    slot.EndTime.ToShortTimeString(),
                    slot.Home.Name,
                    slot.Away.Name,
                    slot.Referee.Name,
                    slot.Group,
                    slot.League
                }));
            }
            await csvWriter.FlushAsync();
        }
        csvWriter.Close();
    }

    private static Dictionary<string, GroupTypeDto> ParseGroupTypes(
    IConfiguration configuration)
    {
        var groupTypesFile = configuration.GetValue<string>(GroupTypeDto.FileKey);

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            MissingFieldFound = null
        };
        using var reader = new StreamReader(groupTypesFile);
        using var csv = new CsvReader(reader, config);
        var records = csv.GetRecords<GroupTypeDto>();
        return records.ToDictionary(r => r.Name);
    }

    private static async Task<List<Game>> ParseFixturesAsync(
        Dictionary<string, GroupTypeDto> groupTypes, string[] fixtureFiles)
    {
        // ed. guess: fixtures for 6 game days à 5 matches
        List<Game> games = new(fixtureFiles.Length * 6 * 5);
        // ed. guess: 10 teams per group
        Dictionary<string, Team> teams = new(fixtureFiles.Length * 10);
        foreach (var file in fixtureFiles)
        {
            // requirement: file has to be called "*_[GroupType]_[Group].*"
            var name = Path.GetFileNameWithoutExtension(file).Split('_');
            if (name.Length < 2 || !groupTypes.TryGetValue(name[^2], out var type))
                throw new ArgumentException($"Could not get group type from file {file}");

            var group = new Group { Name = name[^1], Type = type };


            var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false
            };
            using var reader = new StreamReader(file);
            using var csv = new CsvReader(reader, csvConfig);
            var fixtures = csv.GetRecordsAsync<FixtureDto>();

            await foreach (var fixture in fixtures)
            {

                if (fixture.GameDay < group.Type.FixtureStart)
                    continue;

                if (!teams.TryGetValue(fixture.Home, out Team home))
                {
                    home = new Team { Name = fixture.Home };
                    teams.Add(fixture.Home, home);
                }
                if (!teams.TryGetValue(fixture.Away, out Team away))
                {
                    away = new Team { Name = fixture.Away };
                    teams.Add(fixture.Away, away);
                }

                games.Add(new Game
                {
                    GameDay = l_NormalizedGameDay(group, fixture),
                    Home = home,
                    Away = away,
                    Group = group,
                    Referee = null
                });
            }
        }

        return games;

        static int l_NormalizedGameDay(Group group, FixtureDto fixture)
        {
            return fixture.GameDay - group.Type.FixtureStart + 1;
        }
    }

    private static async Task<List<Pitch>> ParsePitchesAsync(ILogger logger, string pitchesFile,
        char separator = ',')
    {
        var lines = await File.ReadAllLinesAsync(pitchesFile, Encoding.UTF8);

        // first row: Date,R2,R6....        
        var headers = lines[0].Split(separator);
        List<string> pitchNames = new(headers.Length - 1);
        for (int h = 1; h < headers.Length; ++h)
        {
            pitchNames.Add(headers[h]);
        }

        // ed. guess: 4 pitches
        List<Pitch> pitches = new(lines.Length * 4);
        for (int i = 1; i < lines.Length; ++i)
        {
            // following rowes: 08.05.22,10:00-18:00,10:00-18:00,...
            var fields = lines[i].Split(separator);
            if (!DateTime.TryParse(fields[0], out var gameDay))
            {
                throw new ArgumentException($"Could not parse date from line: {lines[i]}");
            }

            for (int j = 1; j < fields.Length; ++j)
            {
                var pitch = new Pitch { Name = pitchNames[j - 1], GameDay = i };
                var times = fields[j].Split('-');
                if (times.Length != 2)
                {
                    if (string.IsNullOrEmpty(times[0]))
                    {
                        logger.LogWarning("Pitch {pitch} not available at {date}", pitch.Name,
                            gameDay.ToShortDateString());
                        continue;
                    }
                    throw new ArgumentException($"Could not parse times from {fields[j]}");
                }

                if (!TimeSpan.TryParse(times[0], out var start)
                    || !TimeSpan.TryParse(times[1], out var end))
                    throw new ArgumentException($"Could not parse times for pitch {pitch.Name}" +
                        $" at {gameDay.ToShortDateString()}");

                pitch.StartTime = gameDay.Add(start);
                pitch.EndTime = gameDay.Add(end);
                pitches.Add(pitch);
            }
        }

        return pitches;
    }
}
