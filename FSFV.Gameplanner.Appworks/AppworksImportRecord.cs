namespace FSFV.Gameplanner.Appworks;

public record AppworksImportRecord(int matchday_id, int division_id, int location_id, int home_team_id, int away_team_id, DateTime start, int? referee_team_id)
{
    public static readonly string DateFormat = "dd/MM/yyyy HH:mm";
}
