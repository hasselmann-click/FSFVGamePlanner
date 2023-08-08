using FSFV.Gameplanner.Service.Slotting;
using FSFV.Gameplanner.Service.Slotting.RuleBased.Rules;
using FSFV.Gameplanner.Service.Slotting.RuleBased.Rules.ZkStartAndEnd;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace FSFV.Gameplanner.Service.Slotting.RuleBased.Extensions;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddRuleBasedSlotting(this IServiceCollection services)
    {
        return services
            .AddScoped<ISlotService, RuleBasedSlotService>()

            .AddSingleton<ISlotRule>(new RequiredPitchFilter(100_000))
            .AddSingleton<ISlotRule>(new MaxParallelPitchesFilter(10_000))
            .AddSingleton<ISlotRule>(new LeagueTogethernessFilter(5000))
            .AddSingleton<ISlotRule>(new LeaguePriorityFilter(1000))
            .AddSingleton<ISlotRule>(sp => new ZkStartAndEndFilter(100,
                sp.GetRequiredService<IConfiguration>(),
                sp.GetRequiredService<ILogger<ZkStartAndEndFilter>>(),
                sp.GetRequiredService<Random>()))

            // Attention: Multiple sorts are kind of useless, because the last one will always win
            .AddSingleton<ISlotRule>(sp => new MorningAndEveningGamesSort(50, sp.GetRequiredService<IConfiguration>()))
            .AddSingleton<ISlotRule>(sp => new ManualSortRule(10))
            ;
    }
}
