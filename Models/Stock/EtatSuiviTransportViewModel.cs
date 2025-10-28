using System.ComponentModel.DataAnnotations;

namespace GESTION_LTIPN.Models.Stock
{
    public class EtatSuiviTransportViewModel
    {
        // Filter Properties
        [Display(Name = "Date Début")]
        [DataType(DataType.Date)]
        public DateTime? DateDebut { get; set; }

        [Display(Name = "Date Fin")]
        [DataType(DataType.Date)]
        public DateTime? DateFin { get; set; }

        [Display(Name = "Camion")]
        public int? CamionId { get; set; }

        // Statistics
        public int TotalDeparts { get; set; }
        public int EnTransit { get; set; }
        public int Livres { get; set; }
        public int TotalPalettes { get; set; }

        // Data
        public List<TransportItemViewModel> Transports { get; set; } = new List<TransportItemViewModel>();
        public List<CamionStock> Camions { get; set; } = new List<CamionStock>();
    }

    public class TransportItemViewModel
    {
        public int IdDepart { get; set; }
        public string? NumeroBon { get; set; }
        public string? NumeroCamion { get; set; }
        public string? Chauffeur { get; set; }
        public string? TypeCamion { get; set; }
        public DateTime? DateDepart { get; set; }
        public string? HeureDepart { get; set; }
        public DateTime? DateReception { get; set; }
        public string? HeureReception { get; set; }
        public int? NombrePalettes { get; set; }
        public int? NombrePalettesRecues { get; set; }
        public string? Destination { get; set; }
        public DateTime? DateHeureArrivee { get; set; }
        public DateTime? DateHeureFin { get; set; }
        public string? DureeTransfert { get; set; }
        public string? EtatTransfert { get; set; }
        public string? Observations { get; set; }
        public decimal? PriceBooking { get; set; }
        public int? IdReception { get; set; }
    }

    public class PaletteDetailViewModel
    {
        public int IdDepart { get; set; }
        public int IdPaletteTransfert { get; set; }
        public string? Article { get; set; }
        public string? Designation { get; set; }
        public string? NumeroPalette { get; set; }
        public string? CodeQr_A_D { get; set; }
        public string? NomBateau { get; set; }
        public string? Marie { get; set; }
        public decimal? PoidsPalette { get; set; }
        public decimal? Cout { get; set; }
        public decimal? Total { get; set; }
        public string? StatutPalette { get; set; }
        public DateTime? DateAssignation { get; set; }
    }

    public class UpdateTransportViewModel
    {
        public int IdDepart { get; set; }
        public int? IdReception { get; set; }

        [Display(Name = "Numéro Bon")]
        public string? NumeroBon { get; set; }

        [Display(Name = "Date Départ")]
        [DataType(DataType.Date)]
        public DateTime? DateDepart { get; set; }

        [Display(Name = "Heure Départ")]
        [DataType(DataType.Time)]
        public TimeSpan? HeureDepart { get; set; }

        [Display(Name = "Date Réception")]
        [DataType(DataType.Date)]
        public DateTime? DateReception { get; set; }

        [Display(Name = "Heure Réception")]
        [DataType(DataType.Time)]
        public TimeSpan? HeureReception { get; set; }
    }
}
