namespace FSFV.Gameplanner.Pdf;

public class PdfConfigOverride
{
    public string? HeaderTitle { get; set; }
    public Dictionary<string, string>? LeagueColors { get; set; }
    public string? FooterDateFormat { get; set; }
    public string? GameStartTimeFormat { get; set; }
}
