using FSFV.Gameplanner.Service.Serialization;
using FSFV.Gameplanner.Service.Serialization.Dto;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Previewer;
using System.Runtime.InteropServices;

namespace FSFV.Gameplanner.Pdf;

public class PdfGenerator(ILogger<PdfGenerator> logger, CsvSerializerService serializer)
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

    public async Task GenerateAsync(
        PdfConfig config,
        Func<Task<Stream>> writeStreamProvider,
        Func<Task<Stream>> gameplanCsvStream,
        Func<Task<Stream>>? holidaysStream = null,
        bool showDocument = false)
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
            await GenerateAsync(config, writeStream, gameplanStream, holidays, showDocument);
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
    public async Task GenerateAsync(PdfConfig config,
        Stream writeStream,
        Stream gameplanCsvStream,
        Stream? holidaysStream = null,
        bool showDocument = false)
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
                    container.Page(ComposePageSpecialDays(key, value, config));
                    nextHoliday = holidays!.OrderBy(x => x.Key).FirstOrDefault(x => x.Key.CompareTo(key) > 0);
                }

                // game day page
                container.Page(ComposePageGameDay(gameDay, config));
            }
        });

        if (showDocument) { document.ShowInPreviewer(); }

        try
        {
            // Use the byte[] overload so QuestPDF manages its own internal stream rather than
            // wrapping our MemoryStream in a native SkWriteStream. The native stream wrapper in
            // QuestPDF 2024.3.x can put the PDF document into an invalid state when a managed
            // MemoryStream is passed directly, causing SkDocument.BeginPage to return null.
            var pdfBytes = document.GeneratePdf();
            await writeStream.WriteAsync(pdfBytes);
        }
        catch (QuestPDF.Drawing.Exceptions.DocumentDrawingException ex)
            when (TryGetInitializationException(ex, out var initializationException))
        {
            var diagnosticMessage = BuildNativeRenderDiagnostics();
            logger.LogError(initializationException,
                "QuestPDF native initialization failed. {Diagnostics}",
                diagnosticMessage);

            throw new InvalidOperationException(
                $"PDF rendering failed due to native renderer initialization. {diagnosticMessage}",
                ex);
        }
    }

    private static bool TryGetInitializationException(Exception ex, out QuestPDF.Drawing.Exceptions.InitializationException? initializationException)
    {
        var current = ex;
        while (current is not null)
        {
            if (current is QuestPDF.Drawing.Exceptions.InitializationException initEx)
            {
                initializationException = initEx;
                return true;
            }

            current = current.InnerException!;
        }

        initializationException = null;
        return false;
    }

    private static string BuildNativeRenderDiagnostics()
    {
        var runtimePath = AppContext.BaseDirectory;
        var runtimeId = RuntimeInformation.RuntimeIdentifier;
        var os = RuntimeInformation.OSDescription;
        var processArch = RuntimeInformation.ProcessArchitecture;
        var osArch = RuntimeInformation.OSArchitecture;
        var nativeRelativePath = GetNativeSkiaRelativePath();
        var nativePath = Path.Combine(runtimePath, nativeRelativePath);

        return $"RID={runtimeId}, OS={os}, ProcessArch={processArch}, OSArch={osArch}, BaseDir={runtimePath}, NativeRelativePath={nativeRelativePath}, NativeExists={File.Exists(nativePath)}";
    }

    private static string GetNativeSkiaRelativePath()
    {
        var ridPart = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? $"win-{ToRidArch(RuntimeInformation.ProcessArchitecture)}"
            : RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                ? $"linux-{ToRidArch(RuntimeInformation.ProcessArchitecture)}"
                : RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                    ? $"osx-{ToRidArch(RuntimeInformation.ProcessArchitecture)}"
                    : "unknown";

        var fileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "QuestPdfSkia.dll"
            : RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                ? "libQuestPdfSkia.so"
                : "libQuestPdfSkia.dylib";

        return Path.Combine("runtimes", ridPart, "native", fileName);
    }

    private static string ToRidArch(Architecture architecture)
    {
        return architecture switch
        {
            Architecture.X64 => "x64",
            Architecture.Arm64 => "arm64",
            Architecture.X86 => "x86",
            Architecture.Arm => "arm",
            _ => architecture.ToString().ToLowerInvariant()
        };
    }

    private Action<PageDescriptor> ComposePageGameDay(IGrouping<DateOnly, GameplanGameDto> gameDay, PdfConfig activeConfig)
    {
        return page =>
        {
            // Set page styles
            page.PlanPageStyle();
            // Set page header and footer
            page.Header().Element(ComposeHeader(activeConfig.HeaderTitle));
            page.Footer().Element(ComposeFooter(gameDay.First().Date.ToString(activeConfig.FooterDateFormat)));

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

                        if (!TryResolveLeagueColor(activeConfig, game.League, out var color))
                        {
                            logger.LogWarning("No usable color defined for league {League}. Falling back to QuestPDF default row color.", game.League);
                            color = Colors.White;
                        }
                        t.Cell().RowContainer(row, color);

                        t.Cell().Row(row).Column(1).ValueCell().Text(game.Pitch);
                        t.Cell().Row(row).Column(2).ValueCell().Text(game.StartTime.ToString(activeConfig.GameStartTimeFormat));
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

    private Action<PageDescriptor> ComposePageSpecialDays(DateOnly date, string title, PdfConfig activeConfig)
    {
        return page =>
        {
            // Set page styles
            page.PlanPageStyle();
            // Set page header and footer
            page.Header().Element(ComposeHeader(activeConfig.HeaderTitle));
            page.Footer().Element(ComposeFooter(date.ToString(activeConfig.FooterDateFormat)));

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
                        .Background(activeConfig.HolidayColor)
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

    private static bool TryResolveLeagueColor(PdfConfig activeConfig, string league, out Color color)
    {
        if (activeConfig.LeagueColors.TryGetValue(league, out var hexColor)
            && Color.FromHex(hexColor) is Color parsedColor)
        {
            color = parsedColor;
            return true;
        }

        if (activeConfig.LeagueColors.TryGetValue(DefaultLeagueColorKey, out var defaultHexColor)
            && Color.FromHex(defaultHexColor) is Color defaultColor)
        {
            color = defaultColor;
            return true;
        }

        color = Colors.Transparent;
        return false;
    }

}
