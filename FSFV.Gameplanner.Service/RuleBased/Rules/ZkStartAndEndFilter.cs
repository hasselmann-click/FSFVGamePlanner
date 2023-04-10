using FSFV.Gameplanner.Common;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FSFV.Gameplanner.Service.RuleBased.Rules;

/// <summary>
/// Checks after a game day, if it starts 
/// </summary>
internal class ZkStartAndEndFilter : AbstractSlotRule
{
    private readonly ILogger<ZkStartAndEndFilter> logger;
    private readonly Random rng;

    public ZkStartAndEndFilter(int priority, ILogger<ZkStartAndEndFilter> logger, Random rng) : base(priority)
    {
        this.logger = logger;
        this.rng = rng;
    }

    public override IEnumerable<Game> Apply(Pitch pitch, IEnumerable<Game> games, List<Pitch> pitches)
    {
        /* do nothing */
        return games;
    }

    public override void ProcessAfterGameday(List<Pitch> pitches)
    {
        l_HandleZkStart(pitches);
        l_HandleZkEnd(pitches);

        void l_HandleZkStart(List<Pitch> pitches)
        {
            var earliestStartTime = pitches.Select(x => x.StartTime).Min();
            var earlyPitches = pitches.Where(p => p.StartTime == earliestStartTime && p.Games.Any());
            var isZkStarting = earlyPitches.Select(p => p.Games.First()).Any(g => g.Home.IsZK || g.Away.IsZK);
            if (!isZkStarting)
            {
                // search for a ZK in game in the same grouptype, then exchange 
                var hasSwitched = false;
                foreach (var p in earlyPitches.OrderBy(p => rng.Next()))
                {
                    var first = p.Games.First();
                    foreach (var p2 in pitches.OrderBy(p => rng.Next()))
                    {
                        for (int i = 0; i < p2.Games.Count; ++i)
                        {
                            var candidate = p2.Games[i];
                            if ((candidate.Home.IsZK || candidate.Away.IsZK) && candidate.Group.Type.Name == first.Group.Type.Name)
                            {
                                p.Games[0] = candidate;
                                p2.Games[i] = first;
                                hasSwitched = true;
                                logger.LogInformation("ZK Start: Switched game of league {league} from pitch {p1} to pitch {p2}", first.Group.Type.Name, p.Name, p2.Name);
                                break;
                            }
                        }
                        if (hasSwitched) break;
                    }
                    if (hasSwitched) break;
                }
                if (!hasSwitched)
                {
                    logger.LogError("ZK Start: Could not start with ZK on Gameday {day}", earlyPitches.First().GameDay);
                }
            }
            else
            {
                logger.LogInformation("ZK Start: Starting with ZK on game day {day}", earlyPitches.First().GameDay);
            }
        }

        void l_HandleZkEnd(List<Pitch> pitches)
        {
            var latestEndTime = pitches.Select(p => p.EndTime).Max();
            var latestPitches = pitches.Where(p => p.EndTime == latestEndTime && p.Games.Any());
            var isZkEnding = latestPitches.Select(p => p.Games.Last()).Any(g => g.Home.IsZK || g.Away.IsZK);

            if (!isZkEnding)
            {
                var hasSwitched = false;
                foreach (var p in latestPitches.OrderBy(p => rng.Next()))
                {
                    var last = p.Games.Last();
                    foreach (var p2 in pitches.OrderBy(p => rng.Next()))
                    {
                        for (int i = p2.Games.Count - 1; i >= 0; --i)
                        {
                            var candidate = p2.Games[i];
                            if ((candidate.Home.IsZK || candidate.Away.IsZK) && candidate.Group.Type.Name == last.Group.Type.Name)
                            {
                                p.Games[^1] = candidate;
                                p2.Games[i] = last;
                                hasSwitched = true;
                                logger.LogInformation("ZK End: Switched game of league {league} from pitch {p1} to pitch {p2}", last.Group.Type.Name, p.Name, p2.Name);
                                break;
                            }
                        }
                        if (hasSwitched) break;
                    }
                    if (hasSwitched) break;
                }
                if (!hasSwitched)
                {
                    logger.LogError("ZK End: Could not end with ZK on Gameday {day}", latestPitches.Last().GameDay);
                }
            }
            else
            {
                logger.LogInformation("ZK End: Ending with ZK on game day {day}", latestPitches.Last().GameDay);
            }
        }
    }

}
