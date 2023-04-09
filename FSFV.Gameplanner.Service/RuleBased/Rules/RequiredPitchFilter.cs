using FSFV.Gameplanner.Common;
using System.Collections.Generic;
using System.Linq;

namespace FSFV.Gameplanner.Service.RuleBased.Rules;

internal class RequiredPitchFilter : AbstractSlotRule
{
    private Dictionary<string, string> requiredPitchByLeague;

    public RequiredPitchFilter(int priority) : base(priority)
    {
    }

    public override void ProcessBeforeGameday(List<Pitch> pitches, List<Game> games)
    {
        requiredPitchByLeague = games
            .Select(g => g.Group.Type)
            .DistinctBy(t => t.Name)
            .Where(t => !string.IsNullOrEmpty(t.RequiredPitchName))
            .ToDictionary(t => t.Name, t => t.RequiredPitchName);
    }

    public override IEnumerable<Game> Apply(Pitch pitch, IEnumerable<Game> games, List<Pitch> pitches)
    {
        if (!requiredPitchByLeague.Any())
        {
            return games;
        }
        // return games that are either not present in the map or if this pitch is the required pitch 
        return games.Where(g =>
            !requiredPitchByLeague.TryGetValue(g.Group.Type.Name, out var requiredPitch)
                || requiredPitch == pitch.Name);
    }
}
