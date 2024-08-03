using System.Collections.Generic;

namespace FSFV.Gameplanner.Service.Slotting.RuleBased.Rules.RefereeUpdate;
internal class RefereeUpdateGroupConfig
{
    public bool HasReferees { get; set; } = true;
    public bool SkipRefereeOnFirstDay { get; set; } = true;
}


