using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Windows.Storage;

namespace FSFV.Gameplanner.UI.Pages;

public partial class MainPageViewModel : INotifyPropertyChanged
{

    /// <summary>
    /// Stolen from <see cref="MvvmHelpers.ObservableObject.SetProperty{T}(ref T, T, string)"/>.<br/>
    /// <seealso cref="https://github.com/CommunityToolkit/dotnet/blob/main/src/CommunityToolkit.Mvvm/ComponentModel/ObservableObject.cs"/>
    /// </summary>
    protected bool SetProperty<T>([NotNullIfNotNull(nameof(newValue))] ref T field, T newValue, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, newValue))
        {
            return false;
        }

        field = newValue;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public static class FileNamePrefixes
    {
        public const string Pitches = "pitches";
        public const string LeagueConfigs = "league_configs";
        public const string Teams = "teams_";
        public const string Fixtures = "fixtures_";
        public const string Gameplan = "matchplan";
    }

    public MainPageViewModel()
    {
        ResetConfigFileRecords();
    }

    private StorageFile gameplanFile;
    private StorageFolder workDir;
    private bool generateFixtursButton_IsEnabled;
    private bool generateFixtursButton_IsGenerating;
    private bool generateFixtursButton_HasGenerated;
    private bool generateGameplanButton_IsEnabled;
    private bool generateGameplanButton_IsGenerating;
    private bool generateGameplanButton_HasGenerated;

    public StorageFolder WorkDir
    {
        get => workDir;
        set => SetProperty(ref workDir, value, nameof(WorkDirPath));
    }
    public string WorkDirPath => WorkDir?.Path;

    public StorageFile GameplanFile
    {
        get => gameplanFile;
        set
        {
            SetProperty(ref gameplanFile, value, nameof(HasGameplanFile));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GameplanFileName)));
        }
    }
    public bool HasGameplanFile => GameplanFile != null;
    public string GameplanFileName => GameplanFile?.Name;

    public ObservableCollection<StorageFile> TeamFiles { get; set; } = new(new List<StorageFile>(4));
    public ObservableCollection<StorageFile> FixtureFiles { get; set; } = new(new List<StorageFile>(4));
    public ObservableCollection<ConfigFileRecordViewModel> ConfigFileRecords { get; set; } = new();

    public bool GenerateFixtursButton_IsEnabled
    {
        get => generateFixtursButton_IsEnabled;
        set => SetProperty(ref generateFixtursButton_IsEnabled, value);
    }
    public bool GenerateFixtursButton_IsGenerating
    {
        get => generateFixtursButton_IsGenerating;
        set => SetProperty(ref generateFixtursButton_IsGenerating, value);
    }
    public bool GenerateFixtursButton_HasGenerated
    {
        get => generateFixtursButton_HasGenerated;
        set => SetProperty(ref generateFixtursButton_HasGenerated, value);
    }

    public bool GenerateGameplanButton_IsEnabled
    {
        get => generateGameplanButton_IsEnabled;
        set => SetProperty(ref generateGameplanButton_IsEnabled, value);
    }
    public bool GenerateGameplanButton_IsGenerating
    {
        get => generateGameplanButton_IsGenerating;
        set => SetProperty(ref generateGameplanButton_IsGenerating, value);
    }
    public bool GenerateGameplanButton_HasGenerated
    {
        get => generateGameplanButton_HasGenerated;
        set => SetProperty(ref generateGameplanButton_HasGenerated, value);
    }

    public void ResetConfigFileRecords()
    {
        ConfigFileRecords.Clear();
        foreach (var record in InitialConfigRecords)
        {
            ConfigFileRecords.Add(record);
        }
    }

    private static readonly List<ConfigFileRecordViewModel> InitialConfigRecords = new(4 + 2)
    {
            new ConfigFileRecordViewModel { Prefix = FileNamePrefixes.Pitches, IsFound = false },
            new ConfigFileRecordViewModel { Prefix = FileNamePrefixes.LeagueConfigs, IsFound = false },
            new ConfigFileRecordViewModel { Prefix = FileNamePrefixes.Fixtures,IsFound = false},
    };

}
