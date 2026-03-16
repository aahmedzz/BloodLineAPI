namespace BloodLineAPI.Domain.Entities.Users
{
    public class User : AuditableEntity
    {
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public Donor? Donor { get; set; }
        public Staff? Staff { get; set; }
    }
}
