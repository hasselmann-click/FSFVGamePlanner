using FSFV.Gameplanner.Appworks.Mappings;
using FSFV.Gameplanner.Appworks.Mappings.File;
using FSFV.Gameplanner.Appworks.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace FSFV.Gameplanner.Appworks;
public static class ServiceRegistry
{

    public static IServiceCollection AddAppworksServices(this IServiceCollection services)
    {
        return services
            .AddTransient<IAppworksSerializer, AppworksSerializer>()
            .AddTransient<AppworksTransformerFactory>();
            ;
    }

}
