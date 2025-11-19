using System.ComponentModel.DataAnnotations;

namespace GESTION_LTIPN.Models
{
    public class EtatSuiviVoyageViewModel
    {
        // Filter Properties
        [Display(Name = "Date Début")]
        [DataType(DataType.Date)]
        public DateTime? DateDebut { get; set; }

        [Display(Name = "Date Fin")]
        [DataType(DataType.Date)]
        public DateTime? DateFin { get; set; }

        [Display(Name = "Numéro BK")]
        public string? NumeroBK { get; set; }

        [Display(Name = "Numéro TC")]
        public string? NumeroTC { get; set; }

        [Display(Name = "Matricule Camion")]
        public int? CamionId { get; set; }

        [Display(Name = "Société")]
        public int? SocietyId { get; set; }

        [Display(Name = "Statut Voyage")]
        public string? VoyageStatus { get; set; }

        [Display(Name = "Type Voyage")]
        public string? TypeVoyage { get; set; }

        [Display(Name = "Ville Départ")]
        public string? DepartureCity { get; set; }

        // Data Lists
        public List<VoyageItemViewModel> Voyages { get; set; } = new List<VoyageItemViewModel>();
        public List<Society> Societies { get; set; } = new List<Society>();
        public List<Camion> Camions { get; set; } = new List<Camion>();
        public List<string> VoyageStatuses { get; set; } = new List<string> { "Planned", "InProgress", "Completed", "Cancelled" };
        public List<string> TypeVoyages { get; set; } = new List<string> { "Congelé", "DRY" };
        public List<string> DepartureCities { get; set; } = new List<string> { "Agadir", "Casablanca" };

        // Title for display
        public string? FilteredTitle { get; set; }
    }

    public class VoyageItemViewModel
    {
        // Voyage Core Info
        public int VoyageId { get; set; }
        public int VoyageNumber { get; set; }
        public string? Numero_TC { get; set; }
        public string? VoyageStatus { get; set; }

        // Booking Info
        public int BookingId { get; set; }
        public string? BookingReference { get; set; }
        public string? Numero_BK { get; set; }
        public string? TypeVoyage { get; set; }

        // Societies
        public string? SocietyPrincipale { get; set; }
        public string? SocietySecondaire { get; set; }

        // Departure Type
        public string? DepartureType { get; set; }
        public string? Type_Emballage { get; set; }

        // Phase 1: Initial Departure
        public string? DepartureCity { get; set; }
        public string? DepartureDate { get; set; }
        public string? DepartureTime { get; set; }
        public string? CamionFirstMatricule { get; set; }
        public string? CamionFirstDriver { get; set; }

        // Phase 2: Reception in Dakhla
        public string? ReceptionDate { get; set; }
        public string? ReceptionTime { get; set; }

        // Phase 3: Return Departure
        public string? ReturnDepartureDate { get; set; }
        public string? ReturnDepartureTime { get; set; }
        public string? CamionSecondMatricule { get; set; }
        public string? CamionSecondDriver { get; set; }

        // Phase 4: Return Arrival
        public string? ReturnArrivalCity { get; set; }
        public string? ReturnArrivalDate { get; set; }
        public string? ReturnArrivalTime { get; set; }

        // Pricing
        public decimal? PricePrincipale { get; set; }
        public decimal? PriceSecondaire { get; set; }
        public string? Currency { get; set; }

        // Calculated Durations
        public string? DureeAllerDakhla { get; set; }      // Departure to Reception
        public string? DureeSejourDakhla { get; set; }     // Reception to Return Departure
        public string? DureeRetour { get; set; }            // Return Departure to Return Arrival
        public string? DureeTotale { get; set; }            // Total voyage duration

        // Timestamps
        public string? CreatedAt { get; set; }
    }
}
