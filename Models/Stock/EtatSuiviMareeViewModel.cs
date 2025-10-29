using System.ComponentModel.DataAnnotations;

namespace GESTION_LTIPN.Models.Stock
{
    public class EtatSuiviMareeViewModel
    {
        // Filter Properties
        [Display(Name = "Première Date (Début de Traitement)")]
        [DataType(DataType.Date)]
        public DateTime? DateDebut { get; set; }

        [Display(Name = "Deuxième Date (Début de Traitement)")]
        [DataType(DataType.Date)]
        public DateTime? DateFin { get; set; }

        [Display(Name = "Année")]
        public string? Annee { get; set; }

        [Display(Name = "Filtrer par Bateau")]
        public string? BateauFilter { get; set; }

        [Display(Name = "Filtrer par Espèce")]
        public string? EspeceFilter { get; set; }

        [Display(Name = "Numéro de Bon")]
        public string? NumeroBon { get; set; }

        [Display(Name = "Matricule Camion")]
        public string? MatriculeCamionFilter { get; set; }

        // Data Lists
        public List<MareeItemViewModel> Marees { get; set; } = new List<MareeItemViewModel>();
        public List<string> Bateaux { get; set; } = new List<string>();
        public List<string> Especes { get; set; } = new List<string>();
        public List<string> MatriculesCamion { get; set; } = new List<string>();
        public List<string> Annees { get; set; } = new List<string> { "2023", "2024", "2025" };

        // Title for display
        public string? FilteredTitle { get; set; }
    }

    public class MareeItemViewModel
    {
        public int ID { get; set; }
        public string? NumeroBon { get; set; }
        public string? MatriculeCamion { get; set; }
        public string? NumeroCiterne { get; set; }
        public decimal? Poids { get; set; }
        public decimal? TemperaturePort { get; set; }
        public decimal? TemperatureCiterne { get; set; }
        public string? Bateau { get; set; }
        public string? Maree { get; set; }
        public string? Cuve { get; set; }
        public string? Espece { get; set; }
        public string? Observation { get; set; }
        public string? DateSortiePort { get; set; }
        public string? HeureSortiePort { get; set; }
        public string? DateArrivee { get; set; }
        public string? HeureArrivee { get; set; }
        public string? ResponsableArrivage { get; set; }
        public string? DateDebutDecharge { get; set; }
        public string? HeureDebutDecharge { get; set; }
        public string? ResponsableDebutDecharge { get; set; }
        public string? Salle { get; set; }
        public string? Bassin { get; set; }
        public string? DateFinDecharge { get; set; }
        public string? HeureFinDecharge { get; set; }
        public string? ResponsableFinDecharge { get; set; }
        public string? DateDebutTraitement { get; set; }
        public string? HeureDebutTraitement { get; set; }
        public string? ResponsableDebutTraitement { get; set; }
        public string? DateDebutTraitementProd { get; set; }
        public string? DateFinTraitement { get; set; }
        public string? HeureFinTraitement { get; set; }
        public string? ResponsableFinTraitement { get; set; }
        public string? TempsAttenteCiterne { get; set; }
        public string? TempsAttenteBassin { get; set; }
        public string? TempsAttenteTraitement { get; set; }
    }

    public class UpdateMareeViewModel
    {
        public int IdSVRSW { get; set; }
        public string? NumeroBon { get; set; }
        public string? Bateau { get; set; }
        public string? Maree { get; set; }
        public string? Cuve { get; set; }
        public string? Espece { get; set; }
        public string? MatriculeCamion { get; set; }
        public string? MatriculeCiterne { get; set; }
        public decimal? Poids { get; set; }
        public string? Observation { get; set; }
        public decimal? TemperaturePort { get; set; }
        public decimal? TemperatureCiterne { get; set; }
        public string? Salle { get; set; }
        public string? Bassin { get; set; }
        public DateTime? DateSortiePort { get; set; }
        public TimeSpan? HeureSortiePort { get; set; }
        public DateTime? DateArrivee { get; set; }
        public TimeSpan? HeureArrivee { get; set; }
        public DateTime? DateDebutDecharge { get; set; }
        public TimeSpan? HeureDebutDecharge { get; set; }
        public DateTime? DateFinDecharge { get; set; }
        public TimeSpan? HeureFinDecharge { get; set; }
        public DateTime? DateDebutTraitement { get; set; }
        public TimeSpan? HeureDebutTraitement { get; set; }
        public DateTime? DateFinTraitement { get; set; }
        public TimeSpan? HeureFinTraitement { get; set; }

        // Dropdowns
        public List<string>? Bateaux { get; set; }
        public List<string>? Especes { get; set; }
        public List<string>? Salles { get; set; }
        public List<string>? Bassins { get; set; }
    }

    public class AnalyseDetailViewModel
    {
        public string? DateDemande { get; set; }
        public string? HeureDemande { get; set; }
        public string? ResponsableDemande { get; set; }
        public string? Espece { get; set; }
        public string? Histamine { get; set; }
        public string? ABVT { get; set; }
        public string? IF { get; set; }
        public string? Observation { get; set; }
        public string? Emplacement { get; set; }
        public string? DateAnalyse { get; set; }
        public string? HeureAnalyse { get; set; }
        public string? ResponsableAnalyse { get; set; }
    }
}
