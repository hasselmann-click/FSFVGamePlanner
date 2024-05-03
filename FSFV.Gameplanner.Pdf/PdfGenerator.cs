using FSFV.Gameplanner.Service.Serialization;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using QuestPDF.Previewer;

namespace FSFV.Gameplanner.Pdf;

public class PdfGenerator(ILogger<PdfGenerator> logger, PdfConfig config, FsfvCustomSerializerService serializer)
{

    static PdfGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    private Action<IContainer> ComposeHeader(string title)
    {
        return c => c
            .PaddingVertical(10)
            .PaddingTop(20)
            .AlignCenter()
            .Text(t =>
            {
                t.AlignCenter();
                t.Span(title);
            });
    }

    private Action<IContainer> ComposeFooter(string title)
    {
        return c => c
            .PaddingVertical(10)
            .PaddingBottom(20)
            .AlignCenter()
            .Text(t =>
            {
                t.AlignCenter();
                t.Span(title);
            });
    }

    public async Task GenerateAsync(Func<Task<Stream>> writeStreamProvider,
        Func<Task<Stream>> gameplanCsvStream, Func<Task<Stream>>? holidaysStream = null, bool showDocument = false)
    {
        var games = await serializer.ParseGameplanAsync(gameplanCsvStream);
        var gamesPerDay = games.GroupBy(x => x.Date).OrderBy(x => x.Key).ToList();

        Dictionary<DateOnly, string>? holidays = null;
        if (holidaysStream is not null)
        {
            holidays = await serializer.ParseHolidaysAsync(holidaysStream);
        }

        var document = Document.Create(container =>
        {
            var nextHoliday = holidays?.OrderBy(x => x.Key).FirstOrDefault();
            foreach (var gameDay in gamesPerDay)
            {

                // add holiday pages as long as they are before the current game day
                while (nextHoliday?.Value is not null && nextHoliday?.Key.CompareTo(gameDay.Key) < 0)
                {
                    var (key, value) = nextHoliday.Value;
                    // special day page, e.g. Pentecost Monday
                    container.Page(ComposePageSpecialDays(value));
                    nextHoliday = holidays!.OrderBy(x => x.Key).FirstOrDefault(x => x.Key.CompareTo(key) > 0);
                }

                // game day page
                container.Page(ComposePageGameDay(gameDay));
            }
        });

        if (showDocument) { document.ShowInPreviewer(); }

        await using var writeStream = await writeStreamProvider();
        document.GeneratePdf(writeStream);
    }

    private Action<PageDescriptor> ComposePageGameDay(IGrouping<DateOnly, FsfvCustomSerializerService.GameplanGameDto> gameDay)
    {
        return page =>
        {
            // Set page styles
            page.PlanPageStyle();
            // Set page header and footer
            page.Header().Element(ComposeHeader(config.HeaderTitle));
            page.Footer().Element(ComposeFooter(gameDay.First().Date.ToString(config.FooterDateFormat)));

            // set page content
            page.Content()
                .PlanPageContentStyle()
                .Table(t =>
                {
                    t.ColumnsDefinition(d =>
                    {
                        d.RelativeColumn(1);
                        d.RelativeColumn(1);
                        d.RelativeColumn(3);
                        d.RelativeColumn(3);
                        d.RelativeColumn(3);
                        d.RelativeColumn(1);
                        d.RelativeColumn(1);
                    });

                    uint row = 1;
                    AddHeaderRow(t, row);
                    ++row;

                    foreach (var game in gameDay.OrderBy(x => x.StartTime))
                    {

                        if (!config.LeagueColors.TryGetValue(game.League, out var color))
                        {
                            logger.LogWarning("No color defined for league {League}. Using default color.", game.League);
                            color = config.LeagueColors["Default"];
                        }
                        t.Cell().RowContainer(row, color);

                        t.Cell().Row(row).Column(1).ValueCell().Text(game.Pitch);
                        t.Cell().Row(row).Column(2).ValueCell().Text(game.StartTime.ToString(config.GameStartTimeFormat));
                        t.Cell().Row(row).Column(3).ValueCell().Text(game.Home);
                        t.Cell().Row(row).Column(4).ValueCell().Text(game.Away);
                        t.Cell().Row(row).Column(5).ValueCell().Text(game.Referee ?? "");
                        t.Cell().Row(row).Column(6).ValueCell().Text(game.Group);
                        t.Cell().Row(row).Column(7).ValueCell().Text(game.League);
                        ++row;
                    }
                });
        };
    }

    private Action<PageDescriptor> ComposePageSpecialDays(string title)
    {
        return page =>
        {
            // Set page styles
            page.PlanPageStyle();
            // Set page header and footer
            page.Header().Element(ComposeHeader(config.HeaderTitle));
            page.Footer().Element(ComposeFooter(title));

            // set page content
            page.Content()
                .PlanPageContentStyle()
                .Table(t =>
                {
                    t.ColumnsDefinition(d =>
                    {
                        d.RelativeColumn(1);
                        d.RelativeColumn(1);
                        d.RelativeColumn(3);
                        d.RelativeColumn(3);
                        d.RelativeColumn(3);
                        d.RelativeColumn(1);
                        d.RelativeColumn(1);
                    });

                    uint row = 1;
                    AddHeaderRow(t, row++);

                    for (; row <= 10; ++row)
                    {
                        // content
                        t.Cell().Row(row).ColumnSpan(7).ValueCell().Text("");
                    }

                    t.Cell()
                        .Row(5).Column(3).RowSpan(5).ColumnSpan(3)
                        .Background("#DCD9C5")
                        .AlignCenter()
                        .AlignMiddle()
                        .LabelCell(title)
                        ;
                });
        };
    }

    private static void AddHeaderRow(TableDescriptor t, uint row)
    {
        t.Cell().Row(row).ColumnSpan(7).Background("#DCD9C5"); // "Olive" - Title

        t.Cell().Row(row).Column(1).LabelCell("PLATZ");
        t.Cell().Row(row).Column(2).LabelCell("ZEIT");
        t.Cell().Row(row).Column(3).LabelCell("TEAM 1");
        t.Cell().Row(row).Column(4).LabelCell("TEAM 2");
        t.Cell().Row(row).Column(5).LabelCell("SCHIRI");
        t.Cell().Row(row).Column(6).LabelCell("GRUPPE");
        t.Cell().Row(row).Column(7).LabelCell("LIGA");
    }
}
