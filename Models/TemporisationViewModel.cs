using System.ComponentModel.DataAnnotations;

namespace GESTION_LTIPN.Models
{
    /// <summary>
    /// ViewModel for Admin/Validator to temporise a booking
    /// </summary>
    public class TemporiseBookingViewModel
    {
        public int BookingId { get; set; }

        [Display(Name = "Référence de réservation")]
        public string? BookingReference { get; set; }

        [Display(Name = "Numéro BK")]
        public string? Numero_BK { get; set; }

        [Display(Name = "Société")]
        public string? SocietyName { get; set; }

        [Display(Name = "Type de voyage")]
        public string? TypeVoyage { get; set; }

        [Display(Name = "Créé par")]
        public string? CreatedByUserName { get; set; }

        [Required(ErrorMessage = "La raison de temporisation est requise")]
        [StringLength(1000, ErrorMessage = "La raison ne peut pas dépasser 1000 caractères")]
        [Display(Name = "Raison de la temporisation")]
        public string ReasonTemporisation { get; set; } = string.Empty;

        [Required(ErrorMessage = "La date estimée de validation est requise")]
        [DataType(DataType.Date)]
        [Display(Name = "Date estimée de validation")]
        public DateTime EstimatedValidationDate { get; set; }
    }

    /// <summary>
    /// ViewModel for booking creator to respond to temporisation
    /// </summary>
    public class RespondToTemporisationViewModel
    {
        public int TemporisationId { get; set; }
        public int BookingId { get; set; }

        [Display(Name = "Référence de réservation")]
        public string? BookingReference { get; set; }

        [Display(Name = "Numéro BK")]
        public string? Numero_BK { get; set; }

        [Display(Name = "Société")]
        public string? SocietyName { get; set; }

        [Display(Name = "Type de voyage")]
        public string? TypeVoyage { get; set; }

        [Display(Name = "Temporisé par")]
        public string? TemporisedByUserName { get; set; }

        [Display(Name = "Date de temporisation")]
        public DateTime TemporisedAt { get; set; }

        [Display(Name = "Raison de la temporisation")]
        public string? ReasonTemporisation { get; set; }

        [Display(Name = "Date estimée de validation")]
        public DateTime EstimatedValidationDate { get; set; }

        [Required(ErrorMessage = "Votre réponse est requise")]
        [Display(Name = "Votre décision")]
        public string CreatorResponse { get; set; } = string.Empty; // 'Accepted' or 'Refused'

        [StringLength(500, ErrorMessage = "Les notes ne peuvent pas dépasser 500 caractères")]
        [Display(Name = "Notes (optionnel)")]
        public string? CreatorResponseNotes { get; set; }
    }

    /// <summary>
    /// ViewModel for displaying temporisation details
    /// </summary>
    public class TemporisationDetailsViewModel
    {
        // Booking Info
        public int BookingId { get; set; }
        public string? BookingReference { get; set; }
        public string? Numero_BK { get; set; }
        public string? SocietyName { get; set; }
        public string? TypeVoyage { get; set; }
        public int Nbr_LTC { get; set; }
        public string? BookingStatus { get; set; }

        // Temporisation Info
        public int TemporisationId { get; set; }
        public string? TemporisedByUserName { get; set; }
        public string? TemporisedByFullName { get; set; }
        public DateTime TemporisedAt { get; set; }
        public string? ReasonTemporisation { get; set; }
        public DateTime EstimatedValidationDate { get; set; }

        // Creator Response
        public string? CreatorResponse { get; set; }
        public DateTime? CreatorRespondedAt { get; set; }
        public string? CreatorResponseNotes { get; set; }

        // Creator Info
        public string? CreatedByUserName { get; set; }
        public string? CreatedByFullName { get; set; }

        // Computed Properties
        public bool IsPending => CreatorResponse == "Pending";
        public bool IsAccepted => CreatorResponse == "Accepted";
        public bool IsRefused => CreatorResponse == "Refused";
        public int DaysUntilEstimatedValidation => (EstimatedValidationDate.Date - DateTime.Today).Days;
        public bool IsOverdue => IsAccepted && EstimatedValidationDate < DateTime.Today;
    }

    /// <summary>
    /// ViewModel for pending temporisations list (Creator dashboard)
    /// </summary>
    public class PendingTemporisationsViewModel
    {
        public List<PendingTemporisationItem> PendingTemporisations { get; set; } = new List<PendingTemporisationItem>();
        public int TotalPending { get; set; }
    }

    public class PendingTemporisationItem
    {
        public int TemporisationId { get; set; }
        public int BookingId { get; set; }
        public string? BookingReference { get; set; }
        public string? Numero_BK { get; set; }
        public string? SocietyName { get; set; }
        public string? TypeVoyage { get; set; }
        public string? TemporisedByFullName { get; set; }
        public DateTime TemporisedAt { get; set; }
        public string? ReasonTemporisation { get; set; }
        public DateTime EstimatedValidationDate { get; set; }
        public int DaysUntilEstimatedValidation { get; set; }
    }
}
