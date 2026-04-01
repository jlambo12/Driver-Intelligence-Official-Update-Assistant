using DriverGuardian.Domain.Entities;

namespace DriverGuardian.Contracts.Models;

public sealed record NormalizedDriverRecord(InstalledDriverSnapshot Snapshot, IReadOnlyCollection<string> NormalizationNotes);
