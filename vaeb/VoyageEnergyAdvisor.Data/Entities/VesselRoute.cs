using System.ComponentModel.DataAnnotations.Schema;

namespace VoyageEnergyAdvisor.Data.Entities
{
    public class VesselRoute
    {
        public int VesselId { get; set; }
        public int RouteId { get; set; }

        [ForeignKey("VesselId")]
        public Vessel Vessel { get; set; } = null!;

        [ForeignKey("RouteId")]
        public Route Route { get; set; } = null!;
    }
}
