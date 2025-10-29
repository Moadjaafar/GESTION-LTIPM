using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GESTION_LTIPN.Models.Stock
{
    [Table("AnalyseRSW")]
    public class AnalyseRSW
    {
        [Key]
        public int idAnalyse { get; set; }

        public int idsvrsw { get; set; }

        public DateTime? DateDemande { get; set; }

        public TimeSpan? HeureDemande { get; set; }

        [StringLength(100)]
        public string? ResponDemande { get; set; }

        [StringLength(200)]
        public string? Espece { get; set; }

        public decimal? ValHistamine { get; set; }

        public decimal? ValAbvt { get; set; }

        [StringLength(50)]
        public string? ValIF { get; set; }

        [StringLength(500)]
        public string? Observation { get; set; }

        [StringLength(100)]
        public string? Emplacement { get; set; }

        public DateTime? DateAnnalyse { get; set; }

        public TimeSpan? HeureAnnalyse { get; set; }

        [StringLength(100)]
        public string? ResponAnnalyse { get; set; }

        // Navigation property
        [ForeignKey("idsvrsw")]
        public virtual SuiveemareeRsw? SuiveeMaree { get; set; }
    }
}
