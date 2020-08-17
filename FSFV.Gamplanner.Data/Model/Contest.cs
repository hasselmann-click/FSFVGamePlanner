using FSFV.Gamplanner.Data.Model.Intermediary;
using System.Collections.Generic;

namespace FSFV.Gamplanner.Data.Model
{
    public partial class Contest
    {

        public int ContestID { get; set; }

        public League League { get; set; }

        public Competition Competition { get; set; }

        public Season Season { get; set; }

        public ICollection<ContestTeam> ContestTeams { get; set; }
    }
}
