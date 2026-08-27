using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace VoyageEnergyAdvisor.Data.Entities
{
    public class Route
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string RouteName { get; set; } = null!;

        [Required]
        [Column(TypeName = "xml")] 
        public string RouteXml { get; set; } = null!;

        public List<VesselRoute> VesselRoutes { get; set; } = new List<VesselRoute>();
    }
}
