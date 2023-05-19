using FSFV.Gameplanner.Common;
using System.Collections.Generic;

namespace FSFV.Gameplanner.Service.Slotting.RuleBased;

internal abstract class AbstractSlotRule : ISlotRule
{
    private readonly int priority;

    protected AbstractSlotRule(int priority)
    {
        this.priority = priority;
    }

    public abstract IEnumerable<Game> Apply(Pitch pitch, IEnumerable<Game> games, List<Pitch> pitches);
    public int GetPriority() => priority;
    public virtual void Update(Pitch pitch, Game game)
    {
        /* do nothing */
    }
    public virtual void ProcessAfterGameday(List<Pitch> pitches)
    {
        /* do nothing */
    }
    public virtual void ProcessBeforeGameday(List<Pitch> pitches, List<Game> games)
    {
        /* do nothing */
    }
}
