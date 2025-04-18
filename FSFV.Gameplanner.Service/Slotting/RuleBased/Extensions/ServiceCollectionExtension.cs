using FSFV.Gameplanner.Service.Slotting.RuleBased.Rules;
using FSFV.Gameplanner.Service.Slotting.RuleBased.Rules.RefereeUpdate;
using FSFV.Gameplanner.Service.Slotting.RuleBased.Rules.TargetState;
using FSFV.Gameplanner.Service.Slotting.RuleBased.Rules.ZkStartAndEnd;
using Microsoft.Extensions.DependencyInjection;

namespace FSFV.Gameplanner.Service.Slotting.RuleBased.Extensions;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddRuleBasedSlotting(this IServiceCollection services)
    {
        return services
            .AddScoped<ISlotService, RuleBasedSlotService>()

            .AddSingleton<TargetStateRuleConfigurationProvider>()
            .AddSingleton<ISlotRule>(sp => ActivatorUtilities.CreateInstance<TargetStateRule>(sp, 200_000))

            .AddSingleton<ISlotRule>(new RequiredPitchFilter(100_000))
            .AddSingleton<ISlotRule>(new MaxParallelPitchesFilter(10_000))
            .AddSingleton<ISlotRule>(new LeagueTogethernessFilter(5000))
            .AddSingleton<ISlotRule>(new LeaguePriorityFilter(1000))
            .AddSingleton<ISlotRule>(sp => ActivatorUtilities.CreateInstance<ZkStartAndEndFilter>(sp, 100))

            // Attention: Multiple sorts are kind of useless, because the last one will always win
            .AddSingleton<ISlotRule>(sp => ActivatorUtilities.CreateInstance<MorningAndEveningGamesSort>(sp, 50))

            // Special update "rule"
            .AddSingleton<ISlotRule>(sp => ActivatorUtilities.CreateInstance<RefereeUpdateRule>(sp, 1));
        ;
    }
}
