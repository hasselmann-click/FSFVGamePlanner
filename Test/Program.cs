using FSFV.Gameplanner.Service.Fixtures;
using FSFV.Gamplanner.Data.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {

            /*
            Gruppe A

-1.FC Brennivin
- Saints
- Zurich United
- Tabula Rasant
- El Social


Gruppe B

- Zenit Emilie
- BSC Tram 5
- Tabula Entspannt
- AS Lettenwiese
- Schachtjor Intermezzo
            */

            var teams = new List<Team>
            {
                new Team { TeamID = 1, Name = "1.FC Brennivin"},
                new Team { TeamID = 2, Name = "Saints"},
                new Team { TeamID = 3, Name = "Zurich United"},
                new Team { TeamID = 4, Name = "Tabula Rasant"},
                new Team { TeamID = 5, Name = "El Social"}
            };

            var games = old_GameCreatorUtil.GetFixtures(teams);

            foreach (var game in games.OrderBy(g => g.GameDay).ThenBy(g => g.GameDayOrder))
            {
                Console.WriteLine(game);
            }
        }
    }
}
