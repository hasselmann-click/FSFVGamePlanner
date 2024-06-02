using FSFV.Gameplanner.Common;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FSFV.Gameplanner.Service.Slotting
{
    public abstract class AbstractSlotService : ISlotService
    {

        protected static readonly Game PLACEHOLDER = new() { Group = new() { Type = new() { MinDurationMinutes = 0 } } };
        // TODO: make configurable. Implement IConfigurationProvider?
        protected static readonly TimeSpan MaxSlotTime = TimeSpan.FromMinutes(120);

        private readonly ILogger logger;
        private readonly Random rng;

        public AbstractSlotService(ILogger logger, Random rng)
        {
            this.logger = logger;
            this.rng = rng;
        }

        protected ILogger Logger => logger;
        protected Random Rng => rng;

        public abstract List<Pitch> SlotGameDay(List<Pitch> pitches, List<Game> games);

        protected virtual void BuildTimeSlots(List<Pitch> pitches)
        {
            foreach (var pitch in pitches)
            {
                if (pitch.Games.Count < 1)
                {
                    Logger.LogWarning("No games for pitch {pitch} on {date}", pitch.Name,
                        pitch.StartTime.ToShortDateString());
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
                    new TimeSlot
                    {
                        StartTime = pitch.StartTime,
                        EndTime = pitch.StartTime.Add(slottime > MaxSlotTime ? MaxSlotTime : slottime),
                        Game = firstGame
                    }
                };

                for (; i < pitch.Games.Count; ++i)
                {
                    var prev = pitch.Slots[i - 1];
                    var game = pitch.Games[i];
                    DateTime start;
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

        protected void AddRefereesToTimeslots(List<Pitch> pitches)
        {
            // TODO make configurable by league
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
}