using FSFV.Gameplanner.Common;
using FSFV.Gameplanner.Service.Slotting;
using System.Collections.Generic;
using System.Linq;

namespace FSFV.Gameplanner.Service.Slotting.RuleBased.Rules;

internal class MaxParallelPitchesFilter(int priority) : AbstractSlotRule(priority)
{
    private Dictionary<string, int> maxParallelPitchesByLeague;
    private Dictionary<string, HashSet<string>> currentParallelPitchesByLeague;
    private HashSet<string> isMaxedOut;

    public override void ProcessBeforeGameday(SlottingContext context, List<Pitch> pitches, List<Game> games)
    {
        maxParallelPitchesByLeague = games
            .Select(g => g.Group.Type)
            .DistinctBy(t => t.Name)
            .Where(t => t.MaxParallelPitches < pitches.Count)
            .ToDictionary(t => t.Name, t => t.MaxParallelPitches);
        currentParallelPitchesByLeague = new Dictionary<string, HashSet<string>>(maxParallelPitchesByLeague.Count);
        isMaxedOut = new HashSet<string>(maxParallelPitchesByLeague.Count);
    }

    public override IEnumerable<Game> Apply(SlottingContext context, Pitch pitch, IEnumerable<Game> games, List<Pitch> pitches)
    {
        // return games which are either not maxed out (yet) or playing on the pitch already
        // TODO: check if there is enough space left for the league to finish on the maximum number of pitches
        return games.Where(g =>
                !isMaxedOut.Contains(g.Group.Type.Name)
                || currentParallelPitchesByLeague[g.Group.Type.Name].Contains(pitch.Name))
            ;
    }

    public override void Update(SlottingContext context, Pitch pitch, Game game)
    {
        var league = game.Group.Type.Name;
        if (!maxParallelPitchesByLeague.TryGetValue(league, out var maxParallelPitches))
        {
            return;
        }

        if (currentParallelPitchesByLeague.TryGetValue(league, out var current))
        {
            current.Add(pitch.Name);
            if (current.Count == maxParallelPitches)
            {
                isMaxedOut.Add(league);
            }
        }
        else
        {
            currentParallelPitchesByLeague.Add(league, new HashSet<string>(maxParallelPitches) { pitch.Name });
        }
    }

    public override IEnumerable<ValidationMessage> Validate(SlottingContext context, IReadOnlyList<Pitch> pitches)
        {
            var byGameDay = pitches.GroupBy(p => p.GameDay);
            foreach (var dayGroup in byGameDay)
            {
                var dayPitches = dayGroup.ToList();
                var dayPitchCount = dayPitches.Count;

                var restrictedLeagues = dayPitches
                    .SelectMany(p => p.Slots)
                    .Select(s => s.Game.Group.Type)
                    .DistinctBy(t => t.Name)
                    .Where(t => t.MaxParallelPitches > 0 && t.MaxParallelPitches < dayPitchCount)
                    .ToDictionary(t => t.Name, t => t.MaxParallelPitches);

                if (restrictedLeagues.Count == 0) continue;

                var allSlots = dayPitches.SelectMany(p => p.Slots.Select(s => new { PitchName = p.Name, Slot = s }));
                foreach (var timeGroup in allSlots.GroupBy(x => x.Slot.StartTime))
                {
                    foreach (var leagueGroup in timeGroup.GroupBy(x => x.Slot.Game.Group.Type.Name))
                    {
                        if (!restrictedLeagues.TryGetValue(leagueGroup.Key, out var maxParallel)) continue;

                        var pitchesServingLeague = leagueGroup.Select(x => x.PitchName).Distinct().ToList();
                        if (pitchesServingLeague.Count > maxParallel)
                        {
                            yield return new ValidationMessage(
                                $"League '{leagueGroup.Key}' uses {pitchesServingLeague.Count} pitches simultaneously"
                                    + $" at {timeGroup.Key:HH:mm} on game day {dayGroup.Key},"
                                    + $" but maximum is {maxParallel}.",
                                ValidationSeverity.Warning,
                                Code: "MAX_PARALLEL_PITCHES_VIOLATION",
                                GameDay: dayGroup.Key);
                        }
                    }
                }
            }
        }
}
