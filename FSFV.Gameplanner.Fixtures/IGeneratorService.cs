namespace FSFV.Gameplanner.Fixtures;

public interface IGeneratorService
{
    List<Fixture> Fix(string[] teams, string placeHolder = "SPIELFREI");
}
