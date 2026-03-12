using System.ComponentModel.DataAnnotations;

namespace BallastLane.Infrastructure.Settings;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    [Required, MinLength(32)]
    public string Secret { get; init; } = string.Empty;

    [Range(1, 1440)]
    public int ExpiryMinutes { get; init; } = 60;
}
