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

            // TODO currently only one sorting rule can be applied
            .AddSingleton<ISlotRule>(new MorningAndEveningGamesSort(1))
            ;
    }
}
