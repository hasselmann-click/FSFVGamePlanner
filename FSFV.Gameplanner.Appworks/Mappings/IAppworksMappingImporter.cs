namespace FSFV.Gameplanner.Appworks.Mappings;

public interface IAppworksMappingImporter
{
    Task<AppworksMappings> ImportMappings();

}