namespace VoyageEnergyAdvisor.Data.Entities
{
    using System.ComponentModel.DataAnnotations.Schema;

    public class UserVessel
    {
        public string UserId { get; set; } = null!;
        public int VesselId { get; set; }

        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; } = null!;

        [ForeignKey("VesselId")]
        public Vessel Vessel { get; set; } = null!;
    }
}
