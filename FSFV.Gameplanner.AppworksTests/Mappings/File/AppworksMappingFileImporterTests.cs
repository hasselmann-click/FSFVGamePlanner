using FSFV.Gameplanner.Appworks.Mappings.File;

namespace FSFV.Gameplanner.Tests.Mappings.File;

[TestClass()]
public class AppworksMappingFileImporterTests
{
    [TestMethod()]
    public async Task ImportMappingsTestAsync()
    {
        // ARRANGE
        // INPUTS
        var filePath = "./Mappings/File/testImport.csv";
        var tournament = "M";

        // EXPECTED
        var expectedLocations = new Dictionary<string, int> { { "R2", 2 }, { "R6", 6 }, { "R7", 7 }, { "R9", 9 }, { "R11", 11 } };
        var expectedMatchdays = new Dictionary<string, int> { { "24.09.", 11 }, { "01.10.", 12 }, { "29.10.", 23 } };
        var expectedDivisions = new Dictionary<string, int> { { "A", 7 }, { "B", 8 } };
        // skip teams

        // ACT
        var importer = new AppworksMappingFileImporter(filePath);
        var mappings = await importer.ImportMappings(tournament);

        // ASSERT
        CollectionAssert.AreEqual(expectedLocations, mappings.Locations);
        CollectionAssert.AreEqual(expectedMatchdays, mappings.Matchdays);

    }
}