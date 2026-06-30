namespace Domain.Models
{
    public class User
    {
        private User() { } // EF Core нужен пустой конструктор
        public Guid Id { get; private set; }
        public string Login { get; private set; } = null!;
        public byte[] PasswordHash { get; private set; } = null!;
        public UserRole Role { get; private set; }
        public ICollection<Booking> Bookings { get; private set; } = null!;
    }

    public enum UserRole
    {
        User,
        Admin
    }
}
