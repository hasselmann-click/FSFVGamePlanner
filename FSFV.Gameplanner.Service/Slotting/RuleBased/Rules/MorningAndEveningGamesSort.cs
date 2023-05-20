using FSFV.Gameplanner.Common;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FSFV.Gameplanner.Service.Slotting.RuleBased.Rules;

internal class MorningAndEveningGamesSort : AbstractSlotRule
{

    private readonly TimeSpan EveningSince;
    private readonly TimeSpan MorningUntil;

    public MorningAndEveningGamesSort(int priority, IConfiguration configuration) : base(priority)
    {
        MorningUntil = configuration.GetValue<TimeSpan>("Schedule:MorningUntil");
        EveningSince = configuration.GetValue<TimeSpan>("Schedule:EveningSince");
    }

    public override IEnumerable<Game> Apply(Pitch pitch, IEnumerable<Game> games, List<Pitch> pitches)
    {
        var currentSlot = pitch.NextStartTime.TimeOfDay;

        // is morning?
        if (currentSlot <= MorningUntil)
        {
            return games.OrderBy(g => g.Home.MorningGames + g.Away.MorningGames);
        }

        // is evening?
        if (currentSlot >= EveningSince)
        {
            return games.OrderBy(g => g.Home.EveningGames + g.Away.EveningGames);
        }

        // if midday, use the teams with the most evening games already
        return games.OrderByDescending(g => g.Home.EveningGames + g.Away.EveningGames);
    }

    public override void ProcessAfterGameday(List<Pitch> pitches)
    {
        // update morning games
        pitches.SelectMany(p => p.Slots)
            .Where(s => s.StartTime.TimeOfDay <= MorningUntil)
            .SelectMany(s => new[] { s.Game.Home, s.Game.Away })
            .ToList()
            .ForEach(t =>
            {
                t.MorningGames += 1;
            });

        // update evening games
        pitches.SelectMany(p => p.Slots)
            .Where(s => s.StartTime.TimeOfDay >= EveningSince)
            .SelectMany(s => new[] { s.Game.Home, s.Game.Away })
            .ToList()
            .ForEach(t =>
            {
                t.EveningGames += 1;
            });
    }
}
