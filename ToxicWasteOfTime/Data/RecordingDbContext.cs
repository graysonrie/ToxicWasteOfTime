using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ToxicWasteOfTime.Models;

namespace ToxicWasteOfTime.Data;

public class RecordingDbContext : DbContext
{
    public DbSet<ControllerRecording> Recordings { get; set; }
    public DbSet<ControllerInputEvent> InputEvents { get; set; }

    public RecordingDbContext(DbContextOptions<RecordingDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ControllerRecording>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired();
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasMany(e => e.Events)
                  .WithOne(e => e.Recording)
                  .HasForeignKey(e => e.RecordingId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ControllerInputEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.RecordingId);
            entity.HasIndex(e => new { e.RecordingId, e.TimestampMs });
        });
    }
}
