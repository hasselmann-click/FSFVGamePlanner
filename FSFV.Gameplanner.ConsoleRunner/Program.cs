using FSFV.Gameplanner.Service.Fixtures;
using FSFV.Gameplanner.Service.Input;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FSFV.Gameplanner.ConsoleRunner
{
    class Program
    {

        public class Arguments
        {
            public const string Input = "input";
        }

        static async Task Main(string[] args)
        {
            var cmdOptions = new ConfigurationBuilder().AddCommandLine(args, ArgsKeys).Build();
            var inputJsonFile = cmdOptions[Arguments.Input];

            var teams = await new InputHandlerService().StoreInputInDB(inputJsonFile);
            var games = GameCreatorUtil.GetFixtures(teams);

            foreach (var game in games.OrderBy(g => g.GameDay).ThenBy(g => g.GameDayOrder))
            {
                Console.WriteLine(game);
            }

        }

        private static readonly Dictionary<string, string> ArgsKeys = new Dictionary<string, string>()
        {
            {"-i", Arguments.Input }
        };

    }
}
