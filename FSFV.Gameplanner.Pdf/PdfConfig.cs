
using QuestPDF.Infrastructure;
using System.Text.Json.Serialization;

namespace FSFV.Gameplanner.Pdf
{
    public class PdfConfig
    {
        public string HeaderTitle { get; set; } = string.Empty;

        public Dictionary<string, string> LeagueColors { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public string FooterDateFormat { get; set; } = "dd.MM.yyyy";
        public string GameStartTimeFormat { get; set; } = "HH:mm";

        public string? HolidayColor { get; set; }
    }
}
