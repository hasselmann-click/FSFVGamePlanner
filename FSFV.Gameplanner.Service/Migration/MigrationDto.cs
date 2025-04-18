using System;

namespace FSFV.Gameplanner.Service.Migration;
public class MigrationDto
{

    public Row Is { get; set; }
    public Row Target { get; set; }

    public class Row
    {
        public DateOnly Date { get; set; }
        public TimeOnly Time { get; set; }
        public string Pitch { get; set; }
        public string Home { get; set; }
        public string Away { get; set; }
        public string Referee { get; set; }
        public string Group { get; set; }
        public string League { get; set; }
    }
}
