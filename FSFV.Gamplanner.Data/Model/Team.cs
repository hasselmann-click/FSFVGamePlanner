using FSFV.Gamplanner.Data.Model.Intermediary;
using System.Collections.Generic;

namespace FSFV.Gamplanner.Data.Model
{
    public partial class Team
    {

        public Team()
        {
            ContestTeams = new List<ContestTeam>();
        }

        public int TeamID { get; set; }

        public string Name { get; set; }

        public bool HasZK { get; set; }

        public ICollection<ContestTeam> ContestTeams { get; set; }

    }
}
