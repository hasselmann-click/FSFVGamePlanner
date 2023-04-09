using FSFV.Gameplanner.Common;
using System.Collections.Generic;
using System.Linq;

namespace FSFV.Gameplanner.Service.RuleBased.Rules;

internal class LeaguePriorityFilter : AbstractSlotRule
{
    public LeaguePriorityFilter(int priority) : base(priority)
    {
    }

    public override IEnumerable<Game> Apply(Pitch pitch, IEnumerable<Game> games, List<Pitch> pitches)
    {
        var g = games.GroupBy(g => g.Group.Type.Priority);
        var go = g.OrderByDescending(gr => gr.Key);
        // only return games with the highest priority
        return go.FirstOrDefault();
    }

}
