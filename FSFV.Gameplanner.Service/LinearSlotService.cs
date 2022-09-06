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

        var requirementGroups = groups.Where(g => !string.IsNullOrEmpty(g.Key.Type.RequiredPitchName));
        foreach(var requirementGroup in requirementGroups)
        {
            // TODO requirement groups will always be scheduled first!
            // TODO support multiple required pitches
            var pname = requirementGroup.Key.Type.RequiredPitchName;
            var pitch = pitches.FirstOrDefault(p => p.Name == pname)
                ?? throw new ArgumentException("Could not find required pitch {name}", pname);
            pitch.Games.AddRange(requirementGroup.ToList());
        }
        groups.RemoveAll(g => requirementGroups.Select(rg => rg.Key).Contains(g.Key));

        foreach (var group in groups.OrderByDescending(g => g.Key.Type.Priority))
        {
            var groupType = group.Key.Type;
            var requiredPitch = groupType.RequiredPitchName;
            var maxParallelGames = Math.Max(1, groupType.ParallelGamesPerPitch);

            var parallelGames = maxParallelGames;
            var parallelPitchName = "";
            foreach (var game in group.OrderBy(p => Rng.Next()))
            {
                var minDuration = game.MinDuration;
                Pitch pitch;
                if (parallelGames == maxParallelGames || parallelGames == 0)
                {
                    var shuffledPitches = pitches
                    .Where(p => string.IsNullOrEmpty(requiredPitch) || requiredPitch.Equals(p.Name))
                    .Where(p => p.TimeLeft > minDuration)
                    .OrderBy(p => p.StartTime)
                    .ThenBy(p => p.Games.Count)
                    .ThenBy(p => Rng.Next())
                    .OrderByDescending(p => p.Games.Count(g => g.Group.Type == groupType))
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
                    pitch = shuffledPitches[pidx];
                    // remember possible parallel game
                    parallelPitchName = pitch.Name;

                }
                else // parallel game
                {
                    pitch = pitches.First(p => p.Name == parallelPitchName);
                }

                --parallelGames;
                pitch.Games.Add(game);
            }

        }

        BuildTimeSlots(pitches);
        AddRefereesToTimeslots(pitches);
        return pitches;
    }

    private void AddRefereesToTimeslots(List<Pitch> pitches)
    {
        foreach(var slotGroups in pitches.SelectMany(p => p.Slots.GroupBy(s => s.Game.Group.Type.Name)))
        {
                var slots = slotGroups.ToArray();
                for(int i = 0; i < slots.Length - 1; ++i)
                {
                    var current = slots[i];
                    var next = slots[i+1];
                // TODO handle parallel games
                    //if(current.StartTime == next.StartTime)
                    //{
                        
                    //}
                }
            }
        }
    }
}
