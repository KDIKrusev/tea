namespace VoyageEnergyAdvisor.Data.Entities
{
    using System.ComponentModel.DataAnnotations;

    public class Vessel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = null!;

        public string VesselNumber { get; set; } = null!;

        public List<VesselRoute> VesselRoutes { get; set; } = new List<VesselRoute>();
        public List<Configuration> Configurations { get; set; } = new List<Configuration>();

        public List<UserVessel> UserVessels { get; set; } = new List<UserVessel>();
    }
}
