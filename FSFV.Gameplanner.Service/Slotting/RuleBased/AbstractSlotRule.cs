using FSFV.Gameplanner.Common;
using FSFV.Gameplanner.Service.Slotting;
using System.Collections.Generic;

namespace FSFV.Gameplanner.Service.Slotting.RuleBased;

internal abstract class AbstractSlotRule(int priority) : ISlotRule
{
    public abstract IEnumerable<Game> Apply(SlottingContext context, Pitch pitch, IEnumerable<Game> games, List<Pitch> pitches);
    public int GetPriority() => priority;
    public virtual void Update(SlottingContext context, Pitch pitch, Game game)
    {
        /* do nothing */
    }
    public virtual void ProcessAfterGameday(SlottingContext context, List<Pitch> pitches)
    {
        /* do nothing */
    }
    public virtual void ProcessBeforeGameday(SlottingContext context, List<Pitch> pitches, List<Game> games)
    {
        /* do nothing */
    }
}
