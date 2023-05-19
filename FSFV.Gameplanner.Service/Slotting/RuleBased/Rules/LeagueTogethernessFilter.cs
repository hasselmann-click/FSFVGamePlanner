using FSFV.Gameplanner.Common;
using System.Collections.Generic;
using System.Linq;

namespace FSFV.Gameplanner.Service.Slotting.RuleBased.Rules;

/// <summary>
/// Filters by the last slotted league. If no games are left from this league, nothing is filtered.
/// </summary>
internal class LeagueTogethernessFilter : AbstractSlotRule
{
    public LeagueTogethernessFilter(int priority) : base(priority)
    {
    }

    public override IEnumerable<Game> Apply(Pitch pitch, IEnumerable<Game> games, List<Pitch> pitches)
    {
        if (!pitch.Games.Any())
            return games;

        var lastSlottedLeague = pitch.Games.Last().Group.Type.Name;
        var samesies = games.Where(g => g.Group.Type.Name == lastSlottedLeague);
        if (samesies.Any())
        {
            return samesies;
        }

        return games;
    }
}
