using FSFV.Gameplanner.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace FSFV.Gameplanner.ConsoleRunner;

class Program
{

    static Task Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();
        var serviceProvider = ConfigureServices(configuration);
        return serviceProvider.GetRequiredService<Runner>().Run(args);
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
            .AddScoped<SlotService>()
            .BuildServiceProvider();
    }
}
