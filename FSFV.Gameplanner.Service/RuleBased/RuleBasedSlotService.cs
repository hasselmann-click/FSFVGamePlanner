using FSFV.Gameplanner.Common;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FSFV.Gameplanner.Service.Rules;

public class RuleBasedSlotService : AbstractSlotService
{
    private static readonly List<Game> EmptyList = new(0);

    private readonly IEnumerable<ISlotRule> rules;
    private readonly ILogger<RuleBasedSlotService> logger;

    public RuleBasedSlotService(ILogger<RuleBasedSlotService> logger, Random rng,
        IEnumerable<ISlotRule> rules) : base(logger, rng)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.rules = rules.OrderByDescending(r => r.GetPriority());

        if (!rules.Any())
        {
            this.logger.LogWarning("No rules detected for slotting games. Moving on without.");
        }
    }

    public override List<Pitch> SlotGameDay(List<Pitch> pitches, List<Game> games)
    {
        rules.ToList().ForEach(r => r.ProcessBeforeGameday(pitches, games));
        while (games.Any())
        {
            foreach (var p in pitches)
            {
                IEnumerable<Game> slotCandidates = games;
                foreach (var rule in rules) // rules are ordered by their priority
                {
                    slotCandidates = rule.Apply(p, slotCandidates, pitches) ?? EmptyList;
                }
                if (!slotCandidates.Any())
                {
                    continue;
                }

                var scheduledGame = slotCandidates.First();
                foreach (var rule in rules)
                {
                    rule.Update(p, scheduledGame);
                }

                games.Remove(scheduledGame);
                p.Games.Add(scheduledGame);
                if (!games.Any())
                {
                    break;
                }
            }
        }
        rules.ToList().ForEach(r => r.ProcessAfterGameday(pitches));

        BuildTimeSlots(pitches);
        AddRefereesToTimeslots(pitches);

        return pitches;
    }

    protected override void BuildTimeSlots(List<Pitch> pitches)
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
            else if (additionalBreak > MaxBreak)
                additionalBreak = MaxBreak;
            else
                // round to nearest 5
                additionalBreak = TimeSpan.FromMinutes(
                    Math.Floor(additionalBreak.TotalMinutes / 5.0) * 5);

            int parallel = 1;
            int i = 0;
            var firstGame = pitch.Games[i++];
            pitch.Slots = new List<TimeSlot>(pitch.Games.Count)
                {
                    new TimeSlot
                    {
                        StartTime = pitch.StartTime,
                        EndTime = pitch.StartTime.Add(firstGame.MinDuration).Add(additionalBreak),
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
}
