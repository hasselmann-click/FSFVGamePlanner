using CsvHelper.Configuration.Attributes;
using System;

namespace FSFV.Gameplanner.Service.Slotting.RuleBased.Rules.TargetState;

public class TargetStateRuleConfiguration
{
    public Current Filter { get; set; }
    public Target Applicator { get; set; }

    public record Current
    {
        public int? Round { get; init; }
        public TimeOnly? Time { get; init; }
        [Format("dd.MM.yyyy", "yyyy.MM.dd")]
        public DateOnly? Date { get; init; }
        public string? Pitch { get; init; }

        public void Deconstruct(out int? round, out TimeOnly? time, out DateOnly? date, out string? pitch)
        {
            round = Round;
            time = Time;
            date = Date;
            pitch = Pitch;
        }
    }

    public record Target(string? Team, TimeOnly? Time, string? League);

}