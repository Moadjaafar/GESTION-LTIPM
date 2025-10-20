using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GESTION_LTIPN.Models
{
    public class Voyage
    {
        [Key]
        public int VoyageId { get; set; }

        [Required]
        public int BookingId { get; set; }

        [Required]
        public int VoyageNumber { get; set; } // Sequential number for voyages in a booking

        [StringLength(50)]
        public string? Numero_TC { get; set; } // TC Number for the voyage

        // Society Assignment
        [Required]
        public int SocietyPrincipaleId { get; set; }

        public int? SocietySecondaireId { get; set; } // Only if DepartureType is 'Emballage'

        // Camion Assignment - TWO TRUCKS
        public int? CamionFirstDepart { get; set; } // Truck for initial departure (Agadir/Casablanca -> Dakhla)

        public int? CamionSecondDepart { get; set; } // Truck for return departure (Dakhla -> Agadir/Casablanca)

        // Departure Information
        [StringLength(50)]
        public string? DepartureCity { get; set; } // 'Agadir' or 'Casablanca' - nullable now

        public DateTime? DepartureDate { get; set; } // Set later when voyage actually departs

        public TimeSpan? DepartureTime { get; set; }

        [Required]
        [StringLength(50)]
        public string DepartureType { get; set; } = string.Empty; // 'Emballage' or 'Empty'

        // Reception Information (in Dakhla)
        public DateTime? ReceptionDate { get; set; }

        public TimeSpan? ReceptionTime { get; set; }

        // Return Information
        public DateTime? ReturnDepartureDate { get; set; }

        public TimeSpan? ReturnDepartureTime { get; set; }

        [StringLength(50)]
        public string? ReturnArrivalCity { get; set; } // 'Agadir' or 'Casablanca'

        public DateTime? ReturnArrivalDate { get; set; }

        public TimeSpan? ReturnArrivalTime { get; set; }

        // Validation and Status
        public bool IsValidated { get; set; } = false;

        public int? ValidatedByUserId { get; set; }

        public DateTime? ValidatedAt { get; set; }

        // Prices (assigned after voyage completion)
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? PricePrincipale { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? PriceSecondaire { get; set; } // Only if SocietySecondaireId is not null

        [StringLength(10)]
        public string Currency { get; set; } = "MAD";

        [Required]
        [StringLength(50)]
        public string VoyageStatus { get; set; } = "Planned"; // 'Planned', 'InProgress', 'Completed', 'Cancelled'

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("BookingId")]
        public virtual Booking? Booking { get; set; }

        [ForeignKey("SocietyPrincipaleId")]
        public virtual Society? SocietyPrincipale { get; set; }

        [ForeignKey("SocietySecondaireId")]
        public virtual Society? SocietySecondaire { get; set; }

        [ForeignKey("CamionFirstDepart")]
        public virtual Camion? CamionFirst { get; set; }

        [ForeignKey("CamionSecondDepart")]
        public virtual Camion? CamionSecond { get; set; }

        [ForeignKey("ValidatedByUserId")]
        public virtual User? ValidatedByUser { get; set; }
    }
}
