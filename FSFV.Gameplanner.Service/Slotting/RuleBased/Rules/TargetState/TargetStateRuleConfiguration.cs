using System;

namespace FSFV.Gameplanner.Service.Slotting.RuleBased.Rules.TargetState;

public class TargetStateRuleConfiguration
{
    public Current Filter { get; set; }
    public Target Applicator { get; set; }

    public record Current(int? Round, TimeOnly? Time, DateOnly? Date, string? Pitch);
    public record Target(string? Team, TimeOnly? Time, string? League);

}