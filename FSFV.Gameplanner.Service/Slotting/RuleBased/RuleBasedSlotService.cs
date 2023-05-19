using FSFV.Gameplanner.Common;
using FSFV.Gameplanner.Service.Slotting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FSFV.Gameplanner.Service.Slotting.RuleBased;

public class RuleBasedSlotService : AbstractSlotService
{
    private static readonly List<Game> EmptyList = new(0);
    private static readonly TimeSpan SlotBuffer = TimeSpan.FromMinutes(30);

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
        var gameDate = pitches.Select(p => new { p.GameDay, Date = p.StartTime.ToShortDateString() }).First();
        // vars for endless loop prevention
        var hasSlottedGames = true;
        var previousGamesCount = games.Count;
        // if at one point no games could be scheduled on a pitch, it's almost impossible
        // to have some scheduled later on. Thus ignoring these pitches.
        var pitchesToIgnore = new HashSet<string>(pitches.Count);

        while (games.Any())
        {
            // order pitches by their next available starting time 
            var orderedPitches = pitches
                .Where(p => !pitchesToIgnore.Contains(p.Name))
                .OrderBy(p => Rng.NextInt64())
                .OrderBy(op => op.NextStartTime);
            var currentStartTime = orderedPitches.Select(p => p.NextStartTime).First();
            logger.LogDebug("Pitch Order: {pitches}", string.Join(", ", orderedPitches.Select(op => "[" + op.Name + ": " + op.NextStartTime + "]")));
            foreach (var nextPitch in orderedPitches)
            {
                // slot games only for pitches which are in 30 minutes of each other.
                if (nextPitch.NextStartTime.Subtract(currentStartTime) > SlotBuffer)
                {
                    logger.LogDebug("Stopping placing at pitch {pitch}", nextPitch.Name);
                    break;
                }

                IEnumerable<Game> slotCandidates = games;
                foreach (var rule in rules) // rules are ordered by their priority
                {
                    slotCandidates = rule.Apply(nextPitch, slotCandidates, pitches) ?? EmptyList;
                }
                if (!slotCandidates.Any())
                {
                    logger.LogDebug("Ignoring pitch {pitch}", nextPitch.Name);
                    pitchesToIgnore.Add(nextPitch.Name);
                    continue;
                }

                var scheduledGame = slotCandidates.First();
                foreach (var rule in rules)
                {
                    rule.Update(nextPitch, scheduledGame);
                }

                games.Remove(scheduledGame);
                nextPitch.Games.Add(scheduledGame);
                if (!games.Any())
                    break;
            }

            // endless loop prevention
            // if any games were slotted, the current count is smaller than the previous count
            var currentGamesCount = games.Count;
            if (previousGamesCount == currentGamesCount)
            {
                // if it hadn't slotted games in the previous "round" already
                if (!hasSlottedGames)
                {
                    var tempPitch = pitches.OrderByDescending(p => p.TimeLeft).First();
                    tempPitch.Games.AddRange(games);
                    logger.LogError("Could not slot any games multiple times on game day {day} at {date}. Adding" +
                        " to {cnt} games to pitch {pitch}", gameDate.GameDay, gameDate.Date, currentGamesCount, tempPitch.Name);
                    break;
                }
                hasSlottedGames = false;
            }
            else
            {
                hasSlottedGames = true;
                previousGamesCount = currentGamesCount;
            }
        }

        BuildTimeSlots(pitches);
        rules.ToList().ForEach(r => r.ProcessAfterGameday(pitches));

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
