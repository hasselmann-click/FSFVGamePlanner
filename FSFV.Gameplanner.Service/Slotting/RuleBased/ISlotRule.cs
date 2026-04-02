using FSFV.Gameplanner.Common;
using FSFV.Gameplanner.Service.Slotting;
using System.Collections.Generic;

namespace FSFV.Gameplanner.Service.Slotting.RuleBased;

public interface ISlotRule
{
    /// <summary>
    /// Highest goes first.
    /// </summary>
    /// <returns></returns>
    public int GetPriority();
    /// <summary>
    /// Applies the rule to the game candidates.
    /// </summary>
    /// <param name="pitch">The current pitch to slot</param>
    /// <param name="gameCandidates">The candidate games</param>
    /// <param name="pitches">All pitches in their current state</param>
    /// <returns></returns>
    public IEnumerable<Game> Apply(SlottingContext context, Pitch pitch, IEnumerable<Game> gameCandidates, List<Pitch> pitches);
    /// <summary>
    /// Runs after a the game was chosen to be placed next on the current pitch.
    /// </summary>
    /// <param name="pitch"></param>
    /// <param name="game"></param>
    public void Update(SlottingContext context, Pitch pitch, Game game);
    public void ProcessBeforeGameday(SlottingContext context, List<Pitch> pitches, List<Game> games);
    public void ProcessAfterGameday(SlottingContext context, List<Pitch> pitches);
}
