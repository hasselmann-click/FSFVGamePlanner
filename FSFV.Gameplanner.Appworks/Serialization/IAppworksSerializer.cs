namespace FSFV.Gameplanner.Appworks.Serialization;

public interface IAppworksSerializer
{
    Task WriteCsvImportFile(Stream writeStream, List<AppworksImportRecord> records);
}