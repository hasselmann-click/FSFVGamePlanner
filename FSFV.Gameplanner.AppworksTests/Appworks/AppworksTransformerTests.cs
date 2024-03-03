using FSFV.Gameplanner.Appworks;
using FSFV.Gameplanner.Appworks.Mappings;
using FSFV.Gameplanner.Service.Serialization;
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

        Dictionary<string, int> locations = new() { { "location1", 5 }, { "location2", 6 }, { "location7", 7 } };
        Dictionary<string, int> teams = new() { { "team1", 10 }, { "team2", 24 }, { "team3", 90 }, { "team4", 112 } };
        Dictionary<string, int> divisions = new() { { "division1", 5 }, { "division2", 9 } };
        Dictionary<string, int> matchdays = new() { { "matchday1", 12 }, { "matchday2", 13 } };
        var mappings = new AppworksMappings(locations, teams, divisions, matchdays);

        // matchplan.csv, parsed by serializer service "FsfvCustomSerializerService.ParseGameplanAsync"
        var gamePlan = new List<FsfvCustomSerializerService.GameplanGameDto>() 
        {
            new()
            {
                Home = "team2",
                Away = "team1",
                Referee = "team3",
                GameDay = 1,
                StartTime = DateTime.Parse("10:00"),
                EndTime = DateTime.Parse("12:00"),
                Group = "1",
                League = "M",
                Pitch = "2",
                Date = DateOnly.ParseExact("01.01.22", FsfvCustomSerializerService.DateFormat, CultureInfo.InvariantCulture),
            },
            new()
            {
                Home = "team3",
                Away = "team4",
                Referee = "team2",
                GameDay = 1,
                StartTime = DateTime.Parse("14:00"),
                EndTime = DateTime.Parse("16:00"),
                Group = "2",
                League = "M",
                Pitch = "7",
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
        importer.Setup(i => i.ImportMappings()).ReturnsAsync(mappings);

        // ACT
        var transformer = new AppworksTransformer(importer.Object);
        var records = await transformer.Transform(gamePlan);

        // ASSERT
        Assert.Equals(expectedRecords.Count, records.Count);

        Assert.Fail();
    }
}