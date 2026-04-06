using FSFV.Gameplanner.Common;
using FSFV.Gameplanner.Service.Slotting;
using System.Collections.Generic;
using System.Linq;

namespace FSFV.Gameplanner.Service.Slotting.RuleBased.Rules;

/// <summary>
/// A validation-only rule that performs structural consistency checks on a scheduled gameplan.
/// All generation methods are intentional no-ops; only <see cref="Validate"/> produces output.
/// </summary>
internal class ConsistencyChecksRule(int priority) : AbstractSlotRule(priority)
{
    // ── Generation no-ops ────────────────────────────────────────────────────

    public override IEnumerable<Game> Apply(
        SlottingContext context, Pitch pitch, IEnumerable<Game> games, List<Pitch> pitches)
        => games; // passthrough — does not influence scheduling

    // ── Validation ───────────────────────────────────────────────────────────

    public override IEnumerable<ValidationMessage> Validate(
        SlottingContext context, IReadOnlyList<Pitch> pitches)
    {
        foreach (var msg in CheckPitchBoundaries(pitches)) yield return msg;
        foreach (var msg in CheckPitchOverlaps(pitches)) yield return msg;

        if (context.ExpectedFixtures is { } expectedFixtures)
        {
            foreach (var msg in CheckFixtureCoverage(pitches, expectedFixtures)) yield return msg;
        }
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private static IEnumerable<ValidationMessage> CheckPitchBoundaries(IReadOnlyList<Pitch> pitches)
    {
        foreach (var pitch in pitches)
        {
            foreach (var slot in pitch.Slots)
            {
                if (slot.StartTime < pitch.StartTime)
                {
                    yield return new ValidationMessage(
                        $"Slot {slot.Game.Home.Name} vs {slot.Game.Away.Name} on pitch '{pitch.Name}'"
                            + $" starts at {slot.StartTime:HH:mm}, before pitch opens at {pitch.StartTime:HH:mm}.",
                        ValidationSeverity.Error,
                        Code: "SLOT_BEFORE_PITCH_START",
                        GameDay: pitch.GameDay,
                        PitchName: pitch.Name);
                }

                if (slot.EndTime > pitch.EndTime)
                {
                    yield return new ValidationMessage(
                        $"Slot {slot.Game.Home.Name} vs {slot.Game.Away.Name} on pitch '{pitch.Name}'"
                            + $" ends at {slot.EndTime:HH:mm}, after pitch closes at {pitch.EndTime:HH:mm}.",
                        ValidationSeverity.Warning,
                        Code: "SLOT_AFTER_PITCH_END",
                        GameDay: pitch.GameDay,
                        PitchName: pitch.Name);
                }
            }
        }
    }

    private static IEnumerable<ValidationMessage> CheckPitchOverlaps(IReadOnlyList<Pitch> pitches)
    {
        foreach (var pitch in pitches)
        {
            var sorted = pitch.Slots.OrderBy(s => s.StartTime).ThenBy(s => s.EndTime).ToList();
            for (int i = 0; i < sorted.Count - 1; i++)
            {
                var current = sorted[i];
                var next = sorted[i + 1];

                // Parallel slots intentionally share the same start time — not a conflict
                if (current.StartTime == next.StartTime) continue;

                if (current.EndTime > next.StartTime)
                {
                    yield return new ValidationMessage(
                        $"Pitch '{pitch.Name}' has overlapping slots on game day {pitch.GameDay}:"
                            + $" {current.StartTime:HH:mm}–{current.EndTime:HH:mm}"
                            + $" ({current.Game.Home.Name} vs {current.Game.Away.Name})"
                            + $" overlaps {next.StartTime:HH:mm}–{next.EndTime:HH:mm}"
                            + $" ({next.Game.Home.Name} vs {next.Game.Away.Name}).",
                        ValidationSeverity.Error,
                        Code: "SLOT_OVERLAP",
                        GameDay: pitch.GameDay,
                        PitchName: pitch.Name);
                }
            }
        }
    }

    private static IEnumerable<ValidationMessage> CheckFixtureCoverage(
        IReadOnlyList<Pitch> pitches,
        IReadOnlyList<(string HomeId, string AwayId, string GroupId, int GameDay)> expectedFixtures)
    {
        var scheduled = pitches
            .SelectMany(p => p.Slots.Select(s => (
                HomeId: s.Game.Home.Name,
                AwayId: s.Game.Away.Name,
                GroupId: s.Game.Group.Name,
                GameDay: s.Game.GameDay)))
            .ToHashSet();

        foreach (var fixture in expectedFixtures)
        {
            if (!scheduled.Contains(fixture))
            {
                yield return new ValidationMessage(
                    $"Fixture {fixture.HomeId} vs {fixture.AwayId}"
                        + $" (group '{fixture.GroupId}') on game day {fixture.GameDay}"
                        + " is not present in the scheduled gameplan.",
                    ValidationSeverity.Warning,
                    Code: "FIXTURE_NOT_SCHEDULED",
                    GameDay: fixture.GameDay);
            }
        }
    }
}
