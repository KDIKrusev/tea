using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;


namespace VoyageEnergyAdvisor.Data.Entities
{
    public class Configuration
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string ConfigName { get; set; } = null!;

        [Required]
        public string ConfigJson { get; set; } = null!;

        [Required]
        public int VesselId { get; set; }

        [ForeignKey("VesselId")]
        public Vessel Vessel { get; set; } = null!;
    }
}
