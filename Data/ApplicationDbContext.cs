using GESTION_LTIPN.Models;
using Microsoft.EntityFrameworkCore;

namespace GESTION_LTIPN.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Society> Societies { get; set; }
        public DbSet<SocietyTransp> SocietiesTransp { get; set; }
        public DbSet<Camion> Camions { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Voyage> Voyages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(e => e.UserId);
                entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Password).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Role).IsRequired().HasMaxLength(50);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.IsActive).HasDefaultValue(true);

                entity.HasIndex(e => e.Username).IsUnique();

                entity.HasOne(e => e.Society)
                      .WithMany(s => s.Users)
                      .HasForeignKey(e => e.SocietyId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure Society entity
            modelBuilder.Entity<Society>(entity =>
            {
                entity.ToTable("Societies");
                entity.HasKey(e => e.SocietyId);
                entity.Property(e => e.SocietyName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.IsActive).HasDefaultValue(true);

                entity.HasIndex(e => e.SocietyName).IsUnique();
            });

            // Configure SocietyTransp entity
            modelBuilder.Entity<SocietyTransp>(entity =>
            {
                entity.ToTable("Societies_Transp");
                entity.HasKey(e => e.SocietyTranspId);
                entity.Property(e => e.SocietyTranspName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.IsActive).HasDefaultValue(true);

                entity.HasIndex(e => e.SocietyTranspName).IsUnique();
            });

            // Configure Camion entity
            modelBuilder.Entity<Camion>(entity =>
            {
                entity.ToTable("Camions");
                entity.HasKey(e => e.CamionId);
                entity.Property(e => e.CamionMatricule).IsRequired().HasMaxLength(50);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.IsActive).HasDefaultValue(true);

                entity.HasIndex(e => e.CamionMatricule).IsUnique();

                entity.HasOne(e => e.SocietyTransp)
                      .WithMany(s => s.Camions)
                      .HasForeignKey(e => e.SocietyTranspId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure Booking entity
            modelBuilder.Entity<Booking>(entity =>
            {
                entity.ToTable("Bookings");
                entity.HasKey(e => e.BookingId);
                entity.Property(e => e.BookingReference).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Numero_BK).IsRequired().HasMaxLength(50);
                entity.Property(e => e.TypeVoyage).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Nbr_LTC).IsRequired();
                entity.Property(e => e.BookingStatus).IsRequired().HasMaxLength(50).HasDefaultValue("Pending");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");

                entity.HasIndex(e => e.BookingReference).IsUnique();

                entity.HasOne(e => e.Society)
                      .WithMany()
                      .HasForeignKey(e => e.SocietyId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.CreatedByUser)
                      .WithMany()
                      .HasForeignKey(e => e.CreatedByUserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.ValidatedByUser)
                      .WithMany()
                      .HasForeignKey(e => e.ValidatedByUserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Voyage entity
            modelBuilder.Entity<Voyage>(entity =>
            {
                entity.ToTable("Voyages");
                entity.HasKey(e => e.VoyageId);
                entity.Property(e => e.Numero_TC).IsRequired().HasMaxLength(50);
                entity.Property(e => e.DepartureCity).HasMaxLength(50); // Nullable now
                entity.Property(e => e.DepartureType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Type_Emballage).HasMaxLength(200);
                entity.Property(e => e.CamionMatricule_FirstDepart_Externe).HasMaxLength(50);
                entity.Property(e => e.CamionMatricule_SecondDepart_Externe).HasMaxLength(50);
                entity.Property(e => e.VoyageStatus).IsRequired().HasMaxLength(50).HasDefaultValue("Planned");
                entity.Property(e => e.Currency).HasMaxLength(10).HasDefaultValue("MAD");
                entity.Property(e => e.PricePrincipale).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.PriceSecondaire).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.IsValidated).HasDefaultValue(false);

                entity.HasOne(e => e.Booking)
                      .WithMany(b => b.Voyages)
                      .HasForeignKey(e => e.BookingId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.SocietyPrincipale)
                      .WithMany()
                      .HasForeignKey(e => e.SocietyPrincipaleId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.SocietySecondaire)
                      .WithMany()
                      .HasForeignKey(e => e.SocietySecondaireId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Two truck foreign keys
                entity.HasOne(e => e.CamionFirst)
                      .WithMany()
                      .HasForeignKey(e => e.CamionFirstDepart)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.CamionSecond)
                      .WithMany()
                      .HasForeignKey(e => e.CamionSecondDepart)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.ValidatedByUser)
                      .WithMany()
                      .HasForeignKey(e => e.ValidatedByUserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
