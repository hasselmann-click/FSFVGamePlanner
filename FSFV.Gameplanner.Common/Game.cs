using System;

namespace FSFV.Gameplanner.Common;

public class Game
{
    public int GameDay { get; set; }
    public Team Home { get; set; }
    public Team Away { get; set; }
    public Team Referee { get; set; }
    public TimeSpan MinDuration { get; set; }
    
    private Group group;
    public Group Group
    {
        get { return group; }
        set
        {
            MinDuration = TimeSpan.FromMinutes(value.Type.MinDurationMinutes);
            group = value;
        }
    }
}
