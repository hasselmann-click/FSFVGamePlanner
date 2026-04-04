
using QuestPDF.Infrastructure;
using System.Text.Json.Serialization;

namespace FSFV.Gameplanner.Pdf
{
    public class PdfConfig
    {
        public string HeaderTitle { get; set; } = string.Empty;

        [JsonConverter(typeof(ColorConverter))]
        public Dictionary<string, Color> LeagueColors { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public string FooterDateFormat { get; set; } = "dd.MM.yyyy";
        public string GameStartTimeFormat { get; set; } = "HH:mm";

        [JsonConverter(typeof(ColorConverter))]
        public Color HolidayColor { get; set; } = Color.FromHex("#DCD9C5");
    }
}
