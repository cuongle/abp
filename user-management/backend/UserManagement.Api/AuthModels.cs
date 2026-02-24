namespace UserManagement.Api;

public record LoginRequest(string UserName, string Password);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public record AuthResponse(string Token, string UserName, string Role);
public record UserProfile(string UserName, string Role, DateTime CreatedAt);

public sealed class JwtOptions
{
    public string Issuer { get; set; } = "UserManagement.Api";
    public string Audience { get; set; } = "UserManagement.Client";
    public string Key { get; set; } = "change-this-key-please-32-chars-min";
    public int ExpiresMinutes { get; set; } = 120;
}

public sealed class AzureAdOptions
{
    public string TenantId { get; set; } = "common";
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}

public sealed class UserEntity
{
    public int Id { get; init; }
    public string UserName { get; init; } = string.Empty;
    public string PasswordHash { get; init; } = string.Empty;
    public string PasswordSalt { get; init; } = string.Empty;
    public string Role { get; init; } = "User";
    public DateTime CreatedAt { get; init; }
}
