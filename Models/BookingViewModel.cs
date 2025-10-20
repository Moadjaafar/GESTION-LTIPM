using System.ComponentModel.DataAnnotations;

namespace GESTION_LTIPN.Models
{
    public class BookingViewModel
    {
        public int BookingId { get; set; }

        [Display(Name = "Référence de réservation")]
        public string BookingReference { get; set; } = string.Empty;

        [Required(ErrorMessage = "La société est requise")]
        [Display(Name = "Société")]
        public int SocietyId { get; set; }

        [Required(ErrorMessage = "Le type de voyage est requis")]
        [Display(Name = "Type de voyage")]
        public string TypeVoyage { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le nombre de LTC est requis")]
        [Display(Name = "Nombre de LTC")]
        [Range(1, 100, ErrorMessage = "Le nombre de LTC doit être entre 1 et 100")]
        public int Nbr_LTC { get; set; }

        [Display(Name = "Notes")]
        public string? Notes { get; set; }

        [Display(Name = "Statut")]
        public string BookingStatus { get; set; } = "Pending";

        [Display(Name = "Date de création")]
        public DateTime CreatedAt { get; set; }

        [Display(Name = "Créé par")]
        public string? CreatedByUserName { get; set; }

        [Display(Name = "Validé par")]
        public string? ValidatedByUserName { get; set; }

        [Display(Name = "Date de validation")]
        public DateTime? ValidatedAt { get; set; }

        [Display(Name = "Société")]
        public string? SocietyName { get; set; }

        // For dropdowns
        public List<Society>? Societies { get; set; }
        public List<string>? TypeVoyages { get; set; }
    }
}
