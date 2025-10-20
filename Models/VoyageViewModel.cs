using System.ComponentModel.DataAnnotations;

namespace GESTION_LTIPN.Models
{
    public class VoyageViewModel
    {
        public int VoyageId { get; set; }

        public int BookingId { get; set; }

        [Display(Name = "Numéro de voyage")]
        public int VoyageNumber { get; set; }

        [Display(Name = "Numéro TC")]
        [StringLength(50)]
        public string? Numero_TC { get; set; }

        [Display(Name = "Société principale")]
        public int SocietyPrincipaleId { get; set; } // Auto-set from booking, no validation needed

        [Display(Name = "Société secondaire")]
        public int? SocietySecondaireId { get; set; }

        [Display(Name = "Société de transport (1er départ)")]
        public int? SocietyTranspFirstId { get; set; }

        [Display(Name = "Camion (1er départ)")]
        public int? CamionFirstDepart { get; set; }

        [Display(Name = "Société de transport (2ème départ)")]
        public int? SocietyTranspSecondId { get; set; }

        [Display(Name = "Camion (2ème départ)")]
        public int? CamionSecondDepart { get; set; }

        [Display(Name = "Ville de départ")]
        public string? DepartureCity { get; set; }

        [Display(Name = "Date de départ")]
        [DataType(DataType.Date)]
        public DateTime? DepartureDate { get; set; } // Optional - set later when voyage actually departs

        [Display(Name = "Heure de départ")]
        [DataType(DataType.Time)]
        public TimeSpan? DepartureTime { get; set; }

        [Required(ErrorMessage = "Le type de départ est requis")]
        [Display(Name = "Type de départ")]
        public string DepartureType { get; set; } = string.Empty;

        // Reception Information (in Dakhla)
        [Display(Name = "Date de réception à Dakhla")]
        [DataType(DataType.Date)]
        public DateTime? ReceptionDate { get; set; }

        [Display(Name = "Heure de réception")]
        [DataType(DataType.Time)]
        public TimeSpan? ReceptionTime { get; set; }

        // Return Information
        [Display(Name = "Date de départ retour")]
        [DataType(DataType.Date)]
        public DateTime? ReturnDepartureDate { get; set; }

        [Display(Name = "Heure de départ retour")]
        [DataType(DataType.Time)]
        public TimeSpan? ReturnDepartureTime { get; set; }

        [Display(Name = "Ville d'arrivée")]
        public string? ReturnArrivalCity { get; set; }

        [Display(Name = "Date d'arrivée")]
        [DataType(DataType.Date)]
        public DateTime? ReturnArrivalDate { get; set; }

        [Display(Name = "Heure d'arrivée")]
        [DataType(DataType.Time)]
        public TimeSpan? ReturnArrivalTime { get; set; }

        // Pricing Information
        [Display(Name = "Prix société principale")]
        [DataType(DataType.Currency)]
        public decimal? PricePrincipale { get; set; }

        [Display(Name = "Prix société secondaire")]
        [DataType(DataType.Currency)]
        public decimal? PriceSecondaire { get; set; }

        [Display(Name = "Devise")]
        public string Currency { get; set; } = "MAD";

        // For dropdowns
        public List<Society>? Societies { get; set; }
        public List<SocietyTransp>? SocietiesTransp { get; set; }
        public List<Camion>? Camions { get; set; }
        public List<string>? DepartureCities { get; set; }
        public List<string>? DepartureTypes { get; set; }

        // For display
        public string? BookingReference { get; set; }
        public string? SocietyPrincipaleName { get; set; }
        public string? SocietySecondaireName { get; set; }
        public string? CamionFirstMatricule { get; set; }
        public string? CamionSecondMatricule { get; set; }
        public string? VoyageStatus { get; set; }
        public bool HasSecondaryPrice { get; set; } // True if voyage has SocietySecondaireId
    }

    public class AssignVoyagesViewModel
    {
        public Booking? Booking { get; set; }
        public List<Voyage>? Voyages { get; set; }
        public int RemainingVoyages { get; set; }
        public bool CanAddVoyage { get; set; }
    }
}
