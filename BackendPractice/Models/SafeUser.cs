namespace BackendPractice.Models;

public sealed record SafeUser(int Id, string Name, string Email, UserRole Role);
