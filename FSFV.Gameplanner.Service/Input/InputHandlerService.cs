using System.Collections.Generic;
using System;
using FSFV.Gameplanner.Common.Dto.Input;
using FSFV.Gamplanner.Data.Context;
using FSFV.Gamplanner.Data.Model;
using System.Linq;
using FSFV.Gamplanner.Data.Model.Intermediary;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;

namespace FSFV.Gameplanner.Service.Input
{
    public class InputHandlerService
    {

        public async Task<int> StoreInputInDB(string inputJsonFile)
        {
            var inputDto = ReadInput(inputJsonFile);
            
            // input validation..?

            await using var context = new GameplannerDbContext();

            if (context.Teams.Any())
            {
                Console.WriteLine("Already data in database. Stopping.");
                return 0;
            }

            var season = new Season() { Description = inputDto.season };
            var leagues = inputDto.leagues.Select(l => new League() { Name = l.name, MachineName = l.machineName });
            var competitions = inputDto.competitions.Select(c => new Competition() { Name = c.name, MachineName = c.machineName });

            var teams = new List<Team>(inputDto.groupings.SelectMany(g => g.teams).Count());
            foreach (var grouping in inputDto.groupings) // 3 
            {

                var league = leagues.First(l => l.MachineName.Equals(grouping.machineName));
                foreach (var teamDto in grouping.teams) // 8 - 24
                {

                    var team = new Team() { Name = teamDto.name, HasZK = teamDto.hasZK };
                    foreach (var contestDto in teamDto.contests) // 1 - 2
                    {
                        var competition = competitions.First(c => c.MachineName.Equals(contestDto.machineName));
                        var contest = new Contest { Season = season, League = league, Competition = competition };
                        team.ContestTeams.Add(new ContestTeam { Contest = contest, Group = contestDto.group ?? 0, Team = team });
                    }
                    teams.Add(team);
                }
            }

            context.Teams.AddRange(teams);
            return await context.SaveChangesAsync();
        }

        private static StartInputDto ReadInput(string inputJsonFile)
        {
            var inputJson = File.ReadAllText(inputJsonFile);
            return JsonSerializer.Deserialize<StartInputDto>(inputJson);
        }

    }
}
