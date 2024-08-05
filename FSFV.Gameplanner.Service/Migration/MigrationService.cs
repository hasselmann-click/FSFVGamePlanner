using FSFV.Gameplanner.Service.Serialization.Dto;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FSFV.Gameplanner.Service.Migration;
public class MigrationService(ILogger<MigrationService> logger) : IMigrationService
{
    public Task<List<GameplanGameDto>> RunMigrations(List<MigrationDto> migrations, List<GameplanGameDto> gamePlan)
    {
        logger.LogDebug("Would have run migrations");
        return Task.FromResult(gamePlan);
    }
}
