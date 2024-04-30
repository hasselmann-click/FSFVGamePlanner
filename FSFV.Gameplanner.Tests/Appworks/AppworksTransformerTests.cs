using FSFV.Gameplanner.Appworks;
using FSFV.Gameplanner.Appworks.Mappings;
using FSFV.Gameplanner.Service.Serialization;
using Microsoft.Extensions.Logging;
using Moq;
using System.Globalization;

namespace FSFV.Gameplanner.Tests.Appworks;

[TestClass()]
public class AppworksTransformerTests
{

    [TestMethod()]
    public async Task TransformTest_HappyCaseAsync()
    {

        // ARRANGE

        // INPUT
        var tournament = "M";
        Dictionary<string, int> locations = new() { { "R1", 5 }, { "R2", 6 }, { "R7", 7 } };
        Dictionary<string, int> teams = new() { { "team1", 10 }, { "team2", 24 }, { "team3", 90 }, 
            // team4 needs to be mapped via Levenshtein distance    
            { "FC teamThatNeedsToBeMapped", 112 } };
        Dictionary<string, int> divisions = new() { { "A", 5 }, { "B", 9 } };
        Dictionary<string, int> matchdays = new() { { "01.01.", 12 }, { "01.02.", 13 } };
        var mappings = new AppworksIdMappings(locations, teams, divisions, matchdays, tournament);

        // matchplan.csv, parsed by "FsfvCustomSerializerService.ParseGameplanAsync"
        var gamePlan = new List<FsfvCustomSerializerService.GameplanGameDto>() 
        {
            new()
            {
                Home = "team2",
                Away = "team1",
                Referee = "team3",
                GameDay = 1,
                StartTime = DateTime.Parse("01.01.22 10:00"),
                EndTime = DateTime.Parse("01.01.22 12:00"),
                Group = "A",
                League = "M",
                Pitch = "R2",
                Date = DateOnly.ParseExact("01.01.22", FsfvCustomSerializerService.DateFormat, CultureInfo.InvariantCulture),
            },
            new()
            {
                Home = "team3",
                Away = "teamThatNeedsToBeMapped",
                Referee = "team2",
                GameDay = 1,
                StartTime = DateTime.Parse("01.01.22 14:00"),
                EndTime = DateTime.Parse("01.01.22 16:00"),
                Group = "B",
                League = "M",
                Pitch = "R7",
                Date = DateOnly.ParseExact("01.01.22", FsfvCustomSerializerService.DateFormat, CultureInfo.InvariantCulture),
            }
        };

        // EXPECTED
        var expectedRecords = new List<AppworksImportRecord>()
        {
            new(12, 5, 6, 24, 10, new DateTime(2022, 1, 1, 10, 0, 0), 90),
            new(12, 9, 7, 90, 112, new DateTime(2022, 1, 1, 14, 0, 0), 24)
        };

        // MOCK
        var importer = new Mock<IAppworksMappingImporter>();
        importer.Setup(i => i.ImportMappings(tournament)).ReturnsAsync(mappings);

        var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<AppworksTransformer>();

        // ACT
        var transformer = new AppworksTransformer(logger, importer.Object);
        var recordsDic = await transformer.Transform(gamePlan);
        var records = recordsDic[tournament];

        // ASSERT
        Assert.AreEqual(expectedRecords.Count, records.Count);
        CollectionAssert.AreEqual(expectedRecords, records);
    }

    [TestMethod()]
    public void LevenshteinDistanceTest()
    {
        // ARRANGE
        string a = "kitten";
        string b = "sitting";
        int expectedDistance = 3;

        // ACT
        int actualDistance = AppworksTransformer.LevenshteinDistance(a, b);

        // ASSERT
        Assert.AreEqual(expectedDistance, actualDistance);
    }

}