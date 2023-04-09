using FSFV.Gameplanner.Common;
using System.Collections.Generic;
using System.Linq;

namespace FSFV.Gameplanner.Service.RuleBased.Rules;

internal class AlphabeticalHomeSort : AbstractSlotRule
{
    public AlphabeticalHomeSort(int priority) : base(priority)
    {
    }

    public override IEnumerable<Game> Apply(Pitch pitch, IEnumerable<Game> games, List<Pitch> pitches)
    {
        return games.OrderBy(g => g.Home.Name[0]);
    }

}
