// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using FSFV.Gameplanner.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

    private delegate void FolderPickedHandler(IReadOnlyList<StorageFile> files);
    private event FolderPickedHandler OnFolderPicked;

    public MainPageViewModel ViewModel { get; }

    public MainPage()
    {
        this.InitializeComponent();
        ViewModel = new MainPageViewModel();

        OnFolderPicked += LookingForTeamFiles;
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
            foreach (var file in fixtureFiles)
            {
                ViewModel.FixtureFiles.Add(file);
            }
        }
        else
        {
            enablePlanGeneratorButton = false;
        }

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
            var fixtures = fixtureGenerator.Fix(teams.ToArray());
            var csv = fixtures.Select(g => g.GameDay + "," + g.Home + "," + g.Away);

            var fixtureFile = await ViewModel.WorkDir.CreateFileAsync(
                "Fixtures_" + Path.ChangeExtension(file.Name, "csv"),
                CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteLinesAsync(fixtureFile, csv);
        }

        GenerateFixtursButton_Loading.IsActive = false;
        GenerateFixtursButton_Done.Visibility = Visibility.Visible;
    }
    #endregion
}
