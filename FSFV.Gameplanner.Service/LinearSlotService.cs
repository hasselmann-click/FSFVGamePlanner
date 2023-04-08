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
                for (int i = slots.Length - 1; i > 0; --i)
                {

                    var current = slots[i];
                    var refCandidates = new List<Team>(8); // ed. guess: 2 before, 2 after, some parallels

                    TimeSlot after = current;
                    while (i < slots.Length - 1 && after.StartTime == current.StartTime)
                    {
                        after = slots[i + 1];
                    }
                    refCandidates.Add(after.Game.Home);
                    refCandidates.Add(after.Game.Away);

                    TimeSlot afterParallel = after;
                    while (i < slots.Length - 1 && afterParallel.StartTime == current.StartTime)
                    {
                        refCandidates.Add(afterParallel.Game.Home);
                        refCandidates.Add(afterParallel.Game.Away);
                        afterParallel = slots[i + 1];
                    }

                    foreach(var refCandidate in refCandidates.OrderBy(c => c.RefereeCommitment))
                    {
                        if (referees.Contains(refCandidate.Name))
                            continue;
                        ++refCandidate.RefereeCommitment;
                        current.Game.Referee = refCandidate;
                        referees.Add(refCandidate.Name);
                    }

                    TimeSlot before = current;
                    while (i > 0 && before.StartTime == current.StartTime)
                    {
                        before = slots[i - 1];
                    }
                    refCandidates.Add(before.Game.Home);
                    refCandidates.Add(before.Game.Away);

                    TimeSlot beforeParallel = after;
                    while (i > 0 && beforeParallel.StartTime == current.StartTime)
                    {
                        refCandidates.Add(beforeParallel.Game.Home);
                        refCandidates.Add(beforeParallel.Game.Away);
                        beforeParallel = slots[i + 1];
                    }

                }
            }
        }
    }
}
}
