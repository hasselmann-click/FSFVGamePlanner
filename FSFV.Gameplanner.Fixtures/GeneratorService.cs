using Microsoft.Extensions.Logging;

namespace FSFV.Gameplanner.Fixtures;

public class GeneratorService
{

    private readonly ILogger<GeneratorService> logger;

    public GeneratorService(ILogger<GeneratorService> logger)
    {
        this.logger = logger;
    }

    /// <summary>
    /// Create all possible, single leg (!) fixtures for the given teams.
    /// In case of uneven teams, the placeholder will be used.
    /// </summary>
    /// <param name="teams">The participating teams</param>
    /// <param name="placeHolder">The placeholder used in case of an uneven number of teams</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public List<Fixture> Fix(string[] teams, string placeHolder = "SPIELFREI")
    {

        if (teams is null)
        {
            throw new ArgumentNullException(nameof(teams));
        }

        List<string> teamsList;
        if ((teams.Length & 1) == 1) // uneven number of teams
        {
            teamsList = new List<string>(teams.Length + 1);
            teamsList.AddRange(teams);
            teamsList.Add(placeHolder);
        }
        else
        {
            teamsList = teams.ToList();
        }

        var table = GameCreatorUtil.GenerateGameTable(teamsList.Count);
#pragma warning disable CA2254 // Template should be a static expression
        logger.LogDebug(GameCreatorUtil.WriteTable(table));
#pragma warning restore CA2254 // Template should be a static expression
        var games = GameCreatorUtil.CreateGameList(teamsList, table);

        return games.OrderBy(g => g.GameDay).ThenBy(g => g.GameDayOrder).ToList();
    }

}
