using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GESTION_LTIPN.Models.Stock
{
    [Table("PaletteTransferts")]
    public class PaletteTransfert
    {
        [Key]
        public int IdPaletteTransfert { get; set; }

        public int IdDepart { get; set; }

        [StringLength(100)]
        public string? CodeQr_A_D { get; set; }

        public int? NbrCarton { get; set; }

        [StringLength(50)]
        public string? StatutPalette { get; set; }

        public DateTime? DateAssignation { get; set; }

        // Navigation properties
        [ForeignKey("IdDepart")]
        public virtual DepartCamion? Depart { get; set; }
    }
}
