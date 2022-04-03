﻿using FSFV.Gameplanner.Common.Dto;
using FSFV.Gameplanner.Service;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace FSFV.Gameplanner.ConsoleRunner
{
    public class Runner
    {
        private readonly ILogger<Runner> logger;
        private readonly SlotService slotService;

        public Runner(ILogger<Runner> logger, SlotService slotService)
        {
            this.logger = logger;
            this.slotService = slotService;
        }

        public void Run(List<Pitch> pitches, List<Game> fixtures)
        {

            var honorGroupType = new Group
            {
                GroupingID = 1,
                Priority = 100,
                Type = new GroupType { GroupTypeID = 1 }
            };
            var menGroupType = new Group
            {
                GroupingID = 2,
                Priority = 50,
                Type = new GroupType { GroupTypeID = 2 }
            };

            List<Game> games = new()
            {
                new Game
                {
                    Home = new Team { Type = new TeamType { Name = "EA1" } },
                    Away = new Team { Type = new TeamType { Name = "EA2" } },
                    Group = honorGroupType,
                    MinDuration = TimeSpan.FromMinutes(85)
                },
                new Game
                {
                    Home = new Team { Type = new TeamType { Name = "EB1" } },
                    Away = new Team { Type = new TeamType { Name = "EB2" } },
                    Group = honorGroupType,
                    MinDuration = TimeSpan.FromMinutes(85)
                },
                new Game
                {
                    Home = new Team { Type = new TeamType { Name = "EA3" } },
                    Away = new Team { Type = new TeamType { Name = "EA4" } },
                    Group = honorGroupType,
                    MinDuration = TimeSpan.FromMinutes(85)
                },
                new Game
                {
                    Home = new Team { Type = new TeamType { Name = "EB3" } },
                    Away = new Team { Type = new TeamType { Name = "EB4" } },
                    Group = honorGroupType,
                    MinDuration = TimeSpan.FromMinutes(85)
                },
                new Game
                {
                    Home = new Team { Type = new TeamType { Name = "MA1" } },
                    Away = new Team { Type = new TeamType { Name = "MA2" } },
                    Group = menGroupType,
                    MinDuration = TimeSpan.FromMinutes(85)
                },
                new Game
                {
                    Home = new Team { Type = new TeamType { Name = "MA3" } },
                    Away = new Team { Type = new TeamType { Name = "MA4" } },
                    Group = menGroupType,
                    MinDuration = TimeSpan.FromMinutes(85)
                },
                new Game
                {
                    Home = new Team { Type = new TeamType { Name = "MA5" } },
                    Away = new Team { Type = new TeamType { Name = "MA6" } },
                    Group = menGroupType,
                    MinDuration = TimeSpan.FromMinutes(85)
                },
                new Game
                {
                    Home = new Team { Type = new TeamType { Name = "MA7" } },
                    Away = new Team { Type = new TeamType { Name = "MA8" } },
                    Group = menGroupType,
                    MinDuration = TimeSpan.FromMinutes(85)
                },
                new Game
                {
                    Home = new Team { Type = new TeamType { Name = "MB1" } },
                    Away = new Team { Type = new TeamType { Name = "MB2" } },
                    Group = menGroupType,
                    MinDuration = TimeSpan.FromMinutes(85)
                },
                new Game
                {
                    Home = new Team { Type = new TeamType { Name = "MB3" } },
                    Away = new Team { Type = new TeamType { Name = "MB4" } },
                    Group = menGroupType,
                    MinDuration = TimeSpan.FromMinutes(85)
                },
                new Game
                {
                    Home = new Team { Type = new TeamType { Name = "MB5" } },
                    Away = new Team { Type = new TeamType { Name = "MB6" } },
                    Group = menGroupType,
                    MinDuration = TimeSpan.FromMinutes(85)
                },
                new Game
                {
                    Home = new Team { Type = new TeamType { Name = "MB7" } },
                    Away = new Team { Type = new TeamType { Name = "MB8" } },
                    Group = menGroupType,
                    MinDuration = TimeSpan.FromMinutes(85)
                }
            };


            var gameDay = slotService.Slot(pitches, games);
            logger.LogInformation("{GameDay}",
                JsonSerializer.Serialize(gameDay, new JsonSerializerOptions
                {
                    WriteIndented = true
                }));

            // TODO remove wait for log to finish
            Task.Delay(2000).Wait();
        }
    }
}