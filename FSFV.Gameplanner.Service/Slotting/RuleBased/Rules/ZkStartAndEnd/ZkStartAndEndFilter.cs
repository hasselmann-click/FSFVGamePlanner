using FSFV.Gameplanner.Common;
using FSFV.Gameplanner.Common.Rng;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FSFV.Gameplanner.Service.Slotting.RuleBased.Rules.ZkStartAndEnd;

/// <summary>
/// Checks after a game day, if it starts 
/// </summary>
internal class ZkStartAndEndFilter(int priority, IConfiguration configuration, ILogger<ZkStartAndEndFilter> logger, IRngProvider rng)
    : AbstractSlotRule(priority)
{
    private const string ConfigKeyZkTeams = "ZkTeams";

    private readonly HashSet<string> zkTeams = configuration.GetSection(ConfigKeyZkTeams).Get<string[]>()?.ToHashSet() ?? [];

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
            var earlyPitches = pitches.Where(p => p.StartTime == earliestStartTime && p.Games.Count != 0);
            var zkStartingGame = earlyPitches.Select(p => p.Games.First()).FirstOrDefault(g => g.HasZk(zkTeams));
            if (zkStartingGame != null)
            {
                logger.LogInformation("ZK Start: Starting with ZK on game day {day} with game {home} - {away}", earlyPitches.First().GameDay, zkStartingGame.Home.Name, zkStartingGame.Away.Name);
                return;
            }

            // search for a ZK in game in the same grouptype, then exchange 
            var hasSwitched = false;
            foreach (var p in earlyPitches.OrderBy(p => rng.NextInt64()))
            {
                var first = p.Games.First();
                foreach (var p2 in pitches.OrderBy(p => rng.NextInt64()))
                {
                    for (int i = 0; i < p2.Games.Count; ++i)
                    {
                        var candidate = p2.Games[i];
                        if (candidate.HasZk(zkTeams) && candidate.Group.Type.Name == first.Group.Type.Name)
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

        void l_HandleZkEnd(List<Pitch> pitches)
        {
            // TODO: dont switch with first games for gamedays where there is only one ZK team present

            var latestEndTime = pitches.Select(p => p.EndTime).Max();
            var latestPitches = pitches.Where(p => p.EndTime == latestEndTime && p.Games.Count != 0);
            var zkEndingGame = latestPitches.Select(p => p.Games.Last()).FirstOrDefault(g => g.HasZk(zkTeams));

            if (zkEndingGame != null)
            {
                logger.LogInformation("ZK End: Ending with ZK on game day {day} with game {home} - {away}", latestPitches.First().GameDay, zkEndingGame.Home.Name, zkEndingGame.Away.Name);
                return;
            }

            var hasSwitched = false;
            foreach (var p in latestPitches.OrderBy(p => rng.NextInt64()))
            {
                var last = p.Games.Last();
                foreach (var p2 in pitches.OrderBy(p => rng.NextInt64()))
                {
                    for (int i = p2.Games.Count - 1; i >= 0; --i)
                    {
                        var candidate = p2.Games[i];
                        if (candidate.HasZk(zkTeams) && candidate.Group.Type.Name == last.Group.Type.Name)
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
    }

}
