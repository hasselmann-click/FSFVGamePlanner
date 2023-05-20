using FSFV.Gameplanner.Service.Slotting.RuleBased.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace FSFV.Gameplanner.ConsoleRunner;

class Program
{

    private static readonly Random RNG = new(23432546);

    static async Task Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();
        var serviceProvider = ConfigureServices(configuration);
        await serviceProvider.GetRequiredService<Runner>().Run(args);

        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }

    private static ServiceProvider ConfigureServices(IConfigurationRoot configuration)
    {
        return new ServiceCollection()
            .AddLogging(config =>
            {
                config.ClearProviders();
                config.AddConfiguration(configuration.GetSection("Logging"));
                config.AddConsole();
            })
            .AddSingleton<IConfiguration>(configuration)
            .AddSingleton<Runner>()
            //.AddScoped<ISlotService, SlotService>()
            //.AddScoped<ISlotService, LinearSlotService>()
            .AddRuleBasedSlotting()
            .AddSingleton(RNG)
            .BuildServiceProvider();
    }
}
