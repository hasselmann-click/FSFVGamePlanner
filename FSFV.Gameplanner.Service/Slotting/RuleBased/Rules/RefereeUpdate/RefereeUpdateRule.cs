using FSFV.Gameplanner.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FSFV.Gameplanner.Service.Slotting.RuleBased.Rules.RefereeUpdate;

internal class RefereeUpdateRule(int priority, ILogger<RefereeUpdateRule> logger, IConfiguration configuration) : AbstractSlotRule(priority)
{
    private const string ConfigKey = "RefereeUpdate";
    // TOOD read from config
    private static readonly TimeSpan MaxBreak = TimeSpan.FromMinutes(30);

    private readonly Dictionary<string, RefereeUpdateGroupConfig> configs = configuration.GetSection(ConfigKey).Get<Dictionary<string, RefereeUpdateGroupConfig>>();

    public override IEnumerable<Game> Apply(Pitch pitch, IEnumerable<Game> games, List<Pitch> pitches)
    {
        // do nothing
        return games;
    }

    public override void ProcessAfterGameday(List<Pitch> pitches)
    {

        // For every pitch group games by league (GroupType)
        foreach (var pitch in pitches)
        {
            foreach (var slotGroups in pitch.Slots.GroupBy(s => s.Game.Group.Type.Name))
            {

                if (!configs.TryGetValue(slotGroups.Key, out var config))
                {
                    config = new RefereeUpdateGroupConfig();
                    configs.Add(slotGroups.Key, config);
                }
                // skip referees if configured
                if (!config.HasReferees) continue;
                if (pitch.GameDay == 1 && config.SkipRefereeOnFirstDay) continue;

                var slots = slotGroups.OrderBy(s => s.StartTime).ToArray();
                if (slots.Length == 1)
                {
                    var slot = slots[0];
                    logger.LogError("Single game of type {type} at game day {day} at {time} on" +
                        " pitch {pitch}. Can't place referee", slotGroups.Key, slot.Game.GameDay,
                        slot.StartTime, pitch.Name);
                    continue;
                }

                var referees = new HashSet<string>(slots.Length);
                for (int i = 0; i < slots.Length; ++i)
                {
                    var current = slots[i];

                    // skip empty games (placeholders)
                    if (string.IsNullOrEmpty(current.Game.Home?.Name)) continue;

                    var refCandidates = new List<Team>(8); // ed. guess: 2 before, 2 after, some parallels

                    // look for a game after the current one
                    TimeSlot after = current;
                    int afterId = i + 1;
                    while (after.StartTime == current.StartTime && afterId < slots.Length)
                    {
                        after = slots[afterId++];
                    }
                    if (after.StartTime != current.StartTime
                         // check for grouped games, which are too far apart
                         && !(after.StartTime - current.EndTime > MaxBreak))
                    {
                        // also look for parallel "after" games
                        TimeSlot afterParallel = after;
                        afterId -= 1; // id hack, otherwise the last option would be skipped
                        while (afterParallel.StartTime == after.StartTime && afterId < slots.Length)
                        {
                            refCandidates.Add(afterParallel.Game.Home);
                            refCandidates.Add(afterParallel.Game.Away);
                            afterParallel = slots[afterId++];
                        }
                    }

                    // look for a game before the current one
                    TimeSlot before = current;
                    int beforeId = i - 1;
                    while (before.StartTime == current.StartTime && beforeId >= 0)
                    {
                        before = slots[beforeId--];
                    }
                    if (before.StartTime != current.StartTime
                         // check for grouped games, which are too far apart
                         && !(current.StartTime - before.EndTime > MaxBreak))
                    {

                        // also look for parallel "before" games
                        TimeSlot beforeParallel = before;
                        beforeId += 1; // id hack, otherwise the first option would be skipped
                        while (beforeParallel.StartTime == before.StartTime && beforeId >= 0)
                        {
                            refCandidates.Add(beforeParallel.Game.Home);
                            refCandidates.Add(beforeParallel.Game.Away);
                            beforeParallel = slots[beforeId--];
                        }
                    }

                    var referee = refCandidates
                        .Where(rc => !referees.Contains(rc.Name))
                        .OrderBy(c => c.RefereeCommitment)
                        .FirstOrDefault();
                    if (referee == null)
                    {
                        logger.LogError("No referee candidates for a game of type {type} at game day" +
                            " {day} at {time} on pitch {pitch}.", slotGroups.Key, current.Game.GameDay, current.StartTime, pitch.Name);
                        continue;
                    }
                    ++referee.RefereeCommitment;
                    current.Game.Referee = referee;
                    referees.Add(referee.Name);
                }
            }
        }
    }
}
