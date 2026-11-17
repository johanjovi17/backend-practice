using System.ComponentModel.DataAnnotations;

namespace BackendPractice.Models;

public sealed class RegisterRequest
{
    [Required, StringLength(100)]
    public string? Name { get; init; }

    [Required, EmailAddress, StringLength(256)]
    public string? Email { get; init; }

    [Required, MinLength(12), StringLength(128)]
    public string? Password { get; init; }
}

public sealed class LoginRequest
{
    [Required, EmailAddress, StringLength(256)]
    public string? Email { get; init; }

    [Required, StringLength(128)]
    public string? Password { get; init; }
}
