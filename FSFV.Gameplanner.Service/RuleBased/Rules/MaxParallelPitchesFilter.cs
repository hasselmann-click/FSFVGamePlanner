using FSFV.Gameplanner.Common;
using System.Collections.Generic;
using System.Linq;

namespace FSFV.Gameplanner.Service.RuleBased.Rules;

internal class MaxParallelPitchesFilter : AbstractSlotRule
{
    private Dictionary<string, int> maxParallelPitchesByLeague;
    private Dictionary<string, HashSet<string>> currentParallelPitchesByLeague;
    private HashSet<string> isMaxedOut;

    public MaxParallelPitchesFilter(int priority) : base(priority)
    {
    }

    public override void ProcessBeforeGameday(List<Pitch> pitches, List<Game> games)
    {
        maxParallelPitchesByLeague = games
            .Select(g => g.Group.Type)
            .DistinctBy(t => t.Name)
            .Where(t => t.MaxParallelPitches < pitches.Count)
            .ToDictionary(t => t.Name, t => t.MaxParallelPitches);
        currentParallelPitchesByLeague = new Dictionary<string, HashSet<string>>(maxParallelPitchesByLeague.Count);
        isMaxedOut = new HashSet<string>(maxParallelPitchesByLeague.Count);
    }

    public override IEnumerable<Game> Apply(Pitch pitch, IEnumerable<Game> games, List<Pitch> pitches)
    {
        // return games which are either not maxed out (yet) or playing on the pitch already
        // TODO: check if there is enough space left for the league to finish on the maximum number of pitches
        return games.Where(g =>
                !isMaxedOut.Contains(g.Group.Type.Name)
                || currentParallelPitchesByLeague[g.Group.Type.Name].Contains(pitch.Name))
            ;
    }

    public override void Update(Pitch pitch, Game game)
    {
        var league = game.Group.Type.Name;
        if (!maxParallelPitchesByLeague.TryGetValue(league, out var maxParallelPitches))
        {
            return;
        }

        if (currentParallelPitchesByLeague.TryGetValue(league, out var current))
        {
            current.Add(pitch.Name);
            if (current.Count == maxParallelPitches)
            {
                isMaxedOut.Add(league);
            }
        }
        else
        {
            currentParallelPitchesByLeague.Add(league, new HashSet<string>(maxParallelPitches) { pitch.Name });
        }
    }
}
