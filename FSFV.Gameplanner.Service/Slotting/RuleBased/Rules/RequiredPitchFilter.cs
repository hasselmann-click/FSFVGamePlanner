using FSFV.Gameplanner.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FSFV.Gameplanner.Service.Slotting.RuleBased.Rules;

internal class RequiredPitchFilter(int priority) : AbstractSlotRule(priority)
{
    private TimeSpan maxMinDurationAtGameDay;
    private Dictionary<string, string> requiredPitchByLeague;

    public override void ProcessBeforeGameday(List<Pitch> pitches, List<Game> games)
    {
        maxMinDurationAtGameDay = TimeSpan.FromMinutes(games.Select(g => g.Group.Type.MinDurationMinutes).Max());
        requiredPitchByLeague = games
            .Select(g => g.Group.Type)
            .DistinctBy(t => t.Name)
            .Where(t => !string.IsNullOrEmpty(t.RequiredPitchName))
            .ToDictionary(t => t.Name, t => t.RequiredPitchName);
    }

    public override IEnumerable<Game> Apply(Pitch pitch, IEnumerable<Game> games, List<Pitch> pitches)
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
            if (pitch.Name != requiredPitch)
            {
                continue;
            }

            // if this pitches next start time is later as this league would require it to finish,
            // we need to return this leagues games.
            var leagueGames = league.ToList();
            var (minDuration, parallelFactor) = leagueGames
                .Select(g => (g.Group.Type.MinDurationMinutes, g.Group.Type.ParallelGamesPerPitch))
                .First();
            var minRequiredTime = TimeSpan.FromMinutes(
                Math.Ceiling(leagueGames.Count / (double)parallelFactor) * minDuration);
            if (pitch.NextStartTime <= pitch.EndTime.Subtract(minRequiredTime.Add(maxMinDurationAtGameDay)))
            {
                continue;
            }

            return league.ToList();
        }

        // return games from leagues that don't have a required pitch or if this pitch is the required pitch 
        return games.Where(g =>
            !requiredPitchByLeague.TryGetValue(g.Group.Type.Name, out var requiredPitch)
                || requiredPitch == pitch.Name);
    }
}
