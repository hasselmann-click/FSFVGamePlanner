// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using FSFV.Gameplanner.Common;
using FSFV.Gameplanner.Fixtures;
using FSFV.Gameplanner.Service.Serialization;
using FSFV.Gameplanner.Service.Slotting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Windows.ApplicationModel.Background;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace FSFV.Gameplanner.UI.Pages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainPage : Page
{
    private const string GamesPlaceholder = "SPIELFREI";

    private delegate void FilesChangedHandler(IReadOnlyList<StorageFile> files);
    private event FilesChangedHandler OnFolderPicked;
    private event FilesChangedHandler OnFixturesGenerated;

    public MainPageViewModel ViewModel { get; }

    public MainPage()
    {
        this.InitializeComponent();
        ViewModel = new MainPageViewModel();

        OnFolderPicked += LookingForTeamFiles;
        OnFolderPicked += LookingForConfigFiles;

        OnFixturesGenerated += LookingForConfigFiles;
    }

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
            FolderName.Text = "Abgebrochen.";
            return;
        }

        StorageApplicationPermissions.FutureAccessList.AddOrReplace("PickedFolderToken", folder);
        FolderName.Text = folder.Name;
        ViewModel.WorkDir = folder;

        var files = await folder.GetFilesAsync();
        OnFolderPicked?.Invoke(files);
    }

    #region Config Files
    private void LookingForConfigFiles(IReadOnlyList<StorageFile> storageFiles)
    {
        ViewModel.ResetConfigFileRecords();
        ViewModel.FixtureFiles.Clear();

        var enablePlanGeneratorButton = true;
        var pitchesFile = storageFiles.FirstOrDefault(s => s.Name.StartsWith(MainPageViewModel.FileNamePrefixes.Pitches, StringComparison.InvariantCultureIgnoreCase));
        if (pitchesFile != null)
        {
            var record = ViewModel.ConfigFileRecords.FirstOrDefault(r => r.Prefix == MainPageViewModel.FileNamePrefixes.Pitches);
            record.IsFound = true;
            record.ConfigFile = pitchesFile;
        }
        else
        {
            enablePlanGeneratorButton = false;
        }

        var leagueConfigsFile = storageFiles.FirstOrDefault(s => s.Name.StartsWith(MainPageViewModel.FileNamePrefixes.LeagueConfigs, StringComparison.InvariantCultureIgnoreCase));
        if (leagueConfigsFile != null)
        {
            var record = ViewModel.ConfigFileRecords.FirstOrDefault(r => r.Prefix == MainPageViewModel.FileNamePrefixes.LeagueConfigs);
            record.IsFound = true;
            record.ConfigFile = leagueConfigsFile;
        }
        else
        {
            enablePlanGeneratorButton = false;
        }

        var fixtureFiles = storageFiles.Where(s => s.Name.StartsWith(MainPageViewModel.FileNamePrefixes.Fixtures, StringComparison.InvariantCultureIgnoreCase));
        if (fixtureFiles.Any())
        {
            ViewModel.ConfigFileRecords.Remove(ViewModel.ConfigFileRecords.First(r => r.Prefix == MainPageViewModel.FileNamePrefixes.Fixtures));
            foreach (var file in fixtureFiles)
            {
                ViewModel.FixtureFiles.Add(file);
            }
        }
        else
        {
            enablePlanGeneratorButton = false;
        }

        GeneratePlanButton.IsEnabled = enablePlanGeneratorButton;
    }

    private async void GeneratePlanButton_Click(object sender, RoutedEventArgs e)
    {

        // TODO ERROR HANDLING > Show error message on exception?

        GeneratePlanButton_Done.Visibility = Visibility.Collapsed;
        GeneratePlanButton_Done_Loading.IsActive = true;

        var serializer = App.Current.Services.GetRequiredService<FsfvCustomSerializerService>();
        // parse league configs to group type dto
        var leagueConfigs = ViewModel.ConfigFileRecords.First(r => r.Prefix == MainPageViewModel.FileNamePrefixes.LeagueConfigs).ConfigFile;
        var groupTypesTask = serializer.ParseGroupTypesAsync(() => leagueConfigs.OpenStreamForReadAsync());
        // parse pitches to pitches
        var pitchesFile = ViewModel.ConfigFileRecords.First(r => r.Prefix == MainPageViewModel.FileNamePrefixes.Pitches).ConfigFile;
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
            foreach(var pitch in slottedPitches)
            {
                var name = pitch.Name;
                foreach(var slot in pitch.Slots)
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
        ViewModel.GameplanFile = await ViewModel.WorkDir.CreateFileAsync("matchplan.csv", CreationCollisionOption.GenerateUniqueName);
        await serializer.WriteCsvGameplanAsync(() => ViewModel.GameplanFile.OpenStreamForWriteAsync(), gameDays);
        await GenerateStatsAsync(serializer, gameplanDtos);

        GeneratePlanButton_Done_Loading.IsActive = false;
        GeneratePlanButton_Done.Visibility = Visibility.Visible;
    }
    #endregion

    #region stats
    private async void GenerateStatsButton_Click(object sender, RoutedEventArgs e)
    {
        var serializer = App.Current.Services.GetRequiredService<FsfvCustomSerializerService>();
        
        var gameDtos = await serializer.ParseGameplanAsync(() => ViewModel.GameplanFile.OpenStreamForReadAsync());
        await GenerateStatsAsync(serializer, gameDtos);
    }

    private async Task GenerateStatsAsync(FsfvCustomSerializerService serializer, IEnumerable<FsfvCustomSerializerService.GameplanGameDto> gameDtos)
    {
        // TODO write statistics
        //var name = Path.GetFileNameWithoutExtension(ViewModel.GameplanFile.Name) + "_stats.csv";
        //var statsFile = await ViewModel.WorkDir.CreateFileAsync("stats.csv", CreationCollisionOption.GenerateUniqueName);
        //await serializer.WriteCsvStatsAsync(() => statsFile.OpenStreamForWriteAsync(), gameDtos);
    }
    #endregion

    #region Team Files
    private void LookingForTeamFiles(IReadOnlyList<StorageFile> storageFiles)
    {
        ViewModel.TeamFiles.Clear();
        var teamFiles = storageFiles.Where(s => s.Name.StartsWith("teams_", StringComparison.InvariantCultureIgnoreCase));
        if (!teamFiles.Any())
        {
            GenerateFixtursButton.IsEnabled = false;
            return;
        }
        foreach (var file in teamFiles)
        {
            ViewModel.TeamFiles.Add(file);
        }
        GenerateFixtursButton.IsEnabled = true;
    }

    private async void GenerateFixtursButton_Click(object sender, RoutedEventArgs e)
    {
        // TODO teams file list doesn't apply page resource style
        GenerateFixtursButton_Loading.IsActive = true;
        GenerateFixtursButton_Done.Visibility = Visibility.Collapsed;

        var fixtureGenerator = App.Current.Services.GetRequiredService<GeneratorService>();
        foreach (var file in ViewModel.TeamFiles)
        {
            var teams = await FileIO.ReadLinesAsync(file);
            var fixtures = fixtureGenerator.Fix(teams.ToArray(), GamesPlaceholder);
            var csv = fixtures.Select(g => g.GameDay + "," + g.Home + "," + g.Away);

            var fixtureFile = await ViewModel.WorkDir.CreateFileAsync(
                "Fixtures_" + Path.ChangeExtension(file.Name, "csv"),
                CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteLinesAsync(fixtureFile, csv);
        }

        GenerateFixtursButton_Loading.IsActive = false;
        GenerateFixtursButton_Done.Visibility = Visibility.Visible;

        var updatedFiles = await ViewModel.WorkDir.GetFilesAsync();
        OnFixturesGenerated?.Invoke(updatedFiles);
    }
    #endregion

}