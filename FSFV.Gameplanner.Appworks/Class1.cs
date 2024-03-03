using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSFV.Gameplanner.Appworks;
public class AppworksImportRecord
{

    public static readonly string DateFormat = "dd/mm/yyyy HH:ii";
    private DateTime start1;

    public int matchday_id { get; set; }
    public int division_id { get; set; }
    public int location_id { get; set; }
    public int home_team_id { get; set; }
    public int away_team_id { get; set; }
    public string start { get => start1.ToString(DateFormat); set => start1 = value; }
    public int referee_team_id { get; set; }

}
