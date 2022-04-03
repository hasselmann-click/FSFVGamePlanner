using CsvHelper.Configuration.Attributes;

namespace FSFV.Gameplanner.Common.Dto;

public class FixtureDto
{
    [Index(0)]
    public int GameDay { get; set; }
    [Index(1)]
    public string Home { get; set; }
    [Index(2)]
    public string Away { get; set; }
}
