using FSFV.Gameplanner.Common;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FSFV.Gameplanner.Service.Slotting;

public abstract class AbstractSlotService(ILogger logger) : ISlotService
{

    protected static readonly Game PLACEHOLDER = new() { Group = new() { Type = new() { MinDurationMinutes = 0 } } };
    // TODO: make configurable. Implement IConfigurationProvider?
    protected static readonly TimeSpan MaxSlotTime = TimeSpan.FromMinutes(120);

    protected ILogger Logger => logger;

    public abstract List<Pitch> SlotGameDay(List<Pitch> pitches, List<Game> games);

}