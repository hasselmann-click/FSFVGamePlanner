namespace FSFV.Gamplanner.Data.Model
{
    public class Game
    {
        public int GameID { get; set; }

        public int GameDay { get; set; }
        public int GameDayOrder { get; set; }

        public Team Home { get; set; }
        public Team Away { get; set; }
        public Team Referee { get; set; }




    }
}
