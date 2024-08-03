using FSFV.Gameplanner.Common.Rng;
using FSFV.Gameplanner.Service.Slotting;
using FSFV.Gameplanner.Service.Slotting.RuleBased.Rules;
using FSFV.Gameplanner.Service.Slotting.RuleBased.Rules.RefereeUpdate;
using FSFV.Gameplanner.Service.Slotting.RuleBased.Rules.Special;
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

            .AddSingleton<ISlotRule>(new LCupRule(100_001))

            .AddSingleton<ISlotRule>(new RequiredPitchFilter(100_000))
            .AddSingleton<ISlotRule>(new MaxParallelPitchesFilter(10_000))
            .AddSingleton<ISlotRule>(new LeagueTogethernessFilter(5000))
            .AddSingleton<ISlotRule>(new LeaguePriorityFilter(1000))
            .AddSingleton<ISlotRule>(sp => ActivatorUtilities.CreateInstance<ZkStartAndEndFilter>(sp, 100))

            // Attention: Multiple sorts are kind of useless, because the last one will always win
            .AddSingleton<ISlotRule>(sp => ActivatorUtilities.CreateInstance<MorningAndEveningGamesSort>(sp, 50))
            .AddSingleton<ISlotRule>(sp => new ManualSortRule(10))

            // Special update "rule"
            .AddSingleton<ISlotRule>(sp => ActivatorUtilities.CreateInstance<RefereeUpdateRule>(sp, 1));
        ;
    }
}
