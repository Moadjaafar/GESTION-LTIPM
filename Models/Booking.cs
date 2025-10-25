using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GESTION_LTIPN.Models
{
    public class Booking
    {
        [Key]
        public int BookingId { get; set; }

        [Required]
        [StringLength(50)]
        public string BookingReference { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Numero_BK { get; set; } = string.Empty;

        [Required]
        public int SocietyId { get; set; }

        [Required]
        [StringLength(100)]
        public string TypeVoyage { get; set; } = string.Empty;

        [Required]
        public int Nbr_LTC { get; set; } // Number of voyages (LTC = Lot de Transport Camion)

        [Required]
        public int CreatedByUserId { get; set; }

        public int? ValidatedByUserId { get; set; }

        [Required]
        [StringLength(50)]
        public string BookingStatus { get; set; } = "Pending"; // 'Pending', 'Validated', 'Completed', 'Cancelled'

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? ValidatedAt { get; set; }

        public string? Notes { get; set; }

        // Navigation properties
        [ForeignKey("SocietyId")]
        public virtual Society? Society { get; set; }

        [ForeignKey("CreatedByUserId")]
        public virtual User? CreatedByUser { get; set; }

        [ForeignKey("ValidatedByUserId")]
        public virtual User? ValidatedByUser { get; set; }

        public virtual ICollection<Voyage> Voyages { get; set; } = new List<Voyage>();
    }
}
