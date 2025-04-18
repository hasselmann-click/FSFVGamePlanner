using System;

namespace FSFV.Gameplanner.Common;

public class TimeSlot
{
    public Game Game { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
}
