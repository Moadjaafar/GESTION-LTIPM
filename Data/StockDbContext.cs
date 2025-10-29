using GESTION_LTIPN.Models.Stock;
using Microsoft.EntityFrameworkCore;

namespace GESTION_LTIPN.Data
{
    public class StockDbContext : DbContext
    {
        public StockDbContext(DbContextOptions<StockDbContext> options)
            : base(options)
        {
        }

        public DbSet<DepartCamion> DepartsCamions { get; set; }
        public DbSet<ReceptionCamion> ReceptionsCamions { get; set; }
        public DbSet<CamionStock> Camions { get; set; }
        public DbSet<PaletteTransfert> PaletteTransferts { get; set; }
        public DbSet<LogistiqueInfoStock> LogistiqueInfoStock { get; set; }
        public DbSet<SuiveemareeRsw> SuiveemareeRsw { get; set; }
        public DbSet<AnalyseRSW> AnalyseRSW { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure DepartCamion entity
            modelBuilder.Entity<DepartCamion>(entity =>
            {
                entity.ToTable("DepartsCamions");
                entity.HasKey(e => e.IdDepart);

                entity.HasOne(e => e.Camion)
                      .WithMany(c => c.Departs)
                      .HasForeignKey(e => e.IdCamion)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Reception)
                      .WithOne(r => r.Depart)
                      .HasForeignKey<ReceptionCamion>(r => r.IdDepart)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure ReceptionCamion entity
            modelBuilder.Entity<ReceptionCamion>(entity =>
            {
                entity.ToTable("ReceptionsCamions");
                entity.HasKey(e => e.IdReception);
                entity.Property(e => e.PriceBooking).HasColumnType("decimal(18, 2)");
            });

            // Configure CamionStock entity
            modelBuilder.Entity<CamionStock>(entity =>
            {
                entity.ToTable("Camions");
                entity.HasKey(e => e.IdCamion);
                entity.Property(e => e.NumeroCamion).IsRequired().HasMaxLength(50);
            });

            // Configure PaletteTransfert entity
            modelBuilder.Entity<PaletteTransfert>(entity =>
            {
                entity.ToTable("PaletteTransferts");
                entity.HasKey(e => e.IdPaletteTransfert);

                entity.HasOne(e => e.Depart)
                      .WithMany(d => d.PaletteTransferts)
                      .HasForeignKey(e => e.IdDepart)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure LogistiqueInfoStock entity
            modelBuilder.Entity<LogistiqueInfoStock>(entity =>
            {
                entity.ToTable("LogistiqueInfoStock");
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.Depart)
                      .WithMany()
                      .HasForeignKey(e => e.IdDepart)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.DatetimeSaisie)
                      .HasDefaultValueSql("GETDATE()");
            });

            // Configure SuiveemareeRsw entity
            modelBuilder.Entity<SuiveemareeRsw>(entity =>
            {
                entity.ToTable("SuiveemareeRsw");
                entity.HasKey(e => e.idsvrsw);
                entity.Property(e => e.Suppression).HasDefaultValue(false);
            });

            // Configure AnalyseRSW entity
            modelBuilder.Entity<AnalyseRSW>(entity =>
            {
                entity.ToTable("AnalyseRSW");
                entity.HasKey(e => e.idAnalyse);

                entity.HasOne(e => e.SuiveeMaree)
                      .WithMany(s => s.Analyses)
                      .HasForeignKey(e => e.idsvrsw)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
