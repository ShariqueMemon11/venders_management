using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Application.Common.Interfaces;
using Shared.Application.Identity;

namespace Vendors.Api.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize(Policy = "Perm.User.Manage")]
public class UsersController : ControllerBase
{
    private readonly IUserManagementService _users;
    private readonly ICurrentUserService _currentUser;

    public UsersController(IUserManagementService users, ICurrentUserService currentUser)
    {
        _users = users;
        _currentUser = currentUser;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var result = await _users.CreateAsync(request, _currentUser.TenantId, cancellationToken);
        if (!result.Success) return BadRequest(result);
        return Created($"/api/v1/users/{result.Data}", result);
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await _users.ListAsync(_currentUser.TenantId, cancellationToken);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var result = await _users.SetActiveAsync(id, isActive: false, _currentUser.TenantId, _currentUser.UserId, cancellationToken);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        var result = await _users.SetActiveAsync(id, isActive: true, _currentUser.TenantId, _currentUser.UserId, cancellationToken);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/terminate")]
    public async Task<IActionResult> Terminate(Guid id, CancellationToken cancellationToken)
    {
        var result = await _users.TerminateAsync(id, _currentUser.TenantId, _currentUser.UserId, cancellationToken);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }
}
