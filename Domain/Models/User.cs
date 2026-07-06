public class User
{
    private User() { } // EF Core нужен пустой конструктор
    public Guid Id { get; private set; }
    public string Login { get; private set; } = null!;
    public byte[] PasswordHash { get; private set; } = null!;
    public UserRole Role { get; private set; }
    public ICollection<Booking> Bookings { get; private set; } = null!;

    public static User Create(Guid id, string login, byte[] passwordHash, UserRole role)
    {
        return new User()
        {
            Id = id,
            Login = login,
            PasswordHash = passwordHash,
            Role = role
        };
    }
}


public enum UserRole
{
    User,
    Admin
}