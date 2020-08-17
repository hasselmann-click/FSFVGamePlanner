using FSFV.Gameplanner.Service.Input;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
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

            await new InputHandlerService().StoreInputInDB(inputJsonFile);


        }

        private static readonly Dictionary<string, string> ArgsKeys = new Dictionary<string, string>()
        {
            {"-i", Arguments.Input }
        };

    }
}
