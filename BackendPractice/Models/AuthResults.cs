namespace BackendPractice.Models;

public sealed record LoginResult(
    string AccessToken,
    string TokenType,
    DateTime ExpiresAt,
    SafeUser User);

public sealed record RegistrationResult(SafeUser? User, bool EmailAlreadyExists);
