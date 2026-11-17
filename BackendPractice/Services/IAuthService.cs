using BackendPractice.Models;

namespace BackendPractice.Services;

public interface IAuthService
{
    Task<RegistrationResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
    Task<LoginResult?> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
}
