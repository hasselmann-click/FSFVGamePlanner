using System;

namespace FSFV.Gameplanner.Common;

public class Game
{
    public int GameDay { get; set; }
    public Group Group { get; set; }
    public Team Home { get; set; }
    public Team Away { get; set; }
    public Team Referee { get; set; }

    // TODO cache timespan
    public TimeSpan MinDuration => TimeSpan.FromMinutes(Group.Type.MinDurationMinutes);
}
