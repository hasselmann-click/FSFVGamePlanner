using System;

namespace FSFV.Gameplanner.Service.Slotting.RuleBased.Rules.TargetState;

public class TargetStateRuleConfiguration
{
    public Current FilterBy { get; set; }
    public Target ToApply { get; set; }

    public record Current(string? Team, int? Round, TimeOnly? Time, DateOnly? Date, string? Pitch);
    public record Target(string? Team, TimeOnly? Time, string? League);

}