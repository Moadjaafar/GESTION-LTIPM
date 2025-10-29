using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GESTION_LTIPN.Models.Stock
{
    [Table("SuiveemareeRsw")]
    public class SuiveemareeRsw
    {
        [Key]
        public int idsvrsw { get; set; }

        public int? numerobon { get; set; }

        [StringLength(50)]
        public string? MatriculeCamion { get; set; }

        [StringLength(50)]
        public string? Num_Citerne { get; set; }

        public decimal? Poids { get; set; }

        public decimal? TemperaturePort { get; set; }

        public decimal? TemperatureCiterne { get; set; }

        [StringLength(100)]
        public string? Bateau { get; set; }

        [StringLength(10)]
        public string? maree { get; set; }

        [StringLength(100)]
        public string? Cuve { get; set; }

        [StringLength(500)]
        public string? Espece { get; set; }

        [StringLength(500)]
        public string? Observation { get; set; }

        public DateTime? DateSortiePort { get; set; }

        public TimeSpan? HeureSortiePort { get; set; }

        public DateTime? DateArrivage { get; set; }

        public TimeSpan? HeureArrivage { get; set; }

        [StringLength(100)]
        public string? ResponsableArrivage { get; set; }

        public DateTime? DateDebutDecharge { get; set; }

        public TimeSpan? HeureDebutDecharge { get; set; }

        [StringLength(100)]
        public string? ResponsableDebutDecha { get; set; }

        [StringLength(100)]
        public string? SalleDecharge { get; set; }

        [StringLength(100)]
        public string? BassinDecharge { get; set; }

        public DateTime? DateFinDecharge { get; set; }

        public TimeSpan? HeureFinDecharge { get; set; }

        [StringLength(100)]
        public string? ResponsableFinDecha { get; set; }

        public DateTime? DateDebutTraitement { get; set; }

        public TimeSpan? HeureDebutTraitement { get; set; }

        [StringLength(100)]
        public string? ResponsableDebutTrait { get; set; }

        public DateTime? dateProductionDebutTraitement { get; set; }

        public DateTime? Date_Fin_Traitement { get; set; }

        public TimeSpan? Heure_Fin_Traitement { get; set; }

        [StringLength(100)]
        public string? Responsable_Fin_Traitement { get; set; }

        public bool Suppression { get; set; } = false;

        [StringLength(100)]
        public string? deletePar { get; set; }

        // Navigation property
        public virtual ICollection<AnalyseRSW>? Analyses { get; set; }
    }
}
