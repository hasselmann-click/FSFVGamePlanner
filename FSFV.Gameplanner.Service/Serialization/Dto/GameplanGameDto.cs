using CsvHelper.Configuration.Attributes;
using System;

namespace FSFV.Gameplanner.Service.Serialization.Dto;

public class GameplanGameDto
{
    [Ignore]
    public int GameDay { get; set; }
    public string Pitch { get; set; }

    [Format("hh:mm")]
    public TimeOnly StartTime { get; set; }
    
    [Format("hh:mm")]
    public TimeOnly EndTime { get; set; }
    public string Home { get; set; }
    public string Away { get; set; }
    public string? Referee { get; set; }
    public string Group { get; set; }
    public string League { get; set; }

    [Format("dd.MM.yyyy")]
    public DateOnly Date { get; set; }
}

