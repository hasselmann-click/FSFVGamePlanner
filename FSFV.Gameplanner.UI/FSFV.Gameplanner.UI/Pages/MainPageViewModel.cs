using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Windows.Storage;

namespace FSFV.Gameplanner.UI.Pages;

public partial class MainPageViewModel : INotifyPropertyChanged
{

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
    public StorageFolder WorkDir
    {
        get => workDir;
        set
        {
            workDir = value;
            //PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(WorkDir)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(WorkDirPath)));
        }
    }
    public string WorkDirPath => WorkDir?.Path;

    public StorageFile GameplanFile
    {
        get => gameplanFile;
        set
        {
            gameplanFile = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasGameplanFile)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GameplanFileName)));
        }
    }
    public bool HasGameplanFile => GameplanFile != null;
    public string GameplanFileName => GameplanFile?.Name;

    public ObservableCollection<StorageFile> TeamFiles { get; set; } = new(new List<StorageFile>(4));
    public ObservableCollection<StorageFile> FixtureFiles { get; set; } = new(new List<StorageFile>(4));
    public ObservableCollection<ConfigFileRecordViewModel> ConfigFileRecords { get; set; } = new();

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
