using System.ComponentModel.DataAnnotations;

namespace GESTION_LTIPN.Models
{
    public class SuperEditViewModel
    {
        // Booking Information
        public int BookingId { get; set; }

        [Display(Name = "Référence de réservation")]
        public string BookingReference { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le numéro BK est requis")]
        [Display(Name = "Numéro BK")]
        [StringLength(50, ErrorMessage = "Le numéro BK ne peut pas dépasser 50 caractères")]
        public string Numero_BK { get; set; } = string.Empty;

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

        [Display(Name = "Type Contenaire")]
        [StringLength(10)]
        public string? TypeContenaire { get; set; }

        [Display(Name = "Nom Client")]
        [StringLength(255)]
        public string? NomClient { get; set; }

        [Display(Name = "Notes")]
        public string? Notes { get; set; }

        [Display(Name = "Statut")]
        public string? BookingStatus { get; set; }

        // Voyages List
        public List<VoyageEditItem> Voyages { get; set; } = new List<VoyageEditItem>();

        // For dropdowns
        public List<Society>? Societies { get; set; }
        public List<string>? TypeVoyages { get; set; }
        public List<string>? TypeContenaires { get; set; }
    }

    public class VoyageEditItem
    {
        public int VoyageId { get; set; }

        [Required(ErrorMessage = "Le numéro de voyage est requis")]
        [Display(Name = "Numéro de Voyage")]
        [Range(1, 1000, ErrorMessage = "Le numéro de voyage doit être entre 1 et 1000")]
        public int VoyageNumber { get; set; }

        [Required(ErrorMessage = "Le numéro TC est requis")]
        [Display(Name = "Numéro TC")]
        [StringLength(100, ErrorMessage = "Le numéro TC ne peut pas dépasser 100 caractères")]
        public string Numero_TC { get; set; } = string.Empty;

        [Display(Name = "Statut")]
        public string? VoyageStatus { get; set; }

        [Display(Name = "Type de départ")]
        [StringLength(50)]
        public string? DepartureType { get; set; }

        [Display(Name = "Ville de départ")]
        [StringLength(100)]
        public string? DepartureCity { get; set; }

        [Display(Name = "Date de départ")]
        public DateTime? DepartureDate { get; set; }

        // For display only
        public bool CanEdit { get; set; }
    }
}
