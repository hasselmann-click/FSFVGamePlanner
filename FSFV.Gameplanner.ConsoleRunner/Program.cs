using FSFV.Gameplanner.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace FSFV.Gameplanner.ConsoleRunner;

class Program
{

    static async Task Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();
        var serviceProvider = ConfigureServices(configuration);

        // TODO expect directory with fixture files instead of single files..
        await serviceProvider.GetRequiredService<Runner>().Run(args);
    }

    private static ServiceProvider ConfigureServices(IConfigurationRoot configuration)
    {
        return new ServiceCollection()
            .AddLogging(b => b.AddConsole())
            .AddSingleton<IConfiguration>(configuration)
            .AddSingleton<Runner>()
            .AddScoped<SlotService>()
            .BuildServiceProvider();
    }
}
