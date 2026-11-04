using Microsoft.EntityFrameworkCore;
using PoolerBotCS.Models;

namespace PoolerBotCS;

public class PoolerDbContext : DbContext 
{


    public PoolerDbContext(DbContextOptions<PoolerDbContext> options) : base(options)
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
    }


    public DbSet<BanchoLobby> BanchoLobbies { get; set; }
    public DbSet<BanchoPlayer> BanchoPlayers { get; set; }
    public DbSet<PoolerMap> PoolerMaps { get; set; }
    public DbSet<Verification> Verifications { get; set; }

    
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Each PoolerMap belongs to exactly one lobby's pool, so the same
        // beatmap can carry different mods in different lobbies.
        modelBuilder.Entity<BanchoLobby>()
            .HasMany(l => l.MapPool)
            .WithOne()
            .HasForeignKey("BanchoLobbyId")
            .IsRequired();

        modelBuilder.Entity<BanchoLobby>()
            .HasOne(l => l.CurrentMap)
            .WithMany()
            .HasForeignKey("CurrentMapId")
            .IsRequired(false);
    }
}
