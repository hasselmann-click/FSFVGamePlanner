using System;

namespace FSFV.Gameplanner.Common;

public class TimeSlot
{
    public Game Game { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}
