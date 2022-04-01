using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FSFV.Gameplanner.Service
{
    public class SlotService
    {

        public SlotService(ILogger<SlotService> logger)
        {
            this.logger = logger;
        }

        private static readonly Game PLACEHOLDER = new Game();
        private readonly ILogger<SlotService> logger;

        private Team DetermineRef(Game game) => game.Home.RefereeCommitment > game.Away.RefereeCommitment ? game.Away : game.Home;
        private Team DetermineRefFromPreLastGame(Tuple<Game, Game> lastPair) => lastPair.Item1.Referee == lastPair.Item2.Home ? lastPair.Item2.Away : lastPair.Item2.Home;

        public GameDay Slot(List<Pitch> pitches, List<Game> games)
        {
            // TODO sub group support

            var groups = games.OrderByDescending(g => g.Group.Priority).GroupBy(g => g.Group).ToList();
            List<Tuple<Game, Game>> pairs = new(games.Count / 2 + groups.Count); // if uneven per group a placeholder

            BuildRefereePairs(groups, pairs);
            Distribute(pitches, pairs);
            BuildTimeSlots(pitches);

            return new GameDay { Pitches = pitches };
        }

        private static void BuildTimeSlots(List<Pitch> pitches)
        {
            foreach (var pitch in pitches)
            {
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
                        EndTime = start.Add(pitch.Games[0].MinDuration).Add(additionalBreak),
                        Game = pitch.Games[0]
                    });
                }
            }
        }

        private void Distribute(List<Pitch> pitches, List<Tuple<Game, Game>> pairs)
        {

            // TODO ZK Duty
            // TODO Max parallel pitches per group

            foreach (var pair in pairs)
            {
                var minDuration = pair.Item1.MinDuration.Add(pair.Item2.MinDuration);
                bool added = false;
                foreach (var pitch in pitches.OrderBy(p => p.StartTime).ThenBy(p => p.Games.Count))
                {
                    if (pitch.TimeLeft < minDuration)
                        continue;

                    pitch.Games.Add(pair.Item1);
                    pitch.Games.Add(pair.Item2);
                    added = true;
                    break;
                }

                if (!added)
                {
                    logger.LogError($"Could not slot game pair. Adding to pitch of type {pitches[0].Type.PitchTypeID}");
                    pitches[0].Games.Add(pair.Item1);
                    pitches[0].Games.Add(pair.Item2);
                }
            }
        }

        private void BuildRefereePairs(List<IGrouping<Grouping, Game>> groups, List<Tuple<Game, Game>> pairs)
        {
            foreach (var group in groups.Select(g => g.ToList()))
            {
                for (int i = 0, j = 1; i < group.Count; i += 2, j += 2)
                {
                    var game1 = group[i];
                    var game2 = group[j];

                    game1.Referee = DetermineRef(game2);
                    game2.Referee = DetermineRef(game1);

                    game1.Referee.RefereeCommitment++;
                    game2.Referee.RefereeCommitment++;

                    pairs.Add(Tuple.Create(game1, game2));
                }
                if ((group.Count & 1) == 1) // is odd? Then last game was skipped
                {
                    logger.LogDebug($"Uneven number of games, adding placeholder");
                    var lastGame = group[group.Count - 1];
                    lastGame.Referee = DetermineRefFromPreLastGame(pairs[pairs.Count - 1]);
                    pairs.Add(Tuple.Create(lastGame, PLACEHOLDER));
                }
            }
        }

        public class GroupType
        {
            public int GroupTypeID { get; set; }
        }
        public class Grouping
        {
            public int GroupingID { get; set; }
            public int Priority { get; set; }
            public GroupType Type { get; set; }
            public Grouping SubGroup { get; set; }

            public override bool Equals(object obj)
            {
                return obj is Grouping grouping &&
                       GroupingID == grouping.GroupingID;
            }
        }
        public class TeamType
        {
            public string Name { get; set; }
        }
        public class Team
        {
            public TeamType Type { get; set; }
            public int RefereeCommitment { get; set; }
        }
        public class Game
        {
            public Grouping Group { get; set; }
            public Team Home { get; set; }
            public Team Away { get; set; }
            public Team Referee { get; set; }

            public TimeSpan MinDuration { get; set; }
        }

        public class TimeSlot
        {
            public Game Game { get; set; }
            public DateTime StartTime { get; set; }
            public DateTime EndTime { get; set; }
        }
        public class PitchType
        {
            public int PitchTypeID { get; set; }
        }
        public class Pitch
        {
            public PitchType Type { get; set; }
            public DateTime StartTime { get; set; }
            public DateTime EndTime { get; set; }

            public List<Game> Games { get; set; } = new(10);
            public List<TimeSlot> Slots { get; set; } = new(10);
            // TODO test performance
            public TimeSpan TimeLeft => EndTime.Subtract(StartTime)
                .Subtract(Games.Select(g => g.MinDuration)
                    .Aggregate(TimeSpan.Zero, (d1, d2) => d1.Add(d2)));


        }
        public class GameDay
        {
            public List<Pitch> Pitches { get; set; }
        }
    }
}
