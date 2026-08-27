namespace VoyageEnergyAdvisor.Data
{
    using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore;
    using VoyageEnergyAdvisor.Data.Entities;

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Vessel> Vessels { get; set; } = null!;
        public DbSet<Route> Routes { get; set; } = null!;
        public DbSet<VesselRoute> VesselRoutes { get; set; } = null!;
        public DbSet<Configuration> Configurations { get; set; } = null!;
        public DbSet<UserVessel> UserVessels { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // User to Vessel: Many-to-Many
            builder.Entity<UserVessel>()
                .HasKey(uv => new { uv.UserId, uv.VesselId });

            builder.Entity<UserVessel>()
                .HasOne(uv => uv.User)
                .WithMany(u => u.UserVessels)
                .HasForeignKey(uv => uv.UserId);

            builder.Entity<UserVessel>()
                .HasOne(uv => uv.Vessel)
                .WithMany(v => v.UserVessels)
                .HasForeignKey(uv => uv.VesselId);

            // Vessel to Routes: Many-to-Many
            builder.Entity<VesselRoute>()
                .HasKey(vr => new { vr.VesselId, vr.RouteId });

            builder.Entity<VesselRoute>()
                .HasOne(vr => vr.Vessel)
                .WithMany(v => v.VesselRoutes)
                .HasForeignKey(vr => vr.VesselId);

            builder.Entity<VesselRoute>()
                .HasOne(vr => vr.Route)
                .WithMany(r => r.VesselRoutes)
                .HasForeignKey(vr => vr.RouteId);

            // Vessel to Configurations: One-to-Many
            builder.Entity<Configuration>()
                .HasOne(c => c.Vessel)
                .WithMany(v => v.Configurations)
                .HasForeignKey(c => c.VesselId)
                .OnDelete(DeleteBehavior.Cascade);

            // Ensure RouteXml is stored as XML in the database
            builder.Entity<Route>()
                .Property(r => r.RouteXml)
                .HasColumnType("xml");

        }
    }
}
