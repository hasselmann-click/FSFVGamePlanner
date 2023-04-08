using FSFV.Gameplanner.Common;
using FSFV.Gameplanner.Common.Dto;
using FSFV.Gameplanner.Service;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Tests.Services;

[TestClass]
public class LinearSlotServiceTest
{

    [TestMethod]
    public void TestSlotGameDay()
    {
        var rng = new Random(23432546);
        var logger = new Mock<ILogger<LinearSlotService>>();
        var slotService = new LinearSlotService(logger.Object, rng);

        var pitches = new List<Pitch>
        {
            new Pitch {
                StartTime = DateTime.Parse("10:00"),
                EndTime = DateTime.Parse("18:00"),
                Name = "R2",
            },
            new Pitch {
                StartTime = DateTime.Parse("12:00"),
                EndTime = DateTime.Parse("18:00"),
                Name = "R6",
            },
            new Pitch {
                StartTime = DateTime.Parse("10:00"),
                EndTime = DateTime.Parse("18:00"),
                Name = "R7",
            },
            new Pitch {
                StartTime = DateTime.Parse("10:00"),
                EndTime = DateTime.Parse("18:00"),
                Name = "R11",
            },
        };

        var womens = new GroupTypeDto
        {
            Name = "F",
            MinDurationMinutes = 75,
            ParallelGamesPerPitch = 2,
            RequiredPitchName = "R2",
            Priority = 1,
            FixtureStart = 1,
            MaxParallelPitches = 1,
            FinalsDays = 1,
        };
        var womens_g = new Group
        {
            Name = "F",
            Type = womens
        };

        var men = new GroupTypeDto
        {
            Name = "M",
            MinDurationMinutes = 95,
            ParallelGamesPerPitch = 1,
            Priority = 1,
            FixtureStart = 1,
            MaxParallelPitches = 4,
            FinalsDays = 1,
        };
        var mens_A = new Group
        {
            Name = "A",
            Type = men
        };
        var mens_B = new Group
        {
            Name = "B",
            Type = men
        };

        var vet = new GroupTypeDto
        {
            Name = "H",
            MinDurationMinutes = 95,
            ParallelGamesPerPitch = 1,
            Priority = 1,
            FixtureStart = 1,
            MaxParallelPitches = 2,
            FinalsDays = 1,
        };
        var vet_h = new Group
        {
            Name = "H",
            Type = vet
        };

        var games = new List<Game>
        {
            new Game
            {
                GameDay = 1,
                Home = new Team { Name= "Home" + rng.Next() },
                Away = new Team  { Name = "Away" + rng.Next() },
                Group = womens_g,
            },
            new Game
            {
                GameDay = 1,
                Home = new Team { Name= "Home" + rng.Next() },
                Away = new Team  { Name = "Away" + rng.Next() },
                Group = womens_g,
            },
            new Game
            {
                GameDay = 1,
                Home = new Team { Name= "Home" + rng.Next() },
                Away = new Team  { Name = "Away" + rng.Next() },
                Group = womens_g,
            },
            new Game
            {
                GameDay = 1,
                Home = new Team { Name= "Home" + rng.Next() },
                Away = new Team  { Name = "Away" + rng.Next() },
                Group = womens_g,
            },
new Game
            {
                GameDay = 1,
                Home = new Team { Name= "Home" + rng.Next() },
                Away = new Team  { Name = "Away" + rng.Next() },
                Group = womens_g,
            },

new Game
            {
                GameDay = 1,
                Home = new Team { Name= "Home" + rng.Next() },
                Away = new Team  { Name = "Away" + rng.Next() },
                Group = vet_h,
            },

new Game
            {
                GameDay = 1,
                Home = new Team { Name= "Home" + rng.Next() },
                Away = new Team  { Name = "Away" + rng.Next() },
                Group = vet_h,
            },

new Game
            {
                GameDay = 1,
                Home = new Team { Name= "Home" + rng.Next() },
                Away = new Team  { Name = "Away" + rng.Next() },
                Group = vet_h,
            },

new Game
            {
                GameDay = 1,
                Home = new Team { Name= "Home" + rng.Next() },
                Away = new Team  { Name = "Away" + rng.Next() },
                Group = vet_h,
            },

new Game
            {
                GameDay = 1,
                Home = new Team { Name= "Home" + rng.Next() },
                Away = new Team  { Name = "Away" + rng.Next() },
                Group = vet_h,
            },

    new Game
            {
                GameDay = 1,
                Home = new Team { Name= "Home" + rng.Next() },
                Away = new Team  { Name = "Away" + rng.Next() },
                Group = vet_h,
            },

new Game
            {
                GameDay = 1,
                Home = new Team { Name= "Home" + rng.Next() },
                Away = new Team  { Name = "Away" + rng.Next() },
                Group = vet_h,
            },

new Game
            {
                GameDay = 1,
                Home = new Team { Name= "Home" + rng.Next() },
                Away = new Team  { Name = "Away" + rng.Next() },
                Group = vet_h,
            },

new Game
            {
                GameDay = 1,
                Home = new Team { Name= "Home" + rng.Next() },
                Away = new Team  { Name = "Away" + rng.Next() },
                Group = vet_h,
            },

new Game
            {
                GameDay = 1,
                Home = new Team { Name= "Home" + rng.Next() },
                Away = new Team  { Name = "Away" + rng.Next() },
                Group = mens_A,
            },
new Game
            {
                GameDay = 1,
                Home = new Team { Name= "Home" + rng.Next() },
                Away = new Team  { Name = "Away" + rng.Next() },
                Group = mens_A,
            },

new Game
            {
                GameDay = 1,
                Home = new Team { Name= "Home" + rng.Next() },
                Away = new Team  { Name = "Away" + rng.Next() },
                Group = mens_A,
            },
new Game
            {
                GameDay = 1,
                Home = new Team { Name= "Home" + rng.Next() },
                Away = new Team  { Name = "Away" + rng.Next() },
                Group = mens_A,
            },
new Game
            {
                GameDay = 1,
                Home = new Team { Name= "Home" + rng.Next() },
                Away = new Team  { Name = "Away" + rng.Next() },
                Group = mens_A,
            },
new Game
            {
                GameDay = 1,
                Home = new Team { Name= "Home" + rng.Next() },
                Away = new Team  { Name = "Away" + rng.Next() },
                Group = mens_A,
            },
new Game
            {
                GameDay = 1,
                Home = new Team { Name= "Home" + rng.Next() },
                Away = new Team  { Name = "Away" + rng.Next() },
                Group = mens_B,
            },

new Game
            {
                GameDay = 1,
                Home = new Team { Name= "Home" + rng.Next() },
                Away = new Team  { Name = "Away" + rng.Next() },
                Group = mens_B,
            },

new Game
            {
                GameDay = 1,
                Home = new Team { Name= "Home" + rng.Next() },
                Away = new Team  { Name = "Away" + rng.Next() },
                Group = mens_B,
            },

new Game
            {
                GameDay = 1,
                Home = new Team { Name= "Home" + rng.Next() },
                Away = new Team  { Name = "Away" + rng.Next() },
                Group = mens_B,
            },

new Game
            {
                GameDay = 1,
                Home = new Team { Name= "Home" + rng.Next() },
                Away = new Team  { Name = "Away" + rng.Next() },
                Group = mens_B,
            },

        new Game
            {
                GameDay = 1,
                Home = new Team { Name= "Home" + rng.Next() },
                Away = new Team  { Name = "Away" + rng.Next() },
                Group = mens_B,
            },
        };

        var slottedPitches = slotService.SlotGameDay(pitches, games);
        slottedPitches.ForEach(p =>
        {
            Console.WriteLine(p.Name + ": " + p.TimeLeft);
        });
        slottedPitches.ForEach(p =>
        {
            Assert.IsTrue(p.TimeLeft.CompareTo(TimeSpan.Zero) <= 0);
        });
    }
}