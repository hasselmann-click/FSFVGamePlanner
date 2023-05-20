﻿using CsvHelper;
using CsvHelper.Configuration;
using FSFV.Gameplanner.Common;
using FSFV.Gameplanner.Common.Dto;
using FSFV.Gameplanner.Fixtures;
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

    private readonly ILogger<FsfvCustomSerializerService> logger;

    public FsfvCustomSerializerService(ILogger<FsfvCustomSerializerService> logger)
    {
        this.logger = logger;
    }

    public async Task<Dictionary<string, GroupTypeDto>> ParseGroupTypesAsync(string filePath)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            MissingFieldFound = null
        };
        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, config);
        var records = csv.GetRecordsAsync<GroupTypeDto>();

        var groupTypes = new Dictionary<string, GroupTypeDto>(3 + 1); // 3 leagues 
        await foreach (var record in records)
        {
            groupTypes.Add(record.Name, record);
        }
        return groupTypes;
    }

    public async Task<List<Game>> ParseFixturesAsync(Dictionary<string, GroupTypeDto> groupTypes,
        string[] fixtureFiles)
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

    public async Task<List<Pitch>> ParsePitchesAsync(string pitchesFile, char separator = ',')
    {
        var lines = await File.ReadAllLinesAsync(pitchesFile, Encoding.UTF8);

        // first row: Date,R2,R6....        
        var headers = lines[0].Split(separator);
        List<string> pitchNames = new(headers.Length - 1);
        for (int h = 1; h < headers.Length; ++h)
        {
            pitchNames.Add(headers[h]);
        }

        // educated guess: 4 pitches
        List<Pitch> pitches = new(lines.Length * 4);
        for (int i = 1; i < lines.Length; ++i)
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

    public static async Task WriteCsvGameplan(string filePath, List<GameDay> gameDays, string dateFormat = "dd.MM.yy")
    {
        // write csv
        using var csvStream = File.OpenWrite(filePath);
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
                    "League",
                    "Date"
        }));
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

    public static async Task WriteCsvStats(string filePath, List<Game> games)
    {
        using var csvStream = File.OpenWrite(filePath);
        using var csvWriter = new StreamWriter(csvStream, Encoding.UTF8);
        await csvWriter.WriteLineAsync(string.Join(",", new string[]
        {
                    "League",
                    "Team",
                    "Referee",
                    "Morning",
                    "Evening"
        }));
        var teams = games
            .SelectMany(g => new[] { (League: g.Group.Type.Name, Team: g.Home), (League: g.Group.Type.Name, Team: g.Away) })
            .DistinctBy(t => t.Team.Name)
            .OrderBy(t => t.League);
        foreach (var t in teams)
        {
            await csvWriter.WriteLineAsync(string.Join(",", new string[]
                   {
                       t.League,
                       t.Team.Name,
                       t.Team.RefereeCommitment + "",
                       t.Team.MorningGames + "",
                       t.Team.EveningGames + ""
                   }));
        }
        csvWriter.Close();
    }

}