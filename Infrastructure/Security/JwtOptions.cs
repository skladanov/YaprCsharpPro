namespace Infrastructure.Security;

public class JwtOptions
{
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "WebProject";
    public string Audience { get; set; } = "WebProjectClient";
    public int LifetimeHours { get; set; } = 1;

    public TimeSpan Lifetime => TimeSpan.FromHours(LifetimeHours);

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Secret))
            throw new ArgumentException("Jwt:Secret is required.", nameof(Secret));

        if (Secret.Length < 32)
            throw new ArgumentException("Jwt:Secret should be at least 32 characters long.", nameof(Secret));
    }
}