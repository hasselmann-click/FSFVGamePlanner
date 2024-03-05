namespace FSFV.Gameplanner.Appworks.Serialization;

public interface IAppworksSerializer
{
    Task WriteCsvImportFile(Func<Task<Stream>> writeStreamProvider, List<AppworksImportRecord> records);
}