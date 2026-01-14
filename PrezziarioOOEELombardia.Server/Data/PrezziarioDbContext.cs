using Microsoft.EntityFrameworkCore;

namespace PrezziarioOOEELombardia.Server.Data;

public class PrezziarioDbContext : DbContext
{
    public PrezziarioDbContext(DbContextOptions<PrezziarioDbContext> options) : base(options)
    {
    }

    public DbSet<Voce> Voci { get; set; }
    public DbSet<Risorsa> Risorse { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Voce>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CodiceVoce);
            entity.HasIndex(e => e.CodLiv1);
            entity.HasIndex(e => e.CodLiv2);
            entity.HasIndex(e => e.CodLiv3);
            entity.HasIndex(e => e.CodLiv4);
            entity.HasIndex(e => e.CodLiv5);
            
            entity.HasMany(e => e.Risorse)
                .WithOne(r => r.Voce)
                .HasForeignKey(r => r.VoceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Risorsa>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.VoceId);
        });
    }
}
