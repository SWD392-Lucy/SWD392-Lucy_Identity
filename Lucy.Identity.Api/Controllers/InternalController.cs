using Lucy.Identity.Api.Authentication;
using Lucy.Identity.Domain.Contracts;
using Lucy.Identity.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lucy.Identity.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/internal")]
public sealed class InternalController : ControllerBase
{
    private readonly IdentityService identityService;
    private readonly JwtTokenService tokenService;

    public InternalController(IdentityService identityService, JwtTokenService tokenService)
    {
        this.identityService = identityService;
        this.tokenService = tokenService;
    }

    [HttpGet("users/{id:guid}/public-profile")]
    [ProducesResponseType<PublicProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPublicProfile(Guid id, CancellationToken cancellationToken)
    {
        var profile = await identityService.GetPublicProfileAsync(id, cancellationToken);
        return profile is null
            ? NotFound(new ErrorResponse("user_not_found", "User does not exist."))
            : Ok(profile);
    }

    [HttpGet("users/{id:guid}/room-identity")]
    [ProducesResponseType<RoomIdentityResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRoomIdentity(Guid id, CancellationToken cancellationToken)
    {
        var roomIdentity = await identityService.GetRoomIdentityAsync(id, cancellationToken);
        return roomIdentity is null
            ? NotFound(new ErrorResponse("user_not_found", "User does not exist."))
            : Ok(roomIdentity);
    }

    [AllowAnonymous]
    [HttpPost("tokens/validate")]
    [ProducesResponseType<TokenValidationResponse>(StatusCodes.Status200OK)]
    public IActionResult ValidateToken(TokenValidationRequest request)
    {
        var validation = tokenService.ValidateForService(request.AccessToken);
        return Ok(new TokenValidationResponse(
            validation.IsValid,
            validation.UserId,
            validation.Role,
            validation.IsAnonymous,
            validation.ExpiresAt));
    }
}
