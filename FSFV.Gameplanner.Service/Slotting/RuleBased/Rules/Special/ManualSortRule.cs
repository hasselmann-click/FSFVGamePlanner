using FSFV.Gameplanner.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSFV.Gameplanner.Service.Slotting.RuleBased.Rules.Special;

/// <summary>
/// This is a custom sort rule, which should only be used in special circumstances.
/// E.g. in 2023 Furttals have a wedding coming up, but want still be able to play.
/// </summary>
internal class ManualSortRule(int priority) : AbstractSlotRule(priority)
{
    public override IEnumerable<Game> Apply(Pitch pitch, IEnumerable<Game> games, List<Pitch> pitches)
    {

        var currentDay = pitch.NextStartTime.Date;
        var furttalLast1 = DateTime.Parse("27.08.2023");
        var auroraFirst = DateTime.Parse("03.09.2023");
        var auroraLast = DateTime.Parse("24.09.2023");
        var furtalLast2 = DateTime.Parse("01.10.2023");


        const string Furttal = "Furttals Finest";
        const string Aurora = "Aurora";
        if (currentDay == furttalLast1)
        {
            return MoveTeamLast(games, Furttal);
        }
        else if (currentDay == auroraFirst)
        {
            return MoveTeamFirst(games, Aurora);
        }
        else if (currentDay == auroraLast)
        {
            return MoveTeamLast(games, Aurora);
        }
        else if (currentDay == furtalLast2)
        {
            return MoveTeamLast(games, Furttal);
        }

        return games;
    }

    private static IEnumerable<Game> MoveTeamFirst(IEnumerable<Game> games, string teamName)
    {
        var i = games.FirstOrDefault(g => g.Home.Name == teamName || g.Away.Name == teamName);
        if (i != null)
        {
            return games.Where(g => g != i).Prepend(i);
        }
        return games;
    }

    private static IEnumerable<Game> MoveTeamLast(IEnumerable<Game> games, string teamName)
    {
        var i = games.FirstOrDefault(g => g.Home.Name == teamName || g.Away.Name == teamName);
        if (i != null)
        {
            return games.Where(g => g != i).Append(i);
        }
        return games;
    }

    public override void ProcessAfterGameday(List<Pitch> pitches)
    {
    }
}
