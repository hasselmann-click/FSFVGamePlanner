using FSFV.Gameplanner.Common;
using System.Collections.Generic;
using System.Linq;

namespace FSFV.Gameplanner.Service.RuleBased.Rules;

internal class ZkStartAndEndFilter : AbstractSlotRule
{
    public ZkStartAndEndFilter(int priority) : base(priority)
    {
    }

    public override IEnumerable<Game> Apply(Pitch pitch, IEnumerable<Game> games, List<Pitch> pitches)
    {
        throw new System.NotImplementedException();
    }

    public override void ProcessBeforeGameday(List<Pitch> pitches, List<Game> games)
    {
        var earliestStartTime = pitches.Select(x => x.StartTime).Min();
        var earlyPitches = pitches.Where(p => p.StartTime == earliestStartTime);

        var latestEndTime = pitches.Select(p => p.EndTime).Max();
        var latestPitches = pitches.Where(p => p.EndTime == latestEndTime);

        var shortestMinDuration = games.Select(g => g.Group.Type.MinDurationMinutes).Min();
    }

    public override void ProcessAfterGameday(List<Pitch> pitches)
    {
        base.ProcessAfterGameday(pitches);
    }

}
