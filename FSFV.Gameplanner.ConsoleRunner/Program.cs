using FSFV.Gameplanner.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace FSFV.Gameplanner.ConsoleRunner;

class Program
{

    static void Main(string[] args)
    {

        if (args.Length == 0)
            throw new ArgumentException("Missing arguments");



        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();
        ConfigureServices(configuration).GetRequiredService<Runner>().Run();
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
