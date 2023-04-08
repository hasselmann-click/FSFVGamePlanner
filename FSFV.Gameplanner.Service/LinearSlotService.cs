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
        //AddRefereesToTimeslots(pitches);
        return pitches;
    }

    private void AddRefereesToTimeslots(List<Pitch> pitches)
    {
        foreach (var slotGroups in pitches.SelectMany(p => p.Slots.GroupBy(s => s.Game.Group.Type.Name)))
        {
            var slots = slotGroups.ToArray();
            for (int i = 0; i < slots.Length - 1; ++i)
            {
                var current = slots[i];
                var next = slots[i + 1];
                // TODO handle parallel games
                //if(current.StartTime == next.StartTime)
                //{

                //}
            }
        }
    }
}