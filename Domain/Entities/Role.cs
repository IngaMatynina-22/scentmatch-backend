using Microsoft.AspNetCore.Identity;

namespace ScentMatch.Domain.Entities;

public class Role : IdentityRole<int>
{
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
