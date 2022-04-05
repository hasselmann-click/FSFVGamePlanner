using FSFV.Gameplanner.Common;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FSFV.Gameplanner.Service;

public class SlotService
{

    private static readonly Random RNG = new Random(23432546);

    private static readonly Game PLACEHOLDER = new() { Group = new() { Type = new() { MinDurationMinutes = 0 } } };
    private static readonly TimeSpan MaxBreak = TimeSpan.FromMinutes(30);

    public SlotService(ILogger<SlotService> logger)
    {
        this.logger = logger;
    }

    private readonly ILogger<SlotService> logger;

    private static Team DetermineRef(Game game)
        => game.Home.RefereeCommitment > game.Away.RefereeCommitment ? game.Away : game.Home;
    private static Team DetermineRefFromPreLastGame((Game, Game) lastPair)
        => lastPair.Item1.Referee == lastPair.Item2.Home ? lastPair.Item2.Away : lastPair.Item2.Home;

    public List<Pitch> SlotGameDay(List<Pitch> pitches, List<Game> games)
    {
        // TODO sub group support

        if (!(pitches?.Count > 0 && games?.Count > 0))
            return pitches;

        var groups = games
            .GroupBy(g => g.Group)
            .OrderByDescending(g => g.Key.Type.Priority)
            .ToList();
        List<(Game, Game)> pairs = new(games.Count / 2 + groups.Count); // if uneven per group a placeholder

        BuildRefereePairs(groups, pairs);
        Distribute(pitches, pairs);
        BuildTimeSlots(pitches);

        return pitches;
    }

    private void BuildTimeSlots(List<Pitch> pitches)
    {
        foreach (var pitch in pitches)
        {
            if (pitch.Games.Count < 1)
            {
                logger.LogWarning("No games for pitch {pitch} on {date}", pitch.Name,
                    pitch.StartTime.ToShortDateString());
                continue;
            }

            var timeLeft = pitch.TimeLeft;
            var numberOfBreaks = pitch.Games.Count - 1;
            var additionalBreak = timeLeft.Divide(numberOfBreaks);
            if (additionalBreak < TimeSpan.Zero)
                additionalBreak = TimeSpan.Zero;
            else if (additionalBreak > MaxBreak)
                additionalBreak = MaxBreak;
            else
                // round to nearest 5
                additionalBreak = TimeSpan.FromMinutes(
                    Math.Floor(additionalBreak.TotalMinutes / 5.0) * 5);

            pitch.Games = pitch.Games.OrderByDescending(g => g.Group.Type.Priority).ToList();

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

    private void Distribute(List<Pitch> pitches, List<(Game, Game)> pairs)
    {

        // TODO ZK Duty

        foreach (var pair in pairs.OrderBy(p => RNG.Next()))
        {

            var groupType = pair.Item1.Group.Type;
            var secondGroupType = pair.Item2.Group.Type;
            var mainGroupType = groupType.Name == null ? secondGroupType : groupType;

            var requiredPitch = mainGroupType.RequiredPitchName;
            var parallelGames = Math.Max(1, mainGroupType.ParallelGamesPerPitch);
            var minDuration = pair.Item1.MinDuration.Add(pair.Item2.MinDuration).Divide(parallelGames);
            bool added = false;

            foreach (var pitch in pitches
                .Where(p => string.IsNullOrEmpty(requiredPitch) || requiredPitch.Equals(p.Name))
                .OrderBy(p => p.StartTime)
                .ThenBy(p => p.Games.Count)
                .ThenBy(p => RNG.Next()))
            {
                // check maximum parallel pitches per group type
                if (!pitch.Games.Any(g => g.Group.Type == mainGroupType)
                    && mainGroupType.MaxParallelPitches <= pitches.Count(p =>
                        p.Games.Any(g => g.Group.Type == mainGroupType)))
                {
                    continue;
                }

                var timeLeft = pitch.TimeLeft;
                if (timeLeft < minDuration)
                {
                    logger.LogDebug("Not enough time left for pitch {pitch}: {time} < {duration}",
                        pitch.Name, timeLeft.TotalMinutes, minDuration.TotalMinutes);
                    continue;
                }
                else
                {
                    logger.LogDebug("{pitch}: {time} > {duration}",
                        pitch.Name, timeLeft.TotalMinutes, minDuration.TotalMinutes);
                }
                pitch.Games.Add(pair.Item1);
                if (pair.Item2 != PLACEHOLDER)
                    pitch.Games.Add(pair.Item2);
                added = true;
                break;
            }

            if (!added)
            {
                var minimumGamesPitch = pitches.OrderBy(p => p.Games.Count).First();
                logger.LogError("Could not slot game pair of type {type} on gameday {gameday}." +
                    " Adding to pitch of type {PitchTypeID}",
                    pair.Item1.Group.Type.Name, pair.Item1.GameDay, minimumGamesPitch.Name);
                minimumGamesPitch.Games.Add(pair.Item1);
                if (pair.Item2 != PLACEHOLDER)
                    minimumGamesPitch.Games.Add(pair.Item2);
            }
        }
    }

    private void BuildRefereePairs(List<IGrouping<Group, Game>> groups, List<(Game, Game)> pairs)
    {
        foreach (var group in groups.Select(g => g.ToList()))
        {
            for (int i = 0, j = 1; j < group.Count; i += 2, j += 2)
            {
                var game1 = group[i];
                var game2 = group[j];

                game1.Referee = DetermineRef(game2);
                game2.Referee = DetermineRef(game1);

                game1.Referee.RefereeCommitment++;
                game2.Referee.RefereeCommitment++;

                pairs.Add((game1, game2));
            }
            if ((group.Count & 1) == 1) // is odd? Then last game was skipped
            {
                logger.LogDebug($"Uneven number of games, adding placeholder");
                var lastGame = group[^1];
                lastGame.Referee = DetermineRefFromPreLastGame(pairs[^1]);
                pairs.Add((lastGame, PLACEHOLDER));
            }
        }
    }
}
