using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Spydersoft.NotificationApi.Infrastructure.Data.Entities;

namespace Spydersoft.NotificationApi.Infrastructure.Data;

public sealed class NotificationDbContext(DbContextOptions<NotificationDbContext> options) : DbContext(options)
{
    public DbSet<NotificationEntity> Notifications => Set<NotificationEntity>();
    public DbSet<NotificationDeliveryEntity> NotificationDeliveries => Set<NotificationDeliveryEntity>();
    public DbSet<DeviceEntity> Devices => Set<DeviceEntity>();
    public DbSet<NotificationPreferenceEntity> NotificationPreferences => Set<NotificationPreferenceEntity>();
    public DbSet<NotificationTypePreferenceEntity> NotificationTypePreferences => Set<NotificationTypePreferenceEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NotificationEntity>(entity =>
        {
            entity.ToTable("notifications");

            entity.Property(n => n.Data)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => v == null ? null : JsonSerializer.Serialize(v, JsonSerializerOptions.Web),
                    v => v == null ? null : JsonSerializer.Deserialize<Dictionary<string, string>>(v, JsonSerializerOptions.Web))
                .Metadata.SetValueComparer(new ValueComparer<Dictionary<string, string>?>(
                    (a, b) => (a == null && b == null) || (a != null && b != null && a.SequenceEqual(b)),
                    v => v == null ? 0 : v.Aggregate(0, (hash, kvp) => HashCode.Combine(hash, kvp.Key, kvp.Value)),
                    v => v == null ? null : new Dictionary<string, string>(v)));

            entity.HasIndex(n => new { n.UserId, n.CreatedAt });
            entity.HasIndex(n => new { n.UserId, n.IsRead }).HasFilter("\"IsRead\" = false");
            entity.HasIndex(n => new { n.UserId, n.Source, n.EntityType, n.EntityId }).HasFilter("\"EntityType\" IS NOT NULL");

            entity.HasMany(n => n.Deliveries)
                .WithOne(d => d.Notification)
                .HasForeignKey(d => d.NotificationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<NotificationDeliveryEntity>(entity =>
        {
            entity.ToTable("notification_deliveries");
            entity.HasIndex(d => d.NotificationId);
        });

        modelBuilder.Entity<DeviceEntity>(entity =>
        {
            entity.ToTable("devices");
            entity.HasIndex(d => d.UserId).HasFilter("\"IsActive\" = true");
        });

        modelBuilder.Entity<NotificationPreferenceEntity>(entity =>
        {
            entity.ToTable("notification_preferences");
            entity.HasKey(p => p.UserId);
        });

        modelBuilder.Entity<NotificationTypePreferenceEntity>(entity =>
        {
            entity.ToTable("notification_type_preferences");
            entity.HasIndex(p => new { p.UserId, p.Source, p.Type }).IsUnique();
        });
    }
}
