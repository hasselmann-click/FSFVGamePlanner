using FSFV.Gamplanner.Data.Model;

namespace FSFV.Gameplanner.Data.Model
{
    public partial class Game
    {
        public int GameID { get; set; }

        public int GameDay { get; set; }
        public int GameDayOrder { get; set; }

        public Team Home { get; set; }
        public Team Away { get; set; }
        public Team Referee { get; set; }

    }
}
