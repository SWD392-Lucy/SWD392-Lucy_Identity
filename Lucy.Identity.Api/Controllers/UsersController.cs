using Lucy.Identity.Domain.Contracts;
using Lucy.Identity.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lucy.Identity.Api.Controllers;

[ApiController]
[Authorize(Policy = "SuperOnly")]
[Route("api/users")]
public sealed class UsersController : ControllerBase
{
    private readonly IdentityService identityService;

    public UsersController(IdentityService identityService)
    {
        this.identityService = identityService;
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<UserProfileResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListUsers(CancellationToken cancellationToken)
    {
        var users = await identityService.ListUsersAsync(cancellationToken);
        return Ok(users);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<UserProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUser(Guid id, CancellationToken cancellationToken)
    {
        var user = await identityService.GetProfileAsync(id, cancellationToken);
        return user is null
            ? NotFound(new ErrorResponse("user_not_found", "User does not exist."))
            : Ok(user);
    }

    [HttpPut("{id:guid}/role")]
    [ProducesResponseType<UserProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRole(Guid id, UpdateUserRoleRequest request, CancellationToken cancellationToken)
    {
        var result = await identityService.UpdateUserRoleAsync(id, request, cancellationToken);
        return ToResult(result);
    }

    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType<UserProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(Guid id, UpdateUserStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await identityService.UpdateUserStatusAsync(id, request, cancellationToken);
        return ToResult(result);
    }

    private IActionResult ToResult(IdentityResult<UserProfileResponse> result)
    {
        if (result.Succeeded)
        {
            return Ok(result.Value);
        }

        var error = new ErrorResponse(result.ErrorCode!, result.ErrorMessage!);
        return result.ErrorCode == "user_not_found" ? NotFound(error) : BadRequest(error);
    }
}
