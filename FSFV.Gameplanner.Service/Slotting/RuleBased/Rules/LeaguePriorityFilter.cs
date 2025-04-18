using FSFV.Gameplanner.Common;
using System.Collections.Generic;
using System.Linq;

namespace FSFV.Gameplanner.Service.Slotting.RuleBased.Rules;

internal class LeaguePriorityFilter(int priority) : AbstractSlotRule(priority)
{
    public override IEnumerable<Game> Apply(Pitch pitch, IEnumerable<Game> games, List<Pitch> pitches)
    {
        var g = games.GroupBy(g => g.Group.Type.Priority);
        var go = g.OrderByDescending(gr => gr.Key);
        // only return games with the highest priority
        return go.FirstOrDefault() ?? games;
    }

}
