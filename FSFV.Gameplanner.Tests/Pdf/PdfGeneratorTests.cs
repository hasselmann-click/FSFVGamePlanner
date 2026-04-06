using FSFV.Gameplanner.Pdf;
using FSFV.Gameplanner.Service.Serialization;
using FSFV.Gameplanner.Service.Serialization.Dto;
using Microsoft.Extensions.Logging.Abstractions;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace FSFV.Gameplanner.Tests.Pdf;

[TestClass]
public class PdfGeneratorTests
{
    [TestMethod]
    public async Task GenerateAsync_WithHolidayAfterLastGame_AddsTrailingHolidayPage()
    {
        var serializer = new CsvSerializerService(NullLogger<CsvSerializerService>.Instance);
        var generator = new PdfGenerator(NullLogger<PdfGenerator>.Instance, serializer);

        var config = new PdfConfig
        {
            HeaderTitle = "Holiday Plan",
            HolidayColor = "#ABCDEF",
            LeagueColors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Default"] = "#ECA64F",
            },
        };

        var games = new List<GameplanGameDto>
        {
            new()
            {
                GameDay = 1,
                Pitch = "Court 1",
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(9, 30),
                Home = "Team A",
                Away = "Team B",
                Referee = "Team C",
                Group = "Group A",
                League = "L",
                Date = new DateOnly(2026, 12, 26),
            },
        };

        await using var gameplanCsvStream = new MemoryStream();
        await CsvSerializerService.WriteCsvGameplanAsync(gameplanCsvStream, games);
        gameplanCsvStream.Position = 0;

        var holidaysCsv = string.Join(Environment.NewLine,
        [
            $"{new DateOnly(2026, 12, 24).ToString(CsvSerializerService.DateFormat, CultureInfo.InvariantCulture)},Christmas Eve",
            $"{new DateOnly(2026, 12, 27).ToString(CsvSerializerService.DateFormat, CultureInfo.InvariantCulture)},Cleanup Day",
        ]);

        await using var holidaysStream = new MemoryStream(Encoding.UTF8.GetBytes(holidaysCsv));
        await using var pdfStream = new MemoryStream();

        await generator.GenerateAsync(config, pdfStream, gameplanCsvStream, holidaysStream);

        var pdfText = Encoding.ASCII.GetString(pdfStream.ToArray());
        var pageCount = Regex.Matches(pdfText, @"/Type\s*/Page\b").Count;

        Assert.AreEqual(3, pageCount, "Expected one leading holiday page, one game day page, and one trailing holiday page.");
    }
}
