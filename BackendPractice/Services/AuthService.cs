using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BackendPractice.Data;
using BackendPractice.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BackendPractice.Services;

public sealed class AuthService(
    CourseDbContext dbContext,
    IPasswordHasher<User> passwordHasher,
    IOptions<JwtOptions> jwtOptions) : IAuthService
{
    private readonly JwtOptions _jwt = jwtOptions.Value;

    public async Task<RegistrationResult> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var email = request.Email!.Trim().ToLowerInvariant();
        if (await dbContext.Users.AnyAsync(user => user.Email == email, cancellationToken))
        {
            return new RegistrationResult(null, true);
        }

        var now = DateTime.UtcNow;
        var user = new User
        {
            Name = request.Name!.Trim(),
            Email = email,
            PasswordHash = string.Empty,
            Role = UserRole.Customer,
            CreatedAt = now,
            UpdatedAt = now
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password!);
        dbContext.Users.Add(user);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            return new RegistrationResult(null, true);
        }

        return new RegistrationResult(ToSafeUser(user), false);
    }

    public async Task<LoginResult?> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var email = request.Email!.Trim().ToLowerInvariant();
        var user = await dbContext.Users.SingleOrDefaultAsync(
            candidate => candidate.Email == email,
            cancellationToken);
        if (user is null ||
            passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password!) ==
            PasswordVerificationResult.Failed)
        {
            return null;
        }

        var expiresAt = DateTime.UtcNow.AddMinutes(_jwt.AccessTokenMinutes);
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("role", user.Role.ToString())
            ],
            expires: expiresAt,
            signingCredentials: credentials);

        return new LoginResult(
            new JwtSecurityTokenHandler().WriteToken(token),
            "Bearer",
            expiresAt,
            ToSafeUser(user));
    }

    private static SafeUser ToSafeUser(User user) =>
        new(user.Id, user.Name, user.Email, user.Role);

    private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };
}
