using System.ComponentModel.DataAnnotations;

namespace BallastLane.Infrastructure.Settings;

public sealed class DatabaseSettings
{
    public const string SectionName = "ConnectionStrings";

    [Required]
    public string Database { get; init; } = string.Empty;

    [Required]
    public string Provider { get; init; } = "PostgreSQL";
}
