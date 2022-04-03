using FSFV.Gameplanner.Common.Dto;
using FSFV.Gameplanner.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace FSFV.Gameplanner.ConsoleRunner;

class Program
{

    static void Main(string[] args)
    {

        if (args.Length < 2)
            throw new ArgumentException("Missing arguments");

        // TODO validate pitch and fixture files
        var pitchesFile = args[0];
        var fixtureFiles = new List<string>(args.Length - 1);
        for(int i = 1; i < args.Length; ++i)
        {
            fixtureFiles.Add(args[i]);
        }

        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        List<Pitch> pitches = ParsePitches(pitchesFile);
        Dictionary<string, GroupType> groupTypes = ParseGroupTypes(configuration);
        List<Game> fixtures = ParseFixutures(groupTypes, fixtureFiles);

        ConfigureServices(configuration)
            .GetRequiredService<Runner>()
            .Run(pitches, fixtures);
    }

    private static Dictionary<string, GroupType> ParseGroupTypes(IConfigurationRoot configuration)
    {
        throw new NotImplementedException();
    }

    private static List<Game> ParseFixutures(Dictionary<string, GroupType> groupTypes, List<string> fixtureFiles)
    {
        throw new NotImplementedException();
    }

    private static List<Pitch> ParsePitches(string pitchesFile)
    {
        throw new NotImplementedException();
    }

    private static ServiceProvider ConfigureServices(IConfigurationRoot configuration)
    {
        return new ServiceCollection()
            .AddLogging(b => b.AddConsole())
            .AddSingleton(configuration)
            .AddSingleton<Runner>()
            .AddScoped<SlotService>()
            .BuildServiceProvider();
    }
}
