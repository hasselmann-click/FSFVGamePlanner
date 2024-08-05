using FSFV.Gameplanner.Common;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FSFV.Gameplanner.Service.Slotting;

public abstract class AbstractSlotService(ILogger logger) : ISlotService
{

    protected static readonly Game PLACEHOLDER = new() { Group = new() { Type = new() { MinDurationMinutes = 0 } } };
    // TODO: make configurable. Implement IConfigurationProvider?
    protected static readonly TimeSpan MaxSlotTime = TimeSpan.FromMinutes(120);

    protected ILogger Logger => logger;

    public abstract List<Pitch> SlotGameDay(List<Pitch> pitches, List<Game> games);

    protected virtual void BuildTimeSlots(List<Pitch> pitches)
    {
        foreach (var pitch in pitches)
        {
            if (pitch.Games.Count < 1)
            {
                Logger.LogWarning("No games for pitch {pitch} on {date}", pitch.Name, pitch.StartTime.ToShortTimeString());
                continue;
            }

            var timeLeft = pitch.TimeLeft;
            var numberOfBreaks = pitch.Games.Count - 1;
            var additionalBreak = numberOfBreaks > 0 ? timeLeft.Divide(numberOfBreaks) : TimeSpan.Zero;
            if (additionalBreak < TimeSpan.Zero)
                additionalBreak = TimeSpan.Zero;
            else
                // round to nearest 5
                additionalBreak = TimeSpan.FromMinutes(
                    Math.Floor(additionalBreak.TotalMinutes / 5.0) * 5);

            pitch.Games = [.. pitch.Games.OrderByDescending(g => g.Group.Type.Priority)];

            int parallel = 1;
            int i = 0;
            var firstGame = pitch.Games[i++];
            var slottime = firstGame.MinDuration.Add(additionalBreak);
            pitch.Slots = new List<TimeSlot>(pitch.Games.Count)
            {
                new() {
                    StartTime = pitch.StartTime,
                    EndTime = pitch.StartTime.Add(slottime > MaxSlotTime ? MaxSlotTime : slottime),
                    Game = firstGame
                }
            };

            for (; i < pitch.Games.Count; ++i)
            {
                var prev = pitch.Slots[i - 1];
                var game = pitch.Games[i];
                TimeOnly start;
                if (prev.Game.Group.Type == game.Group.Type
                    && game.Group.Type.ParallelGamesPerPitch >= ++parallel)
                {
                    start = prev.StartTime;
                }
                else
                {
                    parallel = 1;
                    start = prev.EndTime;
                }

                // don't add break to last timeslot
                var breakToAdd = i == pitch.Games.Count - 1 ? TimeSpan.Zero : additionalBreak;
                pitch.Slots.Add(new TimeSlot
                {
                    StartTime = start,
                    EndTime = start.Add(game.MinDuration).Add(breakToAdd),
                    Game = game
                });
            }
        }
    }

}