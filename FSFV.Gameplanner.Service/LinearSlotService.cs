using FSFV.Gameplanner.Common;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FSFV.Gameplanner.Service;

public class LinearSlotService : AbstractSlotService
{
    public LinearSlotService(ILogger<LinearSlotService> logger, Random rng) : base(logger, rng)
    {
    }

    public override List<Pitch> SlotGameDay(List<Pitch> pitches, List<Game> games)
    {

        if (!(pitches?.Count > 0 && games?.Count > 0))
            return pitches;

        var groups = games
            .OrderBy(g => Rng.Next())
            .GroupBy(g => g.Group)
            .ToList();

        // TODO dont schedule groups with requirements always first!
        var requirementGroups = groups.Where(g => !string.IsNullOrEmpty(g.Key.Type.RequiredPitchName));
        foreach (var requirementGroup in requirementGroups)
        {
            // TODO support multiple required pitches
            var pname = requirementGroup.Key.Type.RequiredPitchName;
            var pitch = pitches.FirstOrDefault(p => p.Name == pname) ?? throw new ArgumentException("Could not find required pitch {name}", pname);
            pitch.Games.AddRange(requirementGroup.ToList());
        }
        groups.RemoveAll(g => requirementGroups.Select(rg => rg.Key).Contains(g.Key));

        foreach (var group in groups.OrderByDescending(g => g.Key.Type.Priority))
        {
            var groupType = group.Key.Type;
            foreach (var game in group.OrderBy(p => Rng.Next()))
            {
                var minDuration = game.MinDuration;
                var shuffledPitches = pitches
                    .Where(p => p.TimeLeft > minDuration)
                    .OrderBy(p => p.TimeLeft)
                    .ToList();

                if (!shuffledPitches.Any())
                {
                    var mPitch = pitches.OrderByDescending(p => p.TimeLeft).First();
                    mPitch.Games.Add(game);
                    Logger.LogError("Could not slot game of type {type} on gameday {day}." +
                        " Adding to pitch {pitch}.", groupType.Name, game.GameDay, mPitch.Name);
                    continue;
                }

                var pidx = shuffledPitches.Count > groupType.MaxParallelPitches ? groupType.MaxParallelPitches - 1 : shuffledPitches.Count - 1;
                shuffledPitches[pidx].Games.Add(game);
            }
        }

        BuildTimeSlots(pitches);
        AddRefereesToTimeslots(pitches);
        return pitches;
    }

    private void AddRefereesToTimeslots(List<Pitch> pitches)
    {
        // For every pitch group games by league (GroupType)
        foreach (var pitch in pitches)
        {
            foreach (var slotGroups in pitch.Slots.GroupBy(s => s.Game.Group.Type.Name))
            {
                var slots = slotGroups.OrderBy(s => s.StartTime).ToArray();
                if (slots.Length == 1)
                {
                    var slot = slots[0];
                    Logger.LogError("Single game of type {type} at game day {day} at {time} on" +
                        " pitch {pitch}. Can't place referee", slotGroups.Key, slot.Game.GameDay,
                        slot.StartTime, pitch.Name);
                    continue;
                }

                var referees = new HashSet<string>(slots.Length);
                for (int i = 0; i < slots.Length; ++i)
                {
                    var current = slots[i];
                    var refCandidates = new List<Team>(8); // ed. guess: 2 before, 2 after, some parallels

                    // look for a game after the current one
                    TimeSlot after = current;
                    int afterId = i + 1;
                    while (after.StartTime == current.StartTime && afterId < slots.Length)
                    {
                        after = slots[afterId++];
                    }
                    if (after.StartTime != current.StartTime)
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
                    if (before.StartTime != current.StartTime)
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
                        Logger.LogError("No referee candidates for a game of type {type} at game day" +
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