namespace Lucy.Identity.Domain.Identity;

public sealed record IdentityResult<T>(T? Value, string? ErrorCode, string? ErrorMessage)
{
    public bool Succeeded => ErrorCode is null;

    public static IdentityResult<T> Success(T value) => new(value, null, null);

    public static IdentityResult<T> Failure(string code, string message) => new(default, code, message);
}
