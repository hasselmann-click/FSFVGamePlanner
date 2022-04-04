using FSFV.Gameplanner.Common;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

namespace FSFV.Gameplanner.Service;

public class SlotService
{

    private static readonly Game PLACEHOLDER = new()
    {
        Group = new() { Type = new() { MinDurationMinutes = 0 } }
    };

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
            .OrderByDescending(g => g.Group.Type.Priority)
            .GroupBy(g => g.Group)
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

            pitch.Slots = new List<TimeSlot>(pitch.Games.Count)
                {
                    new TimeSlot
                    {
                        StartTime = pitch.StartTime,
                        EndTime = pitch.StartTime.Add(pitch.Games[0].MinDuration).Add(additionalBreak),
                        Game = pitch.Games[0]
                    }
                };
            for (int i = 1; i < pitch.Games.Count; ++i)
            {
                var start = pitch.Slots[i - 1].EndTime;
                pitch.Slots.Add(new TimeSlot
                {
                    StartTime = start,
                    EndTime = start.Add(pitch.Games[i].MinDuration).Add(additionalBreak),
                    Game = pitch.Games[i]
                });
            }
        }
    }

    private void Distribute(List<Pitch> pitches, List<(Game, Game)> pairs)
    {

        // TODO ZK Duty
        // TODO Max parallel pitches per group

        foreach (var pair in pairs)
        {
            var minDuration = pair.Item1.MinDuration.Add(pair.Item2.MinDuration);
            bool added = false;
            foreach (var pitch in pitches.OrderBy(p => p.StartTime).ThenBy(p => p.Games.Count))
            {
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
                logger.LogError("Could not slot game pair. Adding to pitch" +
                    " of type {PitchTypeID}", pitches[0].Name);
                pitches[0].Games.Add(pair.Item1);
                if (pair.Item2 != PLACEHOLDER)
                    pitches[0].Games.Add(pair.Item2);
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
