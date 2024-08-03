using FSFV.Gameplanner.Common;
using FSFV.Gameplanner.Common.Rng;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FSFV.Gameplanner.Service.Slotting.RuleBased;

public class RuleBasedSlotService : AbstractSlotService
{
    private static readonly List<Game> EmptyList = [];

    private readonly ILogger<RuleBasedSlotService> logger;
    private readonly List<ISlotRule> rules;
    private readonly IRngProvider rng;

    public RuleBasedSlotService(ILogger<RuleBasedSlotService> logger, IRngProvider rng, IEnumerable<ISlotRule> rules) : base(logger)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.rng = rng;
        this.rules = [.. rules.OrderByDescending(r => r.GetPriority())];

        if (!rules.Any())
        {
            this.logger.LogWarning("No rules detected for slotting games. Moving on without.");
        }
    }

    public override List<Pitch> SlotGameDay(List<Pitch> pitches, List<Game> games)
    {
        rules.ForEach(r => r.ProcessBeforeGameday(pitches, games));

        var gameDate = pitches.Select(p => new { p.GameDay, Date = p.StartTime.ToShortDateString() }).First();
        // vars for endless loop prevention
        var hasSlottedGames = true;
        var previousGamesCount = games.Count;
        // if at one point no games could be scheduled on a pitch, it's almost impossible
        // to have some scheduled later on. Thus ignoring these pitches.
        var pitchesToIgnore = new HashSet<string>(pitches.Count);

        while (games.Count != 0)
        {
            // order pitches by their next available starting time 
            var orderedPitches = pitches
                .Where(p => !pitchesToIgnore.Contains(p.Name))
                .OrderBy(p => rng.NextInt64())
                .ThenBy(op => op.NextStartTime);
            var currentStartTime = orderedPitches.Select(p => p.NextStartTime).First();
            
            logger.LogTrace("Pitch Order: {pitches}", string.Join(", ", orderedPitches.Select(op => "[" + op.Name + ": " + op.NextStartTime + "]")));
            foreach (var nextPitch in orderedPitches)
            {

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

                // check for time left at pitch
                var scheduledGame = slotCandidates.Where(g => g.MinDuration <= nextPitch.TimeLeft).FirstOrDefault();
                if (scheduledGame == null)
                {
                    logger.LogDebug("Not enough time left at pitch {pitch}", nextPitch.Name);
                    continue;
                }

                // update rules
                rules.ForEach(r => r.Update(nextPitch, scheduledGame));

                logger.LogDebug("Game order: {games}", string.Join(", ", games.Select(g => g.Group + ";" + g.Home?.Name ?? "kein")));
                logger.LogDebug("Slotted game: {g}", scheduledGame.Home);

                games.Remove(scheduledGame);
                nextPitch.Games.Add(scheduledGame);

                if (games.Count == 0)
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
                    logger.LogError("Could not slot any games for the second time now on game day {day} at {date}. Adding" +
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
        rules.ForEach(r => r.ProcessAfterGameday(pitches));
        
        return pitches;
    }

    // TODO: missing commentary why this is overriden from abstract class
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

            // "blow up" pitch time by adding breaks
            var timeLeft = pitch.TimeLeft;
            var numberOfBreaks = pitch.Games.Count - 1;
            var additionalBreak = numberOfBreaks > 0 ? timeLeft.Divide(numberOfBreaks) : TimeSpan.Zero;
            if (additionalBreak < TimeSpan.Zero)
                additionalBreak = TimeSpan.Zero;
            else
                // round to nearest 5
                additionalBreak = TimeSpan.FromMinutes(
                    Math.Floor(additionalBreak.TotalMinutes / 5.0) * 5);

            // todo: refactor to single time slot creation method
            // add first game separately, because it's the only one with a fixed start time
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

            // add remaining games
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
                slottime = game.MinDuration.Add(breakToAdd);
                pitch.Slots.Add(new TimeSlot
                {
                    StartTime = start,
                    EndTime = start.Add(slottime > MaxSlotTime ? MaxSlotTime : slottime),
                    Game = game
                });
            }
        }
    }
}
