using FSFV.Gameplanner.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSFV.Gameplanner.ConsoleRunner
{
    class Program
    {

        static void Main(string[] args)
        {
            ConfigureServices().GetRequiredService<Runner>().Run();
        }

        private static ServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddLogging(b => b.AddConsole())
                .AddSingleton<Runner>()
                .AddTransient<SlotService>()
                .BuildServiceProvider();
        }
    }
}
