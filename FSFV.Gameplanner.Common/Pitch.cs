using FSFV.Gameplanner.Common.Dto;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FSFV.Gameplanner.Common;

public class Pitch
{
    public string Name { get; set; }
    public int GameDay { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    public List<Game> Games { get; set; } = new(10);
    public List<TimeSlot> Slots { get; set; } = new(10);

    public TimeSpan TimeLeft => (EndTime - StartTime)
        .Subtract(Games.Select(g => g.MinDuration.Divide(g.Group.Type.ParallelGamesPerPitch))
            .Aggregate(TimeSpan.Zero, (d1, d2) => d1.Add(d2)));

    public TimeOnly NextStartTime => StartTime
            .Add(Games.Aggregate(TimeSpan.Zero, (t1, g2) => t1.Add(g2.MinDuration)));
}
