using FSFV.Gameplanner.Common;
using System.Collections.Generic;

namespace FSFV.Gameplanner.Service.Slotting.RuleBased.Rules.ZkStartAndEnd;

public static class GameExtensions
{
    public static bool HasZk(this Game source, IReadOnlySet<string> zkTeams)
    {
        return zkTeams.Contains(source.Home.Name) || zkTeams.Contains(source.Away.Name);
    }
}
