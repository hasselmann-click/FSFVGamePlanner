using FSFV.Gameplanner.Common.Dto;
using System.Collections.Generic;

namespace FSFV.Gameplanner.Service.Slotting.RuleBased.Rules.TargetState;
public class TargetStateRuleConfigurationProvider
{
    public List<TargetStateRuleConfiguration> RuleConfigs { get; set; }
    public Dictionary<string, GroupTypeDto> GroupTypeConfigs { get; set; }
}
