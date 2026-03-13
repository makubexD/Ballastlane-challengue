using BallastLane.API.Extensions;
using BallastLane.API.Services;
using BallastLane.Application.DTOs;
using BallastLane.Application.Services;
using BallastLane.Infrastructure.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace BallastLane.API.Controllers;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting("auth")]
public sealed class AuthController(
    IAuthService authService,
    ICurrentUserService currentUser,
    IOptions<JwtSettings> jwtOptions) : ControllerBase
{
    private readonly JwtSettings _jwtSettings = jwtOptions.Value;

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.RegisterAsync(request, cancellationToken);
        return result.ToActionResult(user => StatusCode(StatusCodes.Status201Created, new { id = user.Id, email = user.Email }));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, cancellationToken);
        if (!result.IsSuccess) return result.ToActionResult(_ => Ok());

        Response.Cookies.Append("access_token", result.Value!.Token, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = Request.IsHttps ? SameSiteMode.Strict : SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes)
        });
        return Ok(new { result.Value!.UserId, result.Value!.ExpiresAt });
    }

    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("access_token");
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var result = await authService.GetCurrentUserAsync(currentUser.UserId, cancellationToken);
        return result.ToActionResult(Ok);
    }
}
