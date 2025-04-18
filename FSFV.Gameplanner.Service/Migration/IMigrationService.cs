using FSFV.Gameplanner.Service.Serialization.Dto;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FSFV.Gameplanner.Service.Migration;

public interface IMigrationService
{
    public Task<List<GameplanGameDto>> RunMigrations(List<MigrationDto> migrations, List<GameplanGameDto> gamePlan);
}
