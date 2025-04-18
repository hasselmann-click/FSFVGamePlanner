﻿using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using FSFV.Gameplanner.Common;
using FSFV.Gameplanner.Common.Dto;
using FSFV.Gameplanner.Service.Slotting.RuleBased.Rules.TargetState;
using FSFV.Gameplanner.Service.Migration;
using FSFV.Gameplanner.Service.Serialization.Dto;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSFV.Gameplanner.Service.Serialization;

public partial class CsvSerializerService(ILogger<CsvSerializerService> logger)
{
    public const string DateFormat = "dd.MM.yy";
    private const char TimeSeparator = '-';
    private static readonly Encoding DefaultEncoding = Encoding.UTF8;

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

        logger.LogDebug("Found Group Types: {types}", string.Join(", ", groupTypes.Keys));
        return groupTypes;
    }

    public async Task<List<Game>> ParseFixturesAsync(Dictionary<string, GroupTypeDto> groupTypes, IEnumerable<(string FileName, Func<Task<Stream>> StreamProvider)> files)
    {
        var count = files.Count();
        // ed. guess: fixtures for 6 game days à 5 matches
        List<Game> games = new(count * 6 * 5);
        // ed. guess: 10 teams per group
        Dictionary<string, Team> teams = new(count * 10);
        foreach (var (FileName, StreamProvider) in files)
        {
            // requirement: file has to be called "*_[GroupType]_[Group].*"
            var name = Path.GetFileNameWithoutExtension(FileName).Split('_');
            if (name.Length < 2 || !groupTypes.TryGetValue(name[^2], out var type))
                throw new ArgumentException($"Could not get group type from file {FileName}");

            var group = new Group { Name = name[^1], Type = type };
            var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false
            };
            await using var fileStream = await StreamProvider();
            using var reader = new StreamReader(fileStream);
            using var csv = new CsvReader(reader, csvConfig);
            var fixtures = csv.GetRecordsAsync<FixtureDto>();

            logger.LogInformation("Starting at game day {start} for group {group} of type {type}",
                               group.Type.FixtureStart, group.Name, group.Type.Name);
            await foreach (var fixture in fixtures)
            {

                if (fixture.GameDay < group.Type.FixtureStart)
                    continue;

                if (!teams.TryGetValue(fixture.Home, out var home))
                {
                    home = new Team { Name = fixture.Home };
                    teams.Add(fixture.Home, home);
                }
                if (!teams.TryGetValue(fixture.Away, out var away))
                {
                    away = new Team { Name = fixture.Away };
                    teams.Add(fixture.Away, away);
                }
                var game = new Game
                {
                    GameDay = l_NormalizedGameDay(group, fixture),
                    Home = home,
                    Away = away,
                    Group = group,
                    Referee = null
                };
                games.Add(game);
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
        while (await reader.ReadLineAsync() is string line)
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

                pitch.Date = DateOnly.FromDateTime(gameDay);
                pitch.StartTime = TimeOnly.FromDateTime(gameDay.Add(start));
                pitch.EndTime = TimeOnly.FromDateTime(gameDay.Add(end));
                pitches.Add(pitch);
            }
        }

        return pitches;
    }

    public async Task<Dictionary<DateOnly, string>> ParseHolidaysAsync(Func<Task<Stream?>?> streamProvider, char separator = ',')
    {
        if (streamProvider() is not Task<Stream?> task)
        {
            return [];
        }

        await using var stream = await task;
        if (stream is null)
        {
            return [];
        }

        using var reader = new StreamReader(stream, DefaultEncoding);
        var holidays = new Dictionary<DateOnly, string>(3); // educated guess
        while (await reader.ReadLineAsync() is string line)
        {
            var ar = line.Split(separator);
            holidays.Add(DateOnly.Parse(ar[0]), ar[1]);
        }

        logger.LogDebug("Found holidays: {days}", string.Join(", ", holidays.Values));
        return holidays;
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

        // hypothetically it's possible that the stream is inifinitely large. We could use
        // a timeout or a maximum limit of records to read. But for now, this will suffice.
        var records = csv.GetRecordsAsync<GameplanGameDto>();
        var dtos = new List<GameplanGameDto>(100);
        await foreach (var record in records)
        {
            dtos.Add(record);
        }

        logger.LogDebug("Found number of games: {cnt}", dtos.Count);
        return dtos;
    }

    public static async Task WriteCsvGameplanAsync(Stream writeStream, IEnumerable<GameplanGameDto> dtos)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
        };
        await using var csvWriter = new CsvWriter(new StreamWriter(writeStream, DefaultEncoding), config);
        csvWriter.WriteHeader<GameplanGameDto>();
        await csvWriter.WriteRecordsAsync(dtos);
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
                        p.Date,
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
                await csvWriter.WriteLineAsync(string.Join(",",
                [
                    slot.GameDay.ToString(),
                    slot.Pitch,
                    slot.StartTime.ToShortTimeString(),
                    slot.EndTime.ToShortTimeString(),
                    slot.Home.Name,
                    slot.Away.Name,
                    slot.Referee?.Name,
                    slot.Group,
                    slot.League,
                    slot.Date.ToString(dateFormat)
                ]));
            }
        }
        csvWriter.Close();
        logger.LogTrace("Finished writing gameplan as csv");
    }

    public async Task WriteCsvStatsAsync(Func<Task<Stream>> writeStreamProvider, IEnumerable<TeamStatsDto> teamStatsDto)
    {
        await using var csvStream = await writeStreamProvider();
        using var csvWriter = new StreamWriter(csvStream, DefaultEncoding);
        await csvWriter.WriteLineAsync(string.Join(",",
        [
                    "League",
                    "Name",
                    "Referee",
                    "MorningGames",
                    "EveningGames"
        ]));
        foreach (var teamStat in teamStatsDto)
        {
            await csvWriter.WriteLineAsync(string.Join(",",
            [
                    teamStat.League,
                    teamStat.Name,
                    teamStat.Referee.ToString(),
                    teamStat.MorningGames.ToString(),
                    teamStat.EveningGames.ToString()
            ]));
        }
        csvWriter.Close();
        logger.LogTrace("Finished writing stats as csv");
    }

    public async Task<List<TargetStateRuleConfiguration>> ParseTargetRuleConfigs(Func<Task<Stream>> streamProvider)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = false,
            Delimiter = ",",
            IgnoreBlankLines = true,
            MissingFieldFound = null,
        };

        await using var stream = await streamProvider();
        using var reader = new StreamReader(stream, DefaultEncoding);
        using var csv = new CsvReader(reader, config);

        var stateRules = new List<TargetStateRuleConfiguration>(10);
        int rowNr = 0;
        while (await csv.ReadAsync())
        {
            if ((++rowNr & 1) == 0)
            {
                // even, i.e. applicator
                var applicator = csv.GetRecord<TargetStateRuleConfiguration.Target>();
                logger.LogTrace("RowNr|Length: {rnr}|{length}", rowNr, stateRules.Count);
                stateRules[rowNr / 2 - 1].Applicator = applicator;
                continue;
            }

            // odd, i.e. filter
            var filter = csv.GetRecord<TargetStateRuleConfiguration.Current>();
            stateRules.Add(new TargetStateRuleConfiguration
            {
                Filter = filter
            });
        }

        return stateRules;
    }

    public static async Task<List<MigrationDto>> ParseMigrationsAsync(Func<Task<Stream>> value)
    {
        return await Task.FromResult<List<MigrationDto>>([]);
    }

}
