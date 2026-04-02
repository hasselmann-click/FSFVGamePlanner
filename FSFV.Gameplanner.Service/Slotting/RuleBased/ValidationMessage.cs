using System.Collections.Generic;

namespace FSFV.Gameplanner.Service.Slotting.RuleBased;

public enum ValidationSeverity { Info, Warning, Error }

/// <summary>
/// A single diagnostic message produced by a rule's <see cref="ISlotRule.Validate"/> call.
/// </summary>
public record ValidationMessage(
    string Message,
    ValidationSeverity Severity,
    string? Code = null,
    int? GameDay = null,
    string? PitchName = null);
