using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GESTION_LTIPN.Models.Stock
{
    [Table("DepartsCamions")]
    public class DepartCamion
    {
        [Key]
        public int IdDepart { get; set; }

        public string? NumeroBon { get; set; }

        public int IdCamion { get; set; }

        public DateTime? DateDepart { get; set; }

        public TimeSpan? HeureDepart { get; set; }

        public int? NombrePalettes { get; set; }

        public string? Destination { get; set; }

        // Navigation properties
        [ForeignKey("IdCamion")]
        public virtual CamionStock? Camion { get; set; }

        public virtual ReceptionCamion? Reception { get; set; }

        public virtual ICollection<PaletteTransfert> PaletteTransferts { get; set; } = new List<PaletteTransfert>();
    }
}
