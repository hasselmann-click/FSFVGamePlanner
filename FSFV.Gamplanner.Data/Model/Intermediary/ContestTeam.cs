namespace FSFV.Gamplanner.Data.Model.Intermediary
{
    public class ContestTeam
    {
        public int TeamID { get; set; }
        public int ContestID { get; set; }

        public int Group { get; set; }

        public Team Team { get; set; }
        public Contest Contest { get; set; }
    }
}
