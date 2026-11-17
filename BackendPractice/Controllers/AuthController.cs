using BackendPractice.Models;
using BackendPractice.Services;
using Microsoft.AspNetCore.Mvc;

namespace BackendPractice.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<SafeUser>> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.RegisterAsync(request, cancellationToken);
        if (result.EmailAlreadyExists)
        {
            return Conflict(new ProblemDetails { Title = "An account with this email already exists." });
        }

        return StatusCode(StatusCodes.Status201Created, result.User);
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResult>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, cancellationToken);
        return result is null
            ? Unauthorized(new ProblemDetails { Title = "Email or password is incorrect." })
            : Ok(result);
    }
}
