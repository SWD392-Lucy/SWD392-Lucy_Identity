namespace Lucy.Identity.Api.Authentication;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "lucy.identity";
    public string Audience { get; set; } = "lucy.clients";
    public string SigningKey { get; set; } = "CHANGE_ME_TO_A_32_BYTE_SECRET_FOR_LOCAL_DEV";
    public int AccessTokenMinutes { get; set; } = 60;
    public int RefreshTokenDays { get; set; } = 30;
}
