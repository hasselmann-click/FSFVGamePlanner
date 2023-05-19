using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.Storage;

namespace FSFV.Gameplanner.UI.Pages;

public class MainPageViewModel
{
    public MainPageViewModel()
    { }

    public StorageFolder WorkDir { get; set; } = null;
    public ObservableCollection<StorageFile> TeamFiles { get; set; } = new(new List<StorageFile>(4));

}
