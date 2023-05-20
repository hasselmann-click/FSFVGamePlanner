﻿using Windows.Storage;

namespace FSFV.Gameplanner.UI.Pages;

public class ConfigFileRecordViewModel
{
    public string Prefix { get; set; }
    public string DisplayName => Prefix + ".*";
    public bool IsFound { get; set; }
    /// <summary>
    /// Inverse of <see cref="IsFound"/>.<br/>
    /// This is necessary since XAML is purely declarative and does not support inline computations. 
    /// A markup extension or converter could be used instead. But this is plain and simple.
    /// </summary>
    public bool NotIsFound => !IsFound;
    public StorageFile ConfigFile { get; set; }
}
