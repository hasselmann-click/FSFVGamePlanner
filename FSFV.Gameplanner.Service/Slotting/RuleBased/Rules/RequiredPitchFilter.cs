using FSFV.Gameplanner.Common;
using FSFV.Gameplanner.Service.Slotting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FSFV.Gameplanner.Service.Slotting.RuleBased.Rules;

internal class RequiredPitchFilter(int priority) : AbstractSlotRule(priority)
{
    private TimeSpan maxMinDurationAtGameDay;
    private Dictionary<string, string> requiredPitchByLeague;

    public override void ProcessBeforeGameday(SlottingContext context, List<Pitch> pitches, List<Game> games)
    {
        maxMinDurationAtGameDay = TimeSpan.FromMinutes(games.Select(g => g.Group.Type.MinDurationMinutes).Max());
        requiredPitchByLeague = games
            .Select(g => g.Group.Type)
            .DistinctBy(t => t.Name)
            .Where(t => !string.IsNullOrEmpty(t.RequiredPitchName))
            .ToDictionary(t => t.Name, t => t.RequiredPitchName);
    }

    public override IEnumerable<Game> Apply(SlottingContext context, Pitch pitch, IEnumerable<Game> games, List<Pitch> pitches)
    {
        if (requiredPitchByLeague.Count == 0)
        {
            return games;
        }

        // get the games where this pitch is required
        var requiredLeagues = games
            .Where(g => requiredPitchByLeague.ContainsKey(g.Group.Type.Name))
            .GroupBy(g => g.Group.Type.Name);
        if (!requiredLeagues.Any())
        {
            // no requiring games, so we can move on
            return games;
        }

        // check if we need to prioritise a requiring league
        foreach (var league in requiredLeagues)
        {
            // if this is not the required pitch, continue
            var requiredPitch = requiredPitchByLeague[league.Key];
            if (GetPitchDisplayName(pitch) != requiredPitch)
            {
                continue;
            }

            // if this pitches next start time is later as this league would require it to finish,
            // we need to return this leagues games.
            var leagueGames = league.ToList();
            var (minDuration, parallelFactor) = leagueGames
                .Select(g => (g.Group.Type.MinDurationMinutes, g.Group.Type.ParallelGamesPerPitch))
                .First();
            var minRequiredTime = TimeSpan.FromMinutes(Math.Ceiling(leagueGames.Count / (double)parallelFactor) * minDuration);
            if (pitch.NextStartTime <= (pitch.EndTime.Add(minRequiredTime.Add(maxMinDurationAtGameDay).Negate())))
            {
                continue;
            }

            return [.. league];
        }

        // return games from leagues that don't have a required pitch or if this pitch is the required pitch 
        return games.Where(g =>
            !requiredPitchByLeague.TryGetValue(g.Group.Type.Name, out var requiredPitch)
                || requiredPitch == GetPitchDisplayName(pitch));
    }

    public override IEnumerable<ValidationMessage> Validate(SlottingContext context, IReadOnlyList<Pitch> pitches)
        {
            var requiredPitchByLeague = pitches
                .SelectMany(p => p.Slots)
                .Select(s => s.Game.Group.Type)
                .DistinctBy(t => t.Name)
                .Where(t => !string.IsNullOrEmpty(t.RequiredPitchName))
                .ToDictionary(t => t.Name, t => t.RequiredPitchName!);

            if (requiredPitchByLeague.Count == 0) yield break;

            foreach (var pitch in pitches)
            {
                foreach (var slot in pitch.Slots)
                {
                    var leagueName = slot.Game.Group.Type.Name;
                    if (requiredPitchByLeague.TryGetValue(leagueName, out var requiredPitch)
                        && GetPitchDisplayName(pitch) != requiredPitch)
                    {
                        yield return new ValidationMessage(
                            $"Game {slot.Game.Home.Name} vs {slot.Game.Away.Name} (league '{leagueName}')"
                                + $" is on pitch '{GetPitchDisplayName(pitch)}' but must be on pitch '{requiredPitch}'.",
                            ValidationSeverity.Error,
                            Code: "REQUIRED_PITCH_VIOLATION",
                            GameDay: pitch.GameDay,
                            PitchName: GetPitchDisplayName(pitch));
                    }
                }
            }
        }

    private static string GetPitchDisplayName(Pitch pitch)
    {
        return string.IsNullOrWhiteSpace(pitch.DisplayName)
            ? pitch.Name
            : pitch.DisplayName;
    }
}
