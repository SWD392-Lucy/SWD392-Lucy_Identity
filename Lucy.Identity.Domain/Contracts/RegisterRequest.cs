using Lucy.Identity.Domain.Entities;

namespace Lucy.Identity.Domain.Contracts;

public sealed record RegisterRequest(
    string Email,
    string Password,
    string DisplayName,
    AccountRole Role = AccountRole.Lucy,
    string? AvatarPersona = null,
    string? AnonymousDisplayName = null,
    string? PublicBio = null,
    string? LanguageLevel = null);
