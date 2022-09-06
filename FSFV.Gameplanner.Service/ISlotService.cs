using FSFV.Gameplanner.Common;
using System.Collections.Generic;

namespace FSFV.Gameplanner.Service
{
    public interface ISlotService
    {
        List<Pitch> SlotGameDay(List<Pitch> pitches, List<Game> games);
    }
}