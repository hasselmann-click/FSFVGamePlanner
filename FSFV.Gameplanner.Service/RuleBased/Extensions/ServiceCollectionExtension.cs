using FSFV.Gameplanner.Service.RuleBased.Rules;
using FSFV.Gameplanner.Service.Rules;
using Microsoft.Extensions.DependencyInjection;

namespace FSFV.Gameplanner.Service.RuleBased.Extensions;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddRuleBasedSlotting(this IServiceCollection services)
    {
        return services
            .AddScoped<ISlotService, RuleBasedSlotService>()
            //.AddSingleton<ISlotRule>(new SortHomeAlphabetically(100))
            .AddSingleton<ISlotRule>(new RequiredPitchFilter(1000))
            .AddSingleton<ISlotRule>(new MaxParallelPitchesFilter(100))
            .AddSingleton<ISlotRule>(new LeaguePriorityFilter(10))
            // TODO currently only one sorting rule can be applied
            .AddSingleton<ISlotRule>(new MorningAndEveningGamesSort(1))
            ;
    }
}
