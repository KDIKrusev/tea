namespace VoyageEnergyAdvisor.Data.Entities
{
    using Microsoft.AspNetCore.Identity;

    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = null!;

        public List<UserVessel> UserVessels { get; set; } = new List<UserVessel>();
    }
}
