using System.ComponentModel.DataAnnotations;

namespace BallastLane.API.Settings;

public sealed class CorsSettings
{
    public const string SectionName = "Cors";

    [Required, MinLength(1)]
    public string[] AllowedOrigins { get; init; } = [];
}
