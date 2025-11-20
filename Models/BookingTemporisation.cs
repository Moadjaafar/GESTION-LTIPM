using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GESTION_LTIPN.Models
{
    public class BookingTemporisation
    {
        [Key]
        public int TemporisationId { get; set; }

        [Required]
        public int BookingId { get; set; }

        // Temporisation Details (Admin/Validator Input)
        [Required]
        public int TemporisedByUserId { get; set; }

        public DateTime TemporisedAt { get; set; } = DateTime.Now;

        [Required]
        [StringLength(1000)]
        [Display(Name = "Raison de temporisation")]
        public string ReasonTemporisation { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Date estimée de validation")]
        public DateTime EstimatedValidationDate { get; set; }

        // Creator Response
        [StringLength(50)]
        [Display(Name = "Réponse du créateur")]
        public string? CreatorResponse { get; set; } // 'Pending', 'Accepted', 'Refused'

        [Display(Name = "Date de réponse")]
        public DateTime? CreatorRespondedAt { get; set; }

        [StringLength(500)]
        [Display(Name = "Notes du créateur")]
        public string? CreatorResponseNotes { get; set; }

        // Status Management
        public bool IsActive { get; set; } = true;

        // Audit Fields
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Navigation Properties
        [ForeignKey("BookingId")]
        public virtual Booking? Booking { get; set; }

        [ForeignKey("TemporisedByUserId")]
        public virtual User? TemporisedByUser { get; set; }

        // Computed Properties
        [NotMapped]
        public bool IsPending => CreatorResponse == "Pending";

        [NotMapped]
        public bool IsAccepted => CreatorResponse == "Accepted";

        [NotMapped]
        public bool IsRefused => CreatorResponse == "Refused";

        [NotMapped]
        public int DaysUntilEstimatedValidation
        {
            get
            {
                var days = (EstimatedValidationDate.Date - DateTime.Today).Days;
                return days;
            }
        }

        [NotMapped]
        public bool IsOverdue => IsAccepted && EstimatedValidationDate < DateTime.Today;
    }
}
