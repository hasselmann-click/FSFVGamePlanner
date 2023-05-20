using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.Storage;

namespace FSFV.Gameplanner.UI.Pages;

public class MainPageViewModel
{

    public static class FileNamePrefixes
    {
        public const string Pitches = "pitches";
        public const string LeagueConfigs = "league_configs";
        public const string Teams = "teams_";
        public const string Fixtures = "fixtures_";
    }

    public MainPageViewModel()
    {
        ResetConfigFileRecords();
    }

    public StorageFolder WorkDir { get; set; } = null;
    public ObservableCollection<StorageFile> TeamFiles { get; set; } = new(new List<StorageFile>(4));
    public ObservableCollection<StorageFile> FixtureFiles { get; set; } = new(new List<StorageFile>(4));
    public ObservableCollection<ConfigFileRecord> ConfigFileRecords { get; set; } = new();

    public void ResetConfigFileRecords()
    {
        ConfigFileRecords.Clear();
        foreach (var record in InitialConfigRecords)
        {
            ConfigFileRecords.Add(record);
        }
    }

    private static readonly List<ConfigFileRecord> InitialConfigRecords = new(4 + 2)
    {
            new ConfigFileRecord { Prefix = FileNamePrefixes.Pitches, IsFound = false },
            new ConfigFileRecord { Prefix = FileNamePrefixes.LeagueConfigs, IsFound = false },
            new ConfigFileRecord{Prefix = FileNamePrefixes.Fixtures,IsFound = false},
    };

    public class ConfigFileRecord
    {
        public string Prefix { get; set; }
        public bool IsFound { get; set; }
        public StorageFile ConfigFile { get; set; }
    }

}
