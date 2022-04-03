using System;

namespace FSFV.Gameplanner.Common.Dto
{
    public class Game
    {
        public Group Group { get; set; }
        public Team Home { get; set; }
        public Team Away { get; set; }
        public Team Referee { get; set; }

        public TimeSpan MinDuration { get; set; }
    }
}
