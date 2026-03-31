using FSFV.Gameplanner.Service.Serialization;
using FSFV.Gameplanner.Service.Serialization.Dto;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Previewer;
using System.Runtime.InteropServices;

namespace FSFV.Gameplanner.Pdf;

public class PdfGenerator(ILogger<PdfGenerator> logger, PdfConfig config, CsvSerializerService serializer)
{
    private const string DefaultLeagueColorKey = "Default";

    static PdfGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    private static Action<IContainer> ComposeHeader(string title)
    {
        return c => c
            .PaddingVertical(5)
            .PaddingTop(10)
            .AlignCenter()
            .Text(t =>
            {
                t.AlignCenter();
                t.Span(title);
            });
    }

    private static Action<IContainer> ComposeFooter(string title)
    {
        return c => c
            .PaddingVertical(10)
            .PaddingBottom(10)
            .AlignCenter()
            .Text(t =>
            {
                t.AlignCenter();
                t.Span(title);
            });
    }

    public async Task GenerateAsync(Func<Task<Stream>> writeStreamProvider,
        Func<Task<Stream>> gameplanCsvStream, Func<Task<Stream?>?>? holidaysStream = null, bool showDocument = false)
    {
        await using var writeStream = await writeStreamProvider();
        await using var gameplanStream = await gameplanCsvStream();

        Stream? holidays = null;
        if (holidaysStream is not null)
        {
            var holidaysTask = holidaysStream();
            if (holidaysTask is not null)
            {
                holidays = await holidaysTask;
            }
        }

        try
        {
            await GenerateAsync(writeStream, gameplanStream, holidays, showDocument);
        }
        finally
        {
            if (holidays is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else
            {
                holidays?.Dispose();
            }
        }
    }

    // This overload intentionally does not dispose caller-owned streams.
    public async Task GenerateAsync(Stream writeStream,
        Stream gameplanCsvStream, Stream? holidaysStream = null, bool showDocument = false)
    {
        await using var gameplanCsvCopy = new MemoryStream();
        await gameplanCsvStream.CopyToAsync(gameplanCsvCopy);
        gameplanCsvCopy.Position = 0;

        var games = await serializer.ParseGameplanAsync(() => Task.FromResult<Stream>(gameplanCsvCopy));
        var gamesPerDay = games.GroupBy(x => x.Date).OrderBy(x => x.Key).ToList();

        Dictionary<DateOnly, string>? holidays = null;
        if (holidaysStream is not null)
        {
            await using var holidaysCopy = new MemoryStream();
            await holidaysStream.CopyToAsync(holidaysCopy);
            holidaysCopy.Position = 0;
            holidays = await serializer.ParseHolidaysAsync(() => Task.FromResult<Stream?>(holidaysCopy));
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
                    container.Page(ComposePageSpecialDays(key, value));
                    nextHoliday = holidays!.OrderBy(x => x.Key).FirstOrDefault(x => x.Key.CompareTo(key) > 0);
                }

                // game day page
                container.Page(ComposePageGameDay(gameDay));
            }
        });

        if (showDocument) { document.ShowInPreviewer(); }

        try
        {
            document.GeneratePdf(writeStream);
        }
        catch (QuestPDF.Drawing.Exceptions.InitializationException ex)
        {
            var diagnosticMessage = BuildNativeRenderDiagnostics();
            logger.LogError(ex, "QuestPDF native initialization failed. {Diagnostics}", diagnosticMessage);
            throw new InvalidOperationException($"PDF rendering failed due to native renderer initialization. {diagnosticMessage}", ex);
        }
    }

    private static string BuildNativeRenderDiagnostics()
    {
        var runtimePath = AppContext.BaseDirectory;
        var runtimeId = RuntimeInformation.RuntimeIdentifier;
        var os = RuntimeInformation.OSDescription;
        var processArch = RuntimeInformation.ProcessArchitecture;
        var osArch = RuntimeInformation.OSArchitecture;
        var nativePath = Path.Combine(runtimePath, "runtimes", "win-x64", "native", "QuestPdfSkia.dll");

        return $"RID={runtimeId}, OS={os}, ProcessArch={processArch}, OSArch={osArch}, BaseDir={runtimePath}, WinNativeExists={File.Exists(nativePath)}";
    }

    private Action<PageDescriptor> ComposePageGameDay(IGrouping<DateOnly, GameplanGameDto> gameDay)
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

                        if (!TryResolveLeagueColor(game.League, out var color))
                        {
                            logger.LogWarning("No usable color defined for league {League}. Falling back to QuestPDF default row color.", game.League);
                            color = Colors.White;
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

    private Action<PageDescriptor> ComposePageSpecialDays(DateOnly date, string title)
    {
        return page =>
        {
            // Set page styles
            page.PlanPageStyle();
            // Set page header and footer
            page.Header().Element(ComposeHeader(config.HeaderTitle));
            page.Footer().Element(ComposeFooter(date.ToString(config.FooterDateFormat)));

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

    private bool TryResolveLeagueColor(string league, out Color color)
    {
        if (config.LeagueColors.TryGetValue(league, out color))
        {
            return true;
        }

        if (config.LeagueColors.TryGetValue(DefaultLeagueColorKey, out color))
        {
            return true;
        }

        return false;
    }
}
