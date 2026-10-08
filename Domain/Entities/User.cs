using Microsoft.AspNetCore.Identity;

namespace ScentMatch.Domain.Entities;

public class User : IdentityUser<int>
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public UserPreferences? Preferences { get; set; }
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<FavoritePerfume> FavoritePerfumes { get; set; } = new List<FavoritePerfume>();
    public ICollection<Collection> Collections { get; set; } = new List<Collection>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<ViewHistory> ViewHistory { get; set; } = new List<ViewHistory>();
    public ICollection<SelectionHistory> SelectionHistory { get; set; } = new List<SelectionHistory>();
    public ICollection<RecommendationHistory> RecommendationHistory { get; set; } = new List<RecommendationHistory>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
