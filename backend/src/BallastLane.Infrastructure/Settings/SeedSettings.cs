using System.ComponentModel.DataAnnotations;

namespace BallastLane.Infrastructure.Settings;

public sealed class SeedSettings
{
    public const string SectionName = "Seed";

    [Required]
    public string DemoUserEmail { get; init; } = string.Empty;

    [Required]
    public string DemoUserPassword { get; init; } = string.Empty;
}
