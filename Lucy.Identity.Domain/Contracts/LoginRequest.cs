namespace Lucy.Identity.Domain.Contracts;

public sealed record LoginRequest(string Email, string Password);
