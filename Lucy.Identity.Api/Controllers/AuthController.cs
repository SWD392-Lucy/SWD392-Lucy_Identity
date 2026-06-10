using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Lucy.Identity.Domain.Contracts;
using Lucy.Identity.Domain.Identity;

namespace Lucy.Identity.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IdentityService identityService;

    public AuthController(IdentityService identityService)
    {
        this.identityService = identityService;
    }

    [HttpPost("register")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await identityService.RegisterAsync(request, cancellationToken);
        if (!result.Succeeded)
        {
            var error = new ErrorResponse(result.ErrorCode!, result.ErrorMessage!);
            return result.ErrorCode == "email_exists" ? Conflict(error) : BadRequest(error);
        }

        return CreatedAtAction(nameof(Me), new { }, result.Value);
    }

    [HttpPost("login")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await identityService.LoginAsync(request, cancellationToken);
        if (!result.Succeeded)
        {
            return Unauthorized(new ErrorResponse(result.ErrorCode!, result.ErrorMessage!));
        }

        return Ok(result.Value);
    }

    [HttpPost("refresh")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await identityService.RefreshAsync(request, cancellationToken);
        return result.Succeeded
            ? Ok(result.Value)
            : Unauthorized(new ErrorResponse(result.ErrorCode!, result.ErrorMessage!));
    }

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(LogoutRequest request, CancellationToken cancellationToken)
    {
        await identityService.LogoutAsync(request, cancellationToken);
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType<UserProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(subject, out var userId))
        {
            return Unauthorized();
        }

        var profile = await identityService.GetProfileAsync(userId, cancellationToken);
        return profile is null
            ? NotFound(new ErrorResponse("user_not_found", "User does not exist."))
            : Ok(profile);
    }

    [Authorize]
    [HttpPut("me/profile")]
    [ProducesResponseType<UserProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMyProfile(UpdateMyProfileRequest request, CancellationToken cancellationToken)
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(subject, out var userId))
        {
            return Unauthorized();
        }

        var result = await identityService.UpdateMyProfileAsync(userId, request, cancellationToken);
        if (!result.Succeeded)
        {
            var error = new ErrorResponse(result.ErrorCode!, result.ErrorMessage!);
            return result.ErrorCode == "user_not_found" ? NotFound(error) : BadRequest(error);
        }

        return Ok(result.Value);
    }

    [Authorize(Policy = "MentorAccess")]
    [HttpGet("mentor-area")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult MentorArea()
    {
        return NoContent();
    }
}
