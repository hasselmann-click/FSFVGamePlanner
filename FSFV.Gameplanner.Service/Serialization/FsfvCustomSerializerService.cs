using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using FSFV.Gameplanner.Common;
using FSFV.Gameplanner.Common.Dto;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSFV.Gameplanner.Service.Serialization;

public class FsfvCustomSerializerService
{
    public const string DateFormat = "dd.MM.yy";
    private const char TimeSeparator = '-';
    private static readonly Encoding DefaultEncoding = Encoding.UTF8;

    private readonly ILogger<FsfvCustomSerializerService> logger;

    public FsfvCustomSerializerService(ILogger<FsfvCustomSerializerService> logger)
    {
        this.logger = logger;
    }

    public async Task<Dictionary<string, GroupTypeDto>> ParseGroupTypesAsync(Func<Task<Stream>> fileStreamProvider)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            MissingFieldFound = null
        };
        await using var fileStream = await fileStreamProvider();
        using var reader = new StreamReader(fileStream, DefaultEncoding);
        using var csv = new CsvReader(reader, config);
        var records = csv.GetRecordsAsync<GroupTypeDto>();

        var groupTypes = new Dictionary<string, GroupTypeDto>(3 + 1); // 3 leagues 
        await foreach (var record in records)
        {
            groupTypes.Add(record.Name, record);
        }
        return groupTypes;
    }

    public async Task<List<Game>> ParseFixturesAsync(Dictionary<string, GroupTypeDto> groupTypes, IEnumerable<(string FileName, Func<Task<Stream>> StreamProvider)> files)
    {
        var count = files.Count();
        // ed. guess: fixtures for 6 game days à 5 matches
        List<Game> games = new(count * 6 * 5);
        // ed. guess: 10 teams per group
        Dictionary<string, Team> teams = new(count * 10);
        foreach (var file in files)
        {
            // requirement: file has to be called "*_[GroupType]_[Group].*"
            var name = Path.GetFileNameWithoutExtension(file.FileName).Split('_');
            if (name.Length < 2 || !groupTypes.TryGetValue(name[^2], out var type))
                throw new ArgumentException($"Could not get group type from file {file.FileName}");

            var group = new Group { Name = name[^1], Type = type };
            var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false
            };
            await using var fileStream = await file.StreamProvider();
            using var reader = new StreamReader(fileStream);
            using var csv = new CsvReader(reader, csvConfig);
            var fixtures = csv.GetRecordsAsync<FixtureDto>();

            logger.LogInformation("Starting at game day {start} for group {group} of type {type}",
                               group.Type.FixtureStart, group.Name, group.Type.Name);
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

    public async Task<List<Pitch>> ParsePitchesAsync(Func<Task<Stream>> pitchesStreamProvider, char separator = ',')
    {
        await using var stream = await pitchesStreamProvider();
        using var reader = new StreamReader(stream, DefaultEncoding);
        var lines = new List<string>(4 + 1); // 4 pitches + header
        string line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            lines.Add(line);
        }

        // first row: Date,R2,R6....        
        var headers = lines[0].Split(separator);
        List<string> pitchNames = new(headers.Length - 1);
        for (int h = 1; h < headers.Length; ++h)
        {
            pitchNames.Add(headers[h]);
        }

        // educated guess: 4 pitches
        List<Pitch> pitches = new(lines.Count * 4);
        for (int i = 1; i < lines.Count; ++i)
        {
            // following rows: 08.05.22,10:00-18:00,10:00-18:00,...
            var fields = lines[i].Split(separator);
            if (!DateTime.TryParse(fields[0], out var gameDay))
            {
                throw new ArgumentException($"Could not parse date from line: {lines[i]}");
            }

            for (int j = 1; j < fields.Length; ++j)
            {
                var pitch = new Pitch { Name = pitchNames[j - 1], GameDay = i };
                var times = fields[j].Split(TimeSeparator);
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

    public async Task<List<GameplanGameDto>> ParseGameplanAsync(Func<Task<Stream>> streamProvider)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            MissingFieldFound = null,
        };

        await using var stream = await streamProvider();
        using var reader = new StreamReader(stream, DefaultEncoding);
        using var csv = new CsvReader(reader, config);

        var options = new TypeConverterOptions { Formats = [DateFormat] };
        csv.Context.TypeConverterOptionsCache.AddOptions<DateOnly>(options);

        // hypothetically it's possible that the stream is inifinitely large. We could use
        // a timeout or a maximum limit of records to read. But for now, this will suffice.
        var records = csv.GetRecordsAsync<GameplanGameDto>();
        var dtos = new List<GameplanGameDto>(100);
        await foreach (var record in records)
        {
            dtos.Add(record);
        }
        return dtos;
    }

    public async Task WriteCsvGameplanAsync(Func<Task<Stream>> writeStreamProvider, List<GameDay> gameDays, string dateFormat = DateFormat)
    {
        // write csv
        await using var csvStream = await writeStreamProvider();
        using var csvWriter = new StreamWriter(csvStream, DefaultEncoding);
        await csvWriter.WriteLineAsync(string.Join(",",
        [
                    "GameDay",
                    "Pitch",
                    "StartTime",
                    "EndTime",
                    "Home",
                    "Away",
                    "Referee",
                    "Group",
                    "League",
                    "Date"
        ]));
        foreach (var gameDay in gameDays)
        {
            var slots = gameDay.Pitches
                .SelectMany(p => p.Slots
                    .Select(s => new
                    {
                        s.Game.GameDay,
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
                    slot.Referee?.Name ?? "<kein>",
                    slot.Group,
                    slot.League,
                    slot.StartTime.ToString(dateFormat)
                }));
            }
        }
        csvWriter.Close();
    }

    public async Task WriteCsvStatsAsync(Func<Task<Stream>> writeStreamProvider, IEnumerable<TeamStatsDto> teamStatsDto)
    {
        await using var csvStream = await writeStreamProvider();
        using var csvWriter = new StreamWriter(csvStream, DefaultEncoding);
        await csvWriter.WriteLineAsync(string.Join(",", new string[]
        {
                    "League",
                    "Name",
                    "Referee",
                    "MorningGames",
                    "EveningGames"
        }));
        foreach (var teamStat in teamStatsDto)
        {
            await csvWriter.WriteLineAsync(string.Join(",", new string[]
            {
                    teamStat.League,
                    teamStat.Name,
                    teamStat.Referee.ToString(),
                    teamStat.MorningGames.ToString(),
                    teamStat.EveningGames.ToString()
            }));
        }
        csvWriter.Close();
    }

    public class GameplanGameDto
    {
        public int GameDay { get; set; }
        public string Pitch { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Home { get; set; }
        public string Away { get; set; }
        public string Referee { get; set; }
        public string Group { get; set; }
        public string League { get; set; }
        public DateOnly Date { get; set; }
    }

    public class TeamStatsDto
    {
        public string League { get; set; }
        public string Name { get; set; }
        public int Referee { get; set; }
        public int MorningGames { get; set; }
        public int EveningGames { get; set; }
    }

}
