using FSFV.Gameplanner.Common;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FSFV.Gameplanner.Service;

public class SlotService : AbstractSlotService, ISlotService
{
    public SlotService(ILogger<SlotService> Logger, Random rng) : base(Logger, rng)
    {
    }

    private static Team DetermineRef(Game game)
        => game.Home.RefereeCommitment > game.Away.RefereeCommitment ? game.Away : game.Home;
    private static Team DetermineRefFromPreLastGame((Game, Game) lastPair)
        => lastPair.Item1.Referee == lastPair.Item2.Home ? lastPair.Item2.Away : lastPair.Item2.Home;

    public override List<Pitch> SlotGameDay(List<Pitch> pitches, List<Game> games)
    {

        if (!(pitches?.Count > 0 && games?.Count > 0))
            return pitches;

        var groups = games
            .OrderBy(g => Rng.Next())
            .GroupBy(g => g.Group)
            .ToList();
        // TODO referees for parallel games
        var refPairs = BuildRefereePairs(groups);
        Distribute(pitches, refPairs);
        BuildTimeSlots(pitches);

        return pitches;
    }

    private void Distribute(List<Pitch> pitches, IEnumerable<IGrouping<Group, (Game, Game)>> refPairs)
    {
        // TODO ZK Duty

        foreach (var pairs in refPairs.OrderByDescending(g => g.Key.Type.Priority))
        {
            foreach (var pair in pairs.OrderBy(p => Rng.Next()))
            {
                var groupType = pairs.Key.Type;
                var requiredPitch = groupType.RequiredPitchName;
                var parallelGames = Math.Max(1, groupType.ParallelGamesPerPitch);
                var minDuration = pair.Item1.MinDuration
                    .Add(pair.Item2.MinDuration).Divide(parallelGames);

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
                    // TODO try to consider distance between pitches?
                    var mPitches = pitches.OrderByDescending(p => p.TimeLeft).Take(2).ToList();
                    mPitches[0].Games.Add(pair.Item1);
                    if (pair.Item2 != PLACEHOLDER)
                        mPitches[1].Games.Add(pair.Item2);
                    Logger.LogError("Could not slot game pair of type {type} on gameday {gameday}." +
                        " Adding to pitch {pitch1} and {pitch2}",
                        groupType.Name, pair.Item1.GameDay, mPitches[0].Name, mPitches[1].Name);
                    continue;
                }

                var idx = shuffledPitches.Count > groupType.MaxParallelPitches
                        ? groupType.MaxParallelPitches - 1 : shuffledPitches.Count - 1;
                var pitch = shuffledPitches[idx];

                pitch.Games.Add(pair.Item1);
                if (pair.Item2 != PLACEHOLDER)
                    pitch.Games.Add(pair.Item2);
            }
        }
    }

    private IEnumerable<IGrouping<Group, (Game, Game)>> BuildRefereePairs(
        List<IGrouping<Group, Game>> groups)
    {
        List<(Game, Game)> pairs = new(groups.Sum(g => g.Count()));
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
                Logger.LogDebug($"Uneven number of games, adding placeholder");
                var lastGame = group[^1];
                lastGame.Referee = DetermineRefFromPreLastGame(pairs[^1]);
                pairs.Add((lastGame, PLACEHOLDER));
            }
        }
        return pairs.GroupBy(p => p.Item1.Group);
    }
}
