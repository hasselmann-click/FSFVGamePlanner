using Microsoft.UI.Dispatching;
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
        public const string Teams = "teams";
        public const string Fixtures = "fixtures";
        public const string Gameplan = "matchplan";
        public const string AppworksMappings = "mappings";
        public const string PdfGenerationHolidays = "holidays";

        public static class DisplayNames
        {
            public const string Teams = "teams*[League]_[Group].csv";
            public const string Fixtures = "fixtures*[League]_[Group].csv";
            public const string Gameplan = "matchplan*.csv";
            public const string Pitches = "pitches.csv";
            public const string LeagueConfigs = "league_configs.csv";
            public const string AppworksMappings = "mappings*_[League].csv";
            public const string PdfGenerationHolidays = "holidays.csv";
        }
    }

    public MainPageViewModel(DispatcherQueue dispatcher)
    {
        ResetConfigFileRecords();
        ResetTeamFiles();
        ResetMappingsFiles();
        ResetPdfGenerationsFiles();
        Dispatcher = dispatcher;
    }

    private StorageFile gameplanFile;
    private StorageFolder workDir;

    private bool generateFixtursButton_IsEnabled;
    private bool generateFixtursButton_IsGenerating;
    private bool generateFixtursButton_HasGenerated;
    private bool generateGameplanButton_IsEnabled;
    private bool generateGameplanButton_IsGenerating;
    private bool generateGameplanButton_HasGenerated;
    private bool generateStatsButton_IsGenerating;
    private bool generateStatsButton_HasGenerated;
    private bool generateAppworksImportButton_IsGenerating;
    private bool generateAppworksImportButton_HasGenerated;
    private bool generateAppworksImportButton_IsEnabled;
    private bool generatePdfButton_IsEnabled;
    private bool generatePdfButton_IsGenerating;
    private bool generatePdfButton_HasGenerated;

    public StorageFolder WorkDir
    {
        get => workDir;
        set => SetProperty(ref workDir, value, nameof(WorkDirPath));
    }
    public string WorkDirPath => WorkDir?.Path;

    #region Gameplan
    public StorageFile GameplanFile
    {
        get => gameplanFile;
        set
        {
            SetProperty(ref gameplanFile, value, nameof(GameplanFile));

            // all properties that are affected by the change of this property
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasGameplanFile)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NotHasGameplanFile)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GameplanFileName)));

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GenerateStatsButton_IsEnabled)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GenerateAppworksImportButton_IsEnabled)));
        }
    }
    public bool NotHasGameplanFile => !HasGameplanFile;
    public bool HasGameplanFile => GameplanFile != null;
    public string GameplanFileName => GameplanFile?.Name ?? FileNamePrefixes.DisplayNames.Gameplan;

    public ObservableCollection<ConfigFileRecordViewModel> TeamFiles { get; } = [];
    public ObservableCollection<ConfigFileRecordViewModel> ConfigFileRecords { get; } = [];
    public ObservableCollection<StorageFile> FixtureFiles { get; } = new(new List<StorageFile>(4));
    #endregion

    #region buttons
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

    #endregion

    #region stats
    public bool GenerateStatsButton_IsEnabled => HasGameplanFile && !GenerateStatsButton_IsGenerating;
    public bool GenerateStatsButton_IsGenerating
    {
        get => generateStatsButton_IsGenerating;
        set
        {
            SetProperty(ref generateStatsButton_IsGenerating, value);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GenerateStatsButton_IsEnabled)));
        }
    }
    public bool GenerateStatsButton_HasGenerated
    {
        get => generateStatsButton_HasGenerated;
        set => SetProperty(ref generateStatsButton_HasGenerated, value);
    }
    #endregion

    #region Appworks Import
    public bool GenerateAppworksImportButton_IsEnabled
    {
        get => generateAppworksImportButton_IsEnabled;
        set => SetProperty(ref generateAppworksImportButton_IsEnabled, value);
    }
    public bool GenerateAppworksImportButton_IsGenerating
    {
        get => generateAppworksImportButton_IsGenerating;
        set
        {
            SetProperty(ref generateAppworksImportButton_IsGenerating, value);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GenerateAppworksImportButton_IsEnabled)));
        }
    }
    public bool GenerateAppworksImportButton_HasGenerated
    {
        get => generateAppworksImportButton_HasGenerated;
        set => SetProperty(ref generateAppworksImportButton_HasGenerated, value);
    }

    public ObservableCollection<ConfigFileRecordViewModel> AppworksMappingsFiles { get; } = [];

    public void ResetMappingsFiles()
    {
        AppworksMappingsFiles.Clear();
        AppworksMappingsFiles.Add(new ConfigFileRecordViewModel { PreviewDisplayName = FileNamePrefixes.DisplayNames.AppworksMappings, IsFound = false });
    }

    #endregion

    #region Pdf Generation

    public bool GeneratePdfButton_IsEnabled
    {
        get => generatePdfButton_IsEnabled;
        set => SetProperty(ref generatePdfButton_IsEnabled, value);
    }
    public bool GeneratePdfButton_IsGenerating
    {
        get => generatePdfButton_IsGenerating;
        set
        {
            SetProperty(ref generatePdfButton_IsGenerating, value);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GeneratePdfButton_IsEnabled)));
        }
    }
    public bool GeneratePdfButton_HasGenerated
    {
        get => generatePdfButton_HasGenerated;
        set => SetProperty(ref generatePdfButton_HasGenerated, value);
    }
    public ObservableCollection<ConfigFileRecordViewModel> PdfGenerationFiles { get; } = [];
    public void ResetPdfGenerationsFiles()
    {
        PdfGenerationFiles.Clear();
        PdfGenerationFiles.Add(new ConfigFileRecordViewModel { PreviewDisplayName = FileNamePrefixes.DisplayNames.PdfGenerationHolidays, IsFound = false });
    }

    #endregion

    #region configs 
    public void ResetConfigFileRecords()
    {
        ConfigFileRecords.Clear();
        foreach (var record in CreateInitialConfigRecords)
        {
            ConfigFileRecords.Add(record);
        }
    }

    private static List<ConfigFileRecordViewModel> CreateInitialConfigRecords => new(4 + 2)
    {
        new ConfigFileRecordViewModel { PreviewDisplayName = FileNamePrefixes.DisplayNames.Pitches, IsFound = false },
        new ConfigFileRecordViewModel { PreviewDisplayName = FileNamePrefixes.DisplayNames.LeagueConfigs, IsFound = false },
        new ConfigFileRecordViewModel { PreviewDisplayName = FileNamePrefixes.DisplayNames.Fixtures, IsFound = false},
    };

    public void ResetTeamFiles()
    {
        TeamFiles.Clear();
        TeamFiles.Add(new ConfigFileRecordViewModel { PreviewDisplayName = FileNamePrefixes.DisplayNames.Teams, IsFound = false });
    }

    public DispatcherQueue Dispatcher { get; }
    public bool IsPreventRescanForGameplanFile { get; internal set; }
    public bool IsPreventRescanForTeamFiles { get; internal set; }
    public bool IsPreventRescanForAppworksMappings { get; internal set; }
    public bool IsPreventRescanForPdfGenerationFiles { get; internal set; }

    #endregion
}
