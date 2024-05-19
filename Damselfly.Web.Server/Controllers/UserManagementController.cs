using Damselfly.Core.DbModels.Authentication;
using Damselfly.Core.DbModels.Models.APIModels;
using Damselfly.Core.ScopedServices.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Damselfly.Web.Server.Controllers;

//[Authorize(Policy = PolicyDefinitions.s_IsLoggedIn)]
[ApiController]
[Route("/api/users")]
public class UserManagementController : ControllerBase
{
    private readonly ILogger<UserManagementController> _logger;
    private readonly IUserMgmtService _service;

    public UserManagementController(IUserMgmtService service, ILogger<UserManagementController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("/api/users/roles")]
    public async Task<ICollection<ApplicationRole>> GetAllRoles()
    {
        return await _service.GetRoles();
    }

    [HttpGet("/api/users")]
    public async Task<ICollection<AppIdentityUser>> GetAllUsers()
    {
        return await _service.GetUsers();
    }

    [HttpGet("/api/user/{userId}")]
    public async Task<AppIdentityUser> GetUser(int userId)
    {
        return await _service.GetUser(userId);
    }

    [HttpPut("/api/users")]
    public async Task<UserResponse> CreateUser(UserRequest request)
    {
        return await _service.CreateNewUser(request.UserName, request.Email, request.Password, request.Roles);
    }

    [HttpPost("/api/users")]
    public async Task<UserResponse> UpdateUser(UserRequest request)
    {
        if (request.Roles != null && request.Roles.Any())
            return await _service.UpdateUserAsync(request.UserName, request.Email, request.Roles);

        return await _service.SetUserPasswordAsync(request.UserName, request.Password);
    }
}
