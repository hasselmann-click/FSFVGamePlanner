﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace FSFV.Gameplanner.Common.Dto
{
    public class Pitch
    {
        public PitchType Type { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public List<Game> Games { get; set; } = new(10);
        public List<TimeSlot> Slots { get; set; } = new(10);
        // TODO test performance
        public TimeSpan TimeLeft => EndTime.Subtract(StartTime)
            .Subtract(Games.Select(g => g.MinDuration)
                .Aggregate(TimeSpan.Zero, (d1, d2) => d1.Add(d2)));
    }
}