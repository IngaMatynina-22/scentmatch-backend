using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ScentMatch.Domain.Entities;
using ScentMatch.Infrastructure.Data.Seed;

namespace ScentMatch.Infrastructure.Data;

public class ScentMatchDbContext
    : IdentityDbContext<User, Role, int, IdentityUserClaim<int>, UserRole, IdentityUserLogin<int>, IdentityRoleClaim<int>, IdentityUserToken<int>>
{
    public ScentMatchDbContext(DbContextOptions<ScentMatchDbContext> options)
        : base(options)
    {
    }

    public DbSet<UserPreferences> UserPreferences => Set<UserPreferences>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<FragranceType> FragranceTypes => Set<FragranceType>();
    public DbSet<Season> Seasons => Set<Season>();
    public DbSet<Note> Notes => Set<Note>();
    public DbSet<Perfume> Perfumes => Set<Perfume>();
    public DbSet<PerfumeNote> PerfumeNotes => Set<PerfumeNote>();
    public DbSet<PerfumeSeason> PerfumeSeasons => Set<PerfumeSeason>();
    public DbSet<PerfumeShopLink> PerfumeShopLinks => Set<PerfumeShopLink>();
    public DbSet<FavoritePerfume> FavoritePerfumes => Set<FavoritePerfume>();
    public DbSet<Collection> Collections => Set<Collection>();
    public DbSet<CollectionPerfume> CollectionPerfumes => Set<CollectionPerfume>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<ViewHistory> ViewHistory => Set<ViewHistory>();
    public DbSet<SelectionHistory> SelectionHistory => Set<SelectionHistory>();
    public DbSet<RecommendationHistory> RecommendationHistory => Set<RecommendationHistory>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<ScentJournalEntry> ScentJournalEntries => Set<ScentJournalEntry>();
    public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();
    public DbSet<PriceWatch> PriceWatches => Set<PriceWatch>();
    public DbSet<PriceSnapshot> PriceSnapshots => Set<PriceSnapshot>();
    public DbSet<BlindComparisonSession> BlindComparisonSessions => Set<BlindComparisonSession>();
    public DbSet<BlindComparisonSlot> BlindComparisonSlots => Set<BlindComparisonSlot>();
    public DbSet<AdminAuditLog> AdminAuditLogs => Set<AdminAuditLog>();
    public DbSet<BackgroundJobLog> BackgroundJobLogs => Set<BackgroundJobLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>().ToTable("users");
        modelBuilder.Entity<Role>().ToTable("roles");
        modelBuilder.Entity<UserRole>().ToTable("user_roles");
        modelBuilder.Entity<IdentityUserClaim<int>>().ToTable("user_claims");
        modelBuilder.Entity<IdentityUserLogin<int>>().ToTable("user_logins");
        modelBuilder.Entity<IdentityUserToken<int>>().ToTable("user_tokens");
        modelBuilder.Entity<IdentityRoleClaim<int>>().ToTable("role_claims");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ScentMatchDbContext).Assembly);
        ReferenceDataSeed.Seed(modelBuilder);
    }
}
