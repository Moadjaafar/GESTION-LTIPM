using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GESTION_LTIPN.Models.Stock
{
    [Table("LogistiqueInfoStock")]
    public class LogistiqueInfoStock
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("idDepart")]
        public int IdDepart { get; set; }

        [Column("DateHeureArrivee")]
        public DateTime? DateHeureArrivee { get; set; }

        [Column("DateHeureFin")]
        public DateTime? DateHeureFin { get; set; }

        [Column("PriceBooking")]
        public double? PriceBooking { get; set; }

        [Column("datetimeSaisie")]
        public DateTime DatetimeSaisie { get; set; } = DateTime.Now;

        // Navigation property
        [ForeignKey("IdDepart")]
        public virtual DepartCamion? Depart { get; set; }
    }
}
