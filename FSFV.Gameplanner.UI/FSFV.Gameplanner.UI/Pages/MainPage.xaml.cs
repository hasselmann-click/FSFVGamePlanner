// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using CommunityToolkit.WinUI.Helpers;
using FSFV.Gameplanner.Appworks;
using FSFV.Gameplanner.Appworks.Serialization;
using FSFV.Gameplanner.Common;
using FSFV.Gameplanner.Common.Rng;
using FSFV.Gameplanner.Fixtures;
using FSFV.Gameplanner.Pdf;
using FSFV.Gameplanner.Service.Migration;
using FSFV.Gameplanner.Service.Serialization;
using FSFV.Gameplanner.Service.Serialization.Dto;
using FSFV.Gameplanner.Service.Slotting;
using FSFV.Gameplanner.Service.Slotting.RuleBased.Rules.TargetState;
using FSFV.Gameplanner.UI.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using Windows.Storage.Search;
using static FSFV.Gameplanner.UI.Pages.MainPageViewModel;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace FSFV.Gameplanner.UI.Pages;

#pragma warning disable CS8602 // Dereference of a possibly null reference.

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainPage : Page
{
    private const string GamesPlaceholder = "SPIELFREI";
    private const string StatsFileSuffix = "_stats.csv";
    private const string AppworksImportFileSuffix = "_appworks.csv";

    private const int FolderContentsChangedEventThrottleDuration = 10_000;

    private delegate void FilesChangedHandler(IReadOnlyList<StorageFile> files);
    private event FilesChangedHandler OnFolderPicked;

    public MainPageViewModel ViewModel { get; internal set; }

    public MainPage()
    {
        this.InitializeComponent();
        ViewModel = new MainPageViewModel(DispatcherQueue.GetForCurrentThread());

        OnFolderPicked += LookingForTeamFiles;
        OnFolderPicked += LookingForConfigFiles;
        OnFolderPicked += LookingForGameplanFiles;
        OnFolderPicked += LookingForAppworksMappingsFiles;
        OnFolderPicked += LookingForPdfGenerationFiles;
        OnFolderPicked += LookingForRngSeedFile;
        OnFolderPicked += LookingForMigrationFile;

        UILogger.OnMessageLogged += UpdateLog;
    }

    private void UpdateLog(UILogMessage message)
    {
        LogListView.Items.Add(message);
    }

    private void ClearLogButton_Click(object sender, RoutedEventArgs e)
    {
        LogListView.Items.Clear();
    }

    #region Folder Picker
    private StorageFileQueryResult? query = null;
    private Timer? folderContentsChangedThrottleTimer = null;

    private async void FolderPicker_Click(object sender, RoutedEventArgs e)
    {
        // Create a folder picker and initialize the folder picker with the window handle (HWND).
        FolderPicker openPicker = new();
        var hWnd = WindowHelper.GetWindowHandle(this);
        WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);

        // Set options for your folder picker
        openPicker.SuggestedStartLocation = PickerLocationId.Desktop;
        openPicker.FileTypeFilter.Add("*");

        // Open the picker for the user to pick a folder
        StorageFolder folder = await openPicker.PickSingleFolderAsync();
        if (folder == null)
        {
            return;
        }
        await ChangeWorkFolder(folder);
    }

    private async Task ChangeWorkFolder(StorageFolder newFolder)
    {
        query = newFolder.CreateFileQuery();
        query.ContentsChanged += OnFolderContentChanged;
        // trigger initial contents change event listening
        await query.GetFilesAsync();
        // ..and explicitly call the event, since choosing a folder without "files" won't trigger initially.
        // Double invocations are handled by throttling.
        OnFolderContentChanged(query, null);
        ViewModel.WorkDir = newFolder;
    }

    private async void FolderReload_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.WorkDir == null) return;

        var files = await ViewModel.WorkDir.GetFilesAsync();
        OnFolderPicked?.Invoke(files);
    }

    private void OnFolderContentChanged(IStorageQueryResultBase sender, object? args)
    {
        if (folderContentsChangedThrottleTimer != null)
        {
            return;
        }

        ViewModel.Dispatcher.TryEnqueue(DispatcherQueuePriority.High, async () =>
        {
            var files = await sender.Folder.GetFilesAsync();
            OnFolderPicked?.Invoke(files);
        });

        folderContentsChangedThrottleTimer = new Timer(async (state) =>
        {
            if (folderContentsChangedThrottleTimer == null) return;
            await folderContentsChangedThrottleTimer.DisposeAsync();
            folderContentsChangedThrottleTimer = null;
        }, null, FolderContentsChangedEventThrottleDuration, Timeout.Infinite);
    }
    #endregion

    #region Config Files
    private void LookingForConfigFiles(IReadOnlyList<StorageFile> storageFiles)
    {
        ViewModel.ResetConfigFileRecords();
        ViewModel.FixtureFiles.Clear();

        var pitchesFile = storageFiles.FirstOrDefault(s => s.Name.StartsWith(MainPageViewModel.FileNamePrefixes.Pitches, StringComparison.InvariantCultureIgnoreCase));
        if (pitchesFile != null)
        {
            var record = ViewModel.ConfigFileRecords.FirstOrDefault(r => r.PreviewDisplayName == MainPageViewModel.FileNamePrefixes.DisplayNames.Pitches);

            record.IsFound = true;
            record.File = pitchesFile;
        }

        var leagueConfigsFile = storageFiles.FirstOrDefault(s => s.Name.StartsWith(MainPageViewModel.FileNamePrefixes.LeagueConfigs, StringComparison.InvariantCultureIgnoreCase));
        if (leagueConfigsFile != null)
        {
            var record = ViewModel.ConfigFileRecords.FirstOrDefault(r => r.PreviewDisplayName == MainPageViewModel.FileNamePrefixes.DisplayNames.LeagueConfigs);
            record.IsFound = true;
            record.File = leagueConfigsFile;
        }

        var fixtureFiles = storageFiles.Where(s => s.Name.StartsWith(MainPageViewModel.FileNamePrefixes.Fixtures, StringComparison.InvariantCultureIgnoreCase));
        if (fixtureFiles.Any())
        {
            ViewModel.ConfigFileRecords.Remove(ViewModel.ConfigFileRecords.First(r => r.PreviewDisplayName == MainPageViewModel.FileNamePrefixes.DisplayNames.Fixtures));
            foreach (var file in fixtureFiles)
            {
                ViewModel.FixtureFiles.Add(file);
            }
        }

        var targetStateRuleConfigs = storageFiles.FirstOrDefault(s => s.Name.StartsWith(MainPageViewModel.FileNamePrefixes.TargetRuleConfigs, StringComparison.InvariantCultureIgnoreCase));
        if (targetStateRuleConfigs != null)
        {
            ViewModel.ConfigFileRecords.Add(new ConfigFileRecordViewModel
            {
                File = targetStateRuleConfigs,
                IsFound = true
            });
        }

        ViewModel.GenerateGameplanButton_IsEnabled = ViewModel.ConfigFileRecords.All(r => r.IsFound)
            && fixtureFiles.Any();
    }

    #endregion

    #region Gameplan

    private async void OpenGameplanButton_Click(object sender, RoutedEventArgs e)
    {
        StorageFile file = await OpenFilePicker();
        // TODO validate picked file?
        if (file == null)
        {
            return;
        }

        var newWorkDir = await file.GetParentAsync();
        await ChangeWorkFolder(newWorkDir);

        ViewModel.GameplanFile = file;
        ViewModel.IsPreventRescanForGameplanFile = true;
        ViewModel.GenerateStatsButton_HasGenerated = false;
    }

    private IAsyncOperation<StorageFile> OpenFilePicker(string fileTypeFilter = ".csv")
    {
        FileOpenPicker openPicker = new();
        var hWnd = WindowHelper.GetWindowHandle(this);
        WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);

        // Set options for your folder picker
        openPicker.SuggestedStartLocation = PickerLocationId.Desktop;
        openPicker.FileTypeFilter.Add(fileTypeFilter);

        // Open the picker for the user to pick a folder
        return openPicker.PickSingleFileAsync();
    }

    private async void LookingForGameplanFiles(IReadOnlyList<StorageFile> files)
    {
        if (ViewModel.IsPreventRescanForGameplanFile)
        {
            ViewModel.IsPreventRescanForGameplanFile = false;
            return;
        }

        ViewModel.GenerateGameplanButton_HasGenerated = false;
        ViewModel.GenerateStatsButton_HasGenerated = false;

        var gameplanCandidates = files
            .Where(s => s.Name.StartsWith(MainPageViewModel.FileNamePrefixes.Gameplan, StringComparison.InvariantCultureIgnoreCase))
            .Where(s => !s.Name.EndsWith(StatsFileSuffix))
            .ToList();

        // TODO use common file query options
        // this sorting is horrible but there is no other way to get the latest file
        // since getting the properties is an async operation
        var propTasks = new List<(StorageFile File, Task<BasicProperties> PropTask)>(files.Count);
        foreach (var file in gameplanCandidates)
        {
            propTasks.Add((file, file.GetBasicPropertiesAsync().AsTask()));
        }
        await Task.WhenAll(propTasks.Select(p => p.PropTask));

        var gameplanFile = propTasks
            .Where(p => p.PropTask.Result.Size > 0)
            .OrderByDescending(p => p.PropTask.Result.DateModified)
            .Select(p => p.File)
            .FirstOrDefault();

        ViewModel.GameplanFile = gameplanFile;
    }

    private async void GeneratePlanButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ViewModel.GenerateGameplanButton_HasGenerated = false;
            ViewModel.GenerateGameplanButton_IsGenerating = true;
            ViewModel.GenerateGameplanButton_IsEnabled = false;

            await GenerateGamePlanAsync();

            ViewModel.GenerateGameplanButton_IsGenerating = false;
            ViewModel.GenerateGameplanButton_HasGenerated = true;
            ViewModel.GenerateGameplanButton_IsEnabled = true;
        }
        catch
        {
            ViewModel.GenerateGameplanButton_IsGenerating = false;
            ViewModel.GenerateGameplanButton_HasGenerated = false;
            ViewModel.GenerateGameplanButton_IsEnabled = true;
            throw;
        }
    }

    private async Task GenerateGamePlanAsync()
    {
        var services = App.Current.Services;
        var rng = services.GetRequiredService<IRngProvider>();
        var logger = services.GetRequiredService<ILogger<MainPage>>();
        var serializer = services.GetRequiredService<CsvSerializerService>();

        // restart rng sequence
        rng.Reset(ViewModel.RngSeed);

        // parse league configs to group type dto
        var leagueConfigs = ViewModel.ConfigFileRecords.First(r => r.PreviewDisplayName == MainPageViewModel.FileNamePrefixes.DisplayNames.LeagueConfigs).File;
        var groupTypesTask = serializer.ParseGroupTypesAsync(() => leagueConfigs.OpenStreamForReadAsync());
        // parse target rules
        var targetRuleConfigs = ViewModel.ConfigFileRecords.FirstOrDefault(r => r.File?.Name.StartsWith(MainPageViewModel.FileNamePrefixes.TargetRuleConfigs) ?? false)?.File;
        Task<List<TargetStateRuleConfiguration>>? targetRuleTask = null;
        if (targetRuleConfigs != null)
        {
            targetRuleTask = serializer.ParseTargetRuleConfigs(() => targetRuleConfigs.OpenStreamForReadAsync());
        }
        // parse pitches to pitches
        var pitchesFile = ViewModel.ConfigFileRecords.First(r => r.PreviewDisplayName == MainPageViewModel.FileNamePrefixes.DisplayNames.Pitches).File;
        var pitchesTask = serializer.ParsePitchesAsync(() => pitchesFile.OpenStreamForReadAsync());
        // parse fixtures to games
        var groupTypes = await groupTypesTask;
        var fixtureFiles = ViewModel.FixtureFiles.Select<StorageFile, (string FileName, Func<Task<Stream>> StreamProvider)>(f => (f.Name, () => f.OpenStreamForReadAsync()));
        var fixtureTask = serializer.ParseFixturesAsync(groupTypes, fixtureFiles);

        var pitches = await pitchesTask;
        var games = await fixtureTask;

        // remove games with SPIELFREI
        // TODO make "SPIELFREI" rule?
        var spielfrei = games.Where(g => g.Home.Name == GamesPlaceholder || g.Away.Name == GamesPlaceholder);
        if (spielfrei.Any())
        {
            logger.LogInformation("Removing {count} with 'SPIELFREI'", spielfrei.Count());
            games = games.Except(spielfrei).ToList();
        }

        // prepare target state rules
        if (targetRuleConfigs != null)
        {
            var targetStateRuleConfigProvider = services.GetRequiredService<TargetStateRuleConfigurationProvider>();
            var targetRules = await targetRuleTask;
            targetStateRuleConfigProvider.GroupTypeConfigs = groupTypes;
            targetStateRuleConfigProvider.RuleConfigs = targetRules;
        }

        // slot by gameday
        var slotService = services.GetRequiredService<ISlotService>();
        var pitchesOrdered = pitches.GroupBy(p => p.GameDay).OrderBy(g => g.Key);
        var gameplanDtos = new List<GameplanGameDto>(games.Count);
        List<GameDay> gameDays = new(pitchesOrdered.Count());
        foreach (var gameDayPitches in pitchesOrdered)
        {
            List<Pitch> pitchesToSlot = [.. gameDayPitches.OrderBy(p => rng.NextInt64())];
            var slottedPitches = slotService.SlotGameDay(
                pitchesToSlot,
                games.Where(g => g.GameDay == gameDayPitches.Key).ToList()
            );
            gameDays.Add(new GameDay
            {
                GameDayID = gameDayPitches.Key,
                Pitches = slottedPitches
            });

            // gamplan dtos for statistics
            foreach (var pitch in slottedPitches)
            {
                var name = pitch.Name;
                foreach (var slot in pitch.Slots)
                {
                    var game = slot.Game;
                    gameplanDtos.Add(new GameplanGameDto
                    {
                        GameDay = gameDayPitches.Key,
                        Pitch = name,
                        Home = game.Home.Name,
                        Away = game.Away.Name,
                        Referee = game.Referee?.Name,
                        StartTime = slot.StartTime,
                        EndTime = slot.EndTime,
                        Group = game.Group.Name,
                        League = game.Group.Type.Name,
                    });
                }
            }
        }

        // write to file
        ViewModel.GameplanFile = await ViewModel.WorkDir.CreateFileAsync(MainPageViewModel.FileNamePrefixes.Gameplan + ".csv", CreationCollisionOption.GenerateUniqueName);
        await serializer.WriteCsvGameplanAsync(() => ViewModel.GameplanFile.OpenStreamForWriteAsync(), gameDays);

        await StoreRng(rng, logger);
        await GenerateStatsAsync();
    }


    #endregion

    #region Migrations
    private void LookingForMigrationFile(IReadOnlyList<StorageFile> files)
    {
        ViewModel.MigrationFile = files.FirstOrDefault(f => f.Name.StartsWith("migration", StringComparison.InvariantCultureIgnoreCase) && f.FileType == ".csv");
    }

    private async void RunMigrationsButton_Click(object sender, RoutedEventArgs e)
    {
        var migrationService = App.Current.Services.GetRequiredService<IMigrationService>();
        var serializerService = App.Current.Services.GetRequiredService<CsvSerializerService>();

        var gamePlanTask = serializerService.ParseGameplanAsync(() => ViewModel.GameplanFile.OpenStreamForReadAsync());
        var migrationsTask = CsvSerializerService.ParseMigrationsAsync(() => ViewModel.MigrationFile.OpenStreamForReadAsync());
        await Task.WhenAll(migrationsTask, gamePlanTask);

        var updatedGamePlan = await migrationService.RunMigrations(migrationsTask.Result, gamePlanTask.Result);
        using var writeStream = await ViewModel.GameplanFile.OpenStreamForWriteAsync();
        await CsvSerializerService.WriteCsvGameplanAsync(writeStream, updatedGamePlan);
    }

    #endregion

    #region RNG
    /// <summary>
    /// Loading last seed from .rngseed file, if exists. Otherwise setting the default seed.
    /// </summary>
    private async void LookingForRngSeedFile(IReadOnlyList<StorageFile> files)
    {

        var services = App.Current.Services;
        var rng = services.GetRequiredService<IRngProvider>();

        ViewModel.RngSeedFile = files.FirstOrDefault(f => f.Name == ".rngseed");
        var seedfile = ViewModel.RngSeedFile;
        if (seedfile == null)
        {
            ViewModel.RngSeed = rng.CurrentSeed;
            return;
        }

        var lines = await FileIO.ReadLinesAsync(seedfile);
        if (int.TryParse(lines.LastOrDefault(), out var lastSeed))
        {
            rng.Reset(lastSeed);
        }
        else
        {
            var logger = services.GetRequiredService<ILogger<MainPage>>();
            logger.LogError("Found rng seed file, but couldn't read last. Using default.");
        }
        ViewModel.RngSeed = rng.CurrentSeed;
    }

    private void RandomizePlanButton_Click(object sender, RoutedEventArgs e)
    {
        var rng = App.Current.Services.GetRequiredService<IRngProvider>();
        rng.Clear();
        ViewModel.RngSeed = rng.CurrentSeed;
    }

    private async Task StoreRng(IRngProvider rng, ILogger logger)
    {
        var seed = rng.CurrentSeed;
        if (ViewModel.RngSeedFile is not StorageFile seedfile)
        {
            seedfile = await ViewModel.WorkDir.CreateFileAsync(".rngseed", CreationCollisionOption.OpenIfExists);
        }

        /* 
         * Sometimes there is an access exception when writing the file, even though StorageFile.IsAvailable == true.
         * Usually I don't like using the "while - catch exception" combo, but I don't know of any other way in this case.
         */
        const int maxTries = 5;
        int tries = 0;
        while (tries++ < maxTries)
        {
            try
            {
                await FileIO.AppendLinesAsync(seedfile, [seed.ToString()]);
                return;
            }
            catch (System.Runtime.InteropServices.COMException)
            {
                await Task.Delay(100);
            }
        }
        logger.LogError("Could not access seedfile after {max} tries.", maxTries);
    }
    #endregion

    #region Team Files
    private void LookingForTeamFiles(IReadOnlyList<StorageFile> storageFiles)
    {
        if (ViewModel.IsPreventRescanForTeamFiles)
        {
            ViewModel.IsPreventRescanForTeamFiles = false;
            return;
        }

        ViewModel.GenerateFixtursButton_HasGenerated = false;
        ViewModel.TeamFiles.Clear();

        var teamFiles = storageFiles.Where(s => s.Name.StartsWith(MainPageViewModel.FileNamePrefixes.Teams, StringComparison.InvariantCultureIgnoreCase));
        foreach (var file in teamFiles)
        {
            ViewModel.TeamFiles.Add(new ConfigFileRecordViewModel
            {
                IsFound = true,
                File = file,
            });
        }

        var hasTeamFiles = ViewModel.TeamFiles.Any();
        ViewModel.GenerateFixtursButton_IsEnabled = hasTeamFiles;
        if (!hasTeamFiles)
        {
            ViewModel.ResetTeamFiles();
        }
    }

    private async void GenerateFixtursButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.GenerateFixtursButton_HasGenerated = false;
        ViewModel.GenerateFixtursButton_IsGenerating = true;

        var fixtureGenerator = App.Current.Services.GetRequiredService<GeneratorService>();
        foreach (var file in ViewModel.TeamFiles.Select(tf => tf.File))
        {
            var teams = await FileIO.ReadLinesAsync(file);
            var fixtures = fixtureGenerator.Fix([.. teams], GamesPlaceholder);
            var csv = fixtures.Select(g => g.GameDay + "," + g.Home + "," + g.Away);

            var fixtureFile = await ViewModel.WorkDir.CreateFileAsync(
                "Fixtures_" + Path.ChangeExtension(file.Name, "csv"),
                CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteLinesAsync(fixtureFile, csv);
        }

        ViewModel.GenerateFixtursButton_IsGenerating = false;
        ViewModel.GenerateFixtursButton_HasGenerated = true;
        ViewModel.IsPreventRescanForTeamFiles = true;
    }
    #endregion

    #region Stats

    private async void GenerateStatsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ViewModel.GenerateStatsButton_HasGenerated = false;
            ViewModel.GenerateStatsButton_IsGenerating = true;

            await GenerateStatsAsync();

            ViewModel.GenerateStatsButton_HasGenerated = true;
            ViewModel.GenerateStatsButton_IsGenerating = false;
        }
        catch
        {
            ViewModel.GenerateStatsButton_HasGenerated = false;
            ViewModel.GenerateStatsButton_IsGenerating = false;
            throw;
        }
    }

    private async Task GenerateStatsAsync()
    {

        var serializer = App.Current.Services.GetRequiredService<CsvSerializerService>();
        var gameDtos = await serializer.ParseGameplanAsync(ViewModel.GameplanFile.OpenStreamForReadAsync);

        var configuration = App.Current.Services.GetRequiredService<IConfiguration>();
        var morningUntil = configuration.GetValue<TimeOnly>("Schedule:MorningUntil");
        var eveningSince = configuration.GetValue<TimeOnly>("Schedule:EveningSince");

        var teams = gameDtos
            .SelectMany(g => new[] { new TeamStatsDto { League = g.League, Name = g.Home },
                new TeamStatsDto { League = g.League, Name = g.Away } })
            .DistinctBy(x => x.Name)
            .ToDictionary(x => x.Name, x => x);

        foreach (var game in gameDtos)
        {
            if (game.Referee != null && teams.TryGetValue(game.Referee, out var refTeam))
            {
                // refs are sometimes optional or have a placeholder name
                refTeam.Referee++;
            }

            if (game.StartTime < morningUntil)
            {
                teams[game.Home].MorningGames++;
                teams[game.Away].MorningGames++;
            }
            else if (game.StartTime > eveningSince)
            {
                teams[game.Home].EveningGames++;
                teams[game.Away].EveningGames++;
            }
        }

        var name = Path.GetFileNameWithoutExtension(ViewModel.GameplanFile.Name) + StatsFileSuffix;
        var statsFile = await ViewModel.WorkDir.CreateFileAsync(name, CreationCollisionOption.ReplaceExisting);
        await serializer.WriteCsvStatsAsync(() => statsFile.OpenStreamForWriteAsync(), teams.Values.OrderBy(v => v.League).ThenBy(v => v.Name));

        ViewModel.IsPreventRescanForGameplanFile = true;
    }
    #endregion

    #region Appworks

    private void LookingForAppworksMappingsFiles(IReadOnlyList<StorageFile> files)
    {
        if (ViewModel.IsPreventRescanForAppworksMappings)
        {
            ViewModel.IsPreventRescanForAppworksMappings = false;
            return;
        }

        ViewModel.GenerateAppworksImportButton_HasGenerated = false;
        ViewModel.AppworksMappingsFiles.Clear();

        var teamFiles = files.Where(s => s.FileType == ".csv" && s.Name.StartsWith(MainPageViewModel.FileNamePrefixes.AppworksMappings, StringComparison.InvariantCultureIgnoreCase));
        foreach (var file in teamFiles)
        {
            ViewModel.AppworksMappingsFiles.Add(new ConfigFileRecordViewModel
            {
                IsFound = true,
                File = file,
            });
        }

        var hasMappingsFiles = ViewModel.AppworksMappingsFiles.Any();
        ViewModel.GenerateAppworksImportButton_IsEnabled = hasMappingsFiles;
        if (!hasMappingsFiles)
        {
            ViewModel.ResetMappingsFiles();
        }
    }

    private void AppworksOpenGameplanButton_Click(object sender, RoutedEventArgs e)
    {
        OpenGameplanButton_Click(sender, e);
    }

    private async void GenerateAppworksImportButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ViewModel.GenerateAppworksImportButton_HasGenerated = false;
            ViewModel.GenerateAppworksImportButton_IsGenerating = true;

            await GenerateAppworksImportFile();

            ViewModel.GenerateAppworksImportButton_HasGenerated = true;
            ViewModel.GenerateAppworksImportButton_IsGenerating = false;
        }
        catch
        {
            ViewModel.GenerateAppworksImportButton_IsGenerating = false;
            throw;
        }
    }


    /// <summary>
    /// Generates the Appworks import file. 
    /// This method reads the gameplan file and the appworks mappings files, and generates an import 
    /// file per mappings file (a.k.a. "tournament").
    /// </summary>
    private async Task GenerateAppworksImportFile()
    {
        var services = App.Current.Services;

        var gamePlanParser = services.GetRequiredService<CsvSerializerService>();
        var gamePlan = await gamePlanParser.ParseGameplanAsync(ViewModel.GameplanFile.OpenStreamForReadAsync);

        var logger = services.GetRequiredService<ILogger<MainPage>>();
        foreach (var mappings in ViewModel.AppworksMappingsFiles)
        {
            var mappingsFilePath = mappings.File.Path;
            var fileName = Path.GetFileNameWithoutExtension(mappingsFilePath);
            var fileNameAr = fileName.Split('_');
            if (fileNameAr.Length < 2)
            {
                logger.LogError("Invalid mappings file name: {FileName}", fileName);
                continue;
            }
            var tournament = fileNameAr[1];

            logger.LogInformation("Generating Appworks import file for tournament {Tournament}", tournament);
            var transformer = services.GetRequiredService<AppworksTransformerFactory>().CreateTransformer(mappingsFilePath);
            var transformedRecordsByTournament = await transformer.Transform(gamePlan, tournament);

            var name = String.Join('_', Path.GetFileNameWithoutExtension(ViewModel.GameplanFile.Name), tournament, AppworksImportFileSuffix);
            var importFile = await ViewModel.WorkDir.CreateFileAsync(name, CreationCollisionOption.ReplaceExisting);

            var serializer = services.GetRequiredService<IAppworksSerializer>();
            await serializer.WriteCsvImportFile(importFile.OpenStreamForWriteAsync, transformedRecordsByTournament[tournament]);
        }
    }

    #endregion

    #region Pdf Generation

    private void LookingForPdfGenerationFiles(IReadOnlyList<StorageFile> files)
    {
        if (ViewModel.IsPreventRescanForPdfGenerationFiles)
        {
            ViewModel.IsPreventRescanForPdfGenerationFiles = false;
            return;
        }

        ViewModel.GeneratePdfButton_HasGenerated = false;
        ViewModel.PdfGenerationFiles.Clear();

        var generatePdfFiles = files.Where(s => s.FileType == ".csv" && s.Name.StartsWith(MainPageViewModel.FileNamePrefixes.PdfGenerationHolidays, StringComparison.InvariantCultureIgnoreCase));
        foreach (var file in generatePdfFiles)
        {
            ViewModel.PdfGenerationFiles.Add(new ConfigFileRecordViewModel
            {
                IsFound = true,
                File = file,
            });
        }

        var hasMappingsFiles = ViewModel.PdfGenerationFiles.Any();
        ViewModel.GeneratePdfButton_IsEnabled = hasMappingsFiles;
        if (!hasMappingsFiles)
        {
            ViewModel.ResetPdfGenerationsFiles();
        }
    }

    private async void GeneratePdfButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ViewModel.GeneratePdfButton_HasGenerated = false;
            ViewModel.GeneratePdfButton_IsGenerating = true;

            await GeneratePdfAsync();

            ViewModel.GeneratePdfButton_IsGenerating = false;
            ViewModel.GeneratePdfButton_HasGenerated = true;
        }
        catch
        {
            ViewModel.GeneratePdfButton_IsGenerating = false;
            throw;
        }
    }

    private async Task GeneratePdfAsync()
    {
        // Local functions faster than lambda: https://stackoverflow.com/questions/40943117/local-function-vs-lambda-c-sharp-7-0
        Task<Stream> gamePlanStream() => ViewModel.GameplanFile.OpenStreamForReadAsync();
        Task<Stream?>? holidaysStream() => ViewModel.PdfGenerationFiles.FirstOrDefault(f => f.IsFound
                && f.File.Name.StartsWith(MainPageViewModel.FileNamePrefixes.PdfGenerationHolidays)
            )?.File.OpenStreamForReadAsync();

        var outputFile = await ViewModel.WorkDir.CreateFileAsync("Spielplan.pdf", CreationCollisionOption.ReplaceExisting);
        Task<Stream> outputStream() => outputFile.OpenStreamForWriteAsync();

        var pdfGenerator = App.Current.Services.GetRequiredService<PdfGenerator>();
        await pdfGenerator.GenerateAsync(outputStream, gamePlanStream, holidaysStream);
    }

    private void PdfOpenGameplanButton_Click(object sender, RoutedEventArgs e)
    {
        OpenGameplanButton_Click(sender, e);
    }
    #endregion

#pragma warning restore CS8602 // Dereference of a possibly null reference.
}