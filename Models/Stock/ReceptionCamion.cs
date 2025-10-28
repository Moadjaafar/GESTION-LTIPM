using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GESTION_LTIPN.Models.Stock
{
    [Table("ReceptionsCamions")]
    public class ReceptionCamion
    {
        [Key]
        public int IdReception { get; set; }

        public int IdDepart { get; set; }

        public DateTime? DateReception { get; set; }

        public TimeSpan? HeureReception { get; set; }

        public int? NombrePalettesRecues { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? PriceBooking { get; set; }

        public DateTime? DateSaisie { get; set; }

        public string? Observations { get; set; }

        // Navigation properties
        [ForeignKey("IdDepart")]
        public virtual DepartCamion? Depart { get; set; }
    }
}
