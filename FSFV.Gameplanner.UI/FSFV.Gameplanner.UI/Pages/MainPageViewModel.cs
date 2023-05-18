using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace FSFV.Gameplanner.UI.Pages;

public class MainPageViewModel
{
    public MainPageViewModel()
    { }

    public IEnumerable<StorageFile> TeamFiles { get; set; } = Enumerable.Empty<StorageFile>();  

}
