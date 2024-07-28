
using QuestPDF.Infrastructure;
using System.Text.Json.Serialization;

namespace FSFV.Gameplanner.Pdf
{
    public class PdfConfig
    {
        public string HeaderTitle { get; set; }

        [JsonConverter(typeof(ColorConverter))]
        public Dictionary<string, Color> LeagueColors { get; set; }
        public string FooterDateFormat { get; set; }
        public string GameStartTimeFormat { get; set; }
    }
}
