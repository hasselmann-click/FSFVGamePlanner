// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using FSFV.Gameplanner.Appworks;
using FSFV.Gameplanner.Appworks.Serialization;
using FSFV.Gameplanner.Common;
using FSFV.Gameplanner.Fixtures;
using FSFV.Gameplanner.Service.Serialization;
using FSFV.Gameplanner.Service.Slotting;
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
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using Windows.Storage.Search;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace FSFV.Gameplanner.UI.Pages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainPage : Page
{
    private const string FolderAccessToken = "PickedFolderToken";

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
    }

    #region Folder Picker
    private StorageFileQueryResult query = null;
    private Timer folderContentsChangedThrottleTimer = null;

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
        StorageApplicationPermissions.FutureAccessList.AddOrReplace(FolderAccessToken, folder);
        await ChangeWorkFolder(folder);
    }

    private void OnFolderContentChanged(IStorageQueryResultBase sender, object args)
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

        // TODO refactor to property dependency in view model
        ViewModel.GenerateGameplanButton_IsEnabled = ViewModel.ConfigFileRecords.All(r => r.IsFound)
            && fixtureFiles.Any();
    }

    private async void GeneratePlanButton_Click(object sender, RoutedEventArgs e)
    {
        // TODO ERROR HANDLING > Show error message on exception?

        ViewModel.GenerateGameplanButton_HasGenerated = false;
        ViewModel.GenerateGameplanButton_IsGenerating = true;
        ViewModel.GenerateGameplanButton_IsEnabled = false;

        var serializer = App.Current.Services.GetRequiredService<FsfvCustomSerializerService>();
        // parse league configs to group type dto
        var leagueConfigs = ViewModel.ConfigFileRecords.First(r => r.PreviewDisplayName == MainPageViewModel.FileNamePrefixes.DisplayNames.LeagueConfigs).File;
        var groupTypesTask = serializer.ParseGroupTypesAsync(() => leagueConfigs.OpenStreamForReadAsync());
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
        var logger = App.Current.Services.GetRequiredService<ILogger<MainPage>>();
        var spielfrei = games.Where(g => g.Home.Name == GamesPlaceholder || g.Away.Name == GamesPlaceholder);
        if (spielfrei.Any())
        {
            logger.LogInformation("Removing {count} with 'SPIELFREI'", spielfrei.Count());
            games = games.Except(spielfrei).ToList();
        }
        // slot by gameday
        var slotService = App.Current.Services.GetRequiredService<ISlotService>();
        var pitchesOrdered = pitches.GroupBy(p => p.GameDay).OrderBy(g => g.Key);
        var gameplanDtos = new List<FsfvCustomSerializerService.GameplanGameDto>(games.Count);
        List<GameDay> gameDays = new(pitchesOrdered.Count());
        foreach (var gameDayPitches in pitchesOrdered)
        {
            var slottedPitches = slotService.SlotGameDay(
                gameDayPitches.ToList(),
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
                    gameplanDtos.Add(new FsfvCustomSerializerService.GameplanGameDto
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
        
        // todo bhas test this
        await GenerateStatsAsync();

        ViewModel.GenerateGameplanButton_IsGenerating = false;
        ViewModel.GenerateGameplanButton_HasGenerated = true;
        ViewModel.GenerateGameplanButton_IsEnabled = true;
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
            var fixtures = fixtureGenerator.Fix(teams.ToArray(), GamesPlaceholder);
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
        ViewModel.GenerateStatsButton_HasGenerated = false;
        ViewModel.GenerateStatsButton_IsGenerating = true;

        await GenerateStatsAsync();

        ViewModel.GenerateStatsButton_HasGenerated = true;
        ViewModel.GenerateStatsButton_IsGenerating = false;

    }

    private async Task GenerateStatsAsync()
    {

        var serializer = App.Current.Services.GetRequiredService<FsfvCustomSerializerService>();
        var gameDtos = await serializer.ParseGameplanAsync(ViewModel.GameplanFile.OpenStreamForReadAsync);

        var configuration = App.Current.Services.GetRequiredService<IConfiguration>();
        var morningUntil = configuration.GetValue<TimeSpan>("Schedule:MorningUntil");
        var eveningSince = configuration.GetValue<TimeSpan>("Schedule:EveningSince");

        var teams = gameDtos
            .SelectMany(g => new[] { new FsfvCustomSerializerService.TeamStatsDto { League = g.League, Name = g.Home }, 
                new FsfvCustomSerializerService.TeamStatsDto { League = g.League, Name = g.Away } })
            .DistinctBy(x => x.Name)
            .ToDictionary(x => x.Name, x => x);

        foreach (var game in gameDtos)
        {
            if (game.Referee != null && teams.TryGetValue(game.Referee, out var refTeam))
            {
                // refs are sometimes optional or have a placeholder name
                refTeam.Referee++;
            }

            if (game.StartTime.TimeOfDay < morningUntil)
            {
                teams[game.Home].MorningGames++;
                teams[game.Away].MorningGames++;
            }
            else if (game.EndTime.TimeOfDay < eveningSince)
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

    private void AppworksOpenGameplanButton_Click(object sender, RoutedEventArgs e)
    {
        OpenGameplanButton_Click(sender, e);
    }

    private async void GenerateAppworksImportButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.GenerateAppworksImportButton_HasGenerated = false;
        ViewModel.GenerateAppworksImportButton_IsGenerating = true;

        await GenerateAppworksImportFile();

        ViewModel.GenerateAppworksImportButton_HasGenerated = true;
        ViewModel.GenerateAppworksImportButton_IsGenerating = false;
    }

    private async Task GenerateAppworksImportFile()
    {
        // todo bhas make this configurable
        var tournament = "M";

        var services = App.Current.Services;

        var gamePlanParser = services.GetRequiredService<FsfvCustomSerializerService>();
        var gamePlan = await gamePlanParser.ParseGameplanAsync(ViewModel.GameplanFile.OpenStreamForReadAsync);

        var transformer = services.GetRequiredService<AppworksTransformer>();
        var transformedRecordsByTournament = await transformer.Transform(gamePlan, tournament);

        var name = Path.GetFileNameWithoutExtension(ViewModel.GameplanFile.Name) + AppworksImportFileSuffix;
        var importFile = await ViewModel.WorkDir.CreateFileAsync(name, CreationCollisionOption.ReplaceExisting);

        var serializer = services.GetRequiredService<AppworksSerializer>();
        await serializer.WriteCsvImportFile(importFile.OpenStreamForWriteAsync, transformedRecordsByTournament[tournament]);
    }


    #endregion

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

    private async void OpenGameplanButton_Click(object sender, RoutedEventArgs e)
    {
        FileOpenPicker openPicker = new();
        var hWnd = WindowHelper.GetWindowHandle(this);
        WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);

        // Set options for your folder picker
        openPicker.SuggestedStartLocation = PickerLocationId.Desktop;
        openPicker.FileTypeFilter.Add(".csv");

        // Open the picker for the user to pick a folder
        StorageFile file = await openPicker.PickSingleFileAsync();
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

}