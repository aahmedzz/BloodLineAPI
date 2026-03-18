using Microsoft.AspNetCore.Identity;

namespace BloodLineAPI.Domain.Entities.Users
{
    public class Role : IdentityRole<Guid>
    {
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}
