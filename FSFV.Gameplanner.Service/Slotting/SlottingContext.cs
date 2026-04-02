using System;
using System.Collections.Generic;
using FSFV.Gameplanner.Common.Dto;
using FSFV.Gameplanner.Service.Slotting.RuleBased.Rules.RefereeUpdate;
using FSFV.Gameplanner.Service.Slotting.RuleBased.Rules.TargetState;

namespace FSFV.Gameplanner.Service.Slotting;

public class SlottingContext
{
    public static SlottingContext Empty { get; } = new();

    public IReadOnlyList<TargetStateRuleConfiguration>? TargetStateRules { get; init; }
    public IReadOnlyDictionary<string, GroupTypeDto>? GroupTypeConfigs { get; init; }
    public TimeOnly? MorningUntil { get; init; }
    public TimeOnly? EveningSince { get; init; }
    public IReadOnlySet<string>? ZkTeams { get; init; }
    public IReadOnlyDictionary<string, RefereeUpdateGroupConfig>? RefereeUpdate { get; init; }

    /// <summary>
    /// Expected (homeId, awayId, groupId, gameDay) tuples. Only populated during validation;
    /// null during normal generation. Used by <see cref="FSFV.Gameplanner.Service.Slotting.RuleBased.Rules.ConsistencyChecksRule"/>.
    /// </summary>
    public IReadOnlyList<(string HomeId, string AwayId, string GroupId, int GameDay)>? ExpectedFixtures { get; init; }
}