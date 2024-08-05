using FSFV.Gameplanner.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace FSFV.Gameplanner.Service.Slotting.RuleBased.Rules.Special;

internal class LCupRule(int priority) : AbstractSlotRule(priority)
{
    public override IEnumerable<Game> Apply(Pitch pitch, IEnumerable<Game> games, List<Pitch> pitches)
    {
        // In HS 24, L plays a tournament over 2 days from 12 - 16
        // This requires a league config "LCup", with 60min single games
        bool execute = pitch.GameDay is 5 or 6
            && pitch.Name == "R2"
            && pitch.NextStartTime.CompareTo(new DateTime(2024, 09, 22, 11, 00, 00)) > 0
            && pitch.NextStartTime.CompareTo(new DateTime(2024, 09, 22, 15, 59, 00)) < 0
            && pitch.NextStartTime.CompareTo(new DateTime(2024, 09, 29, 11, 00, 00)) > 0
            && pitch.NextStartTime.CompareTo(new DateTime(2024, 09, 29, 15, 59, 00)) < 0;

        if (execute)
        {
            return games.Where(g => g.Group.Type.Name == "LCup");
        }
        else
        {
            return games.Where(g => g.Group.Type.Name != "LCup");
        }
    }

    public override void ProcessAfterGameday(List<Pitch> pitches)
    {
        if (pitches.FirstOrDefault().GameDay is not 5 and not 6) return;

        var slots = pitches.First(p => p.Name == "R2").Slots;
        var idx = slots.FindIndex(s => s.Game.Group.Type.Name == "LCup");
        int hour = 11;
        for (; idx < slots.Count; ++idx)
        {
            var slot = slots[idx];
            slot.StartTime = new TimeOnly(++hour, 0);
        }

        base.ProcessAfterGameday(pitches);
    }
}
