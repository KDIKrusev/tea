using VoyageEnergyAdvisor.Core.Models.VoyageEnergyAdvisor;

namespace VoyageEnergyAdvisor.Core.Services.VoyageEnergyAdvisorService.Models
{
    public class VoyageEnergyAdvisorVoyageOption
    {
        public DateTime Etd { get; set; }
        public DateTime Eta { get; set; }
        public bool IsValid { get; set; }
        public double AverageSpeed { get; set; }
        public double DurationInSeconds { get; set; }
        public IList<VoyageEnergyAdvisorVoyageOptionRouteSegment> RouteSegments { get; set; } = new List<VoyageEnergyAdvisorVoyageOptionRouteSegment>();

        // Energy
        public double? AbsTotalWindEnergy { get; set; }
        public double? RelativeWindEnergyConsumption { get; set; }

        public double? AbsTotalWaveEnergy { get; set; }
        public double? RelativeWaveEnergyConsumption { get; set; }

        public double? AbsTotalCurrentEnergy { get; set; }
        public double? RelativeCurrentEnergyConsumption { get; set; }

        public double? AbsTotalSailEnergy { get; set; }
        public double? RelativeSailEnergyConsumption { get; set; }

        public double? TotalCalmWaterResistanceEnergyConsumption { get; set; }
        public double? TotalResistanceEnergyConsumption { get; set; }
        public double? AverageResistancePower { get; set; }
        public double? EnergyConsumptionRelative { get; set; }  // Todo Shall not be nullable???+

        // Fuel Consumption
        public double? AbsTotalWindFuelConsumption { get; set; }
        public double? RelativeWindFuelConsumption { get; set; }
        public double? AbsTotalWaveFuelConsumption { get; set; }
        public double? RelativeWaveFuelConsumption { get; set; }
        public double? AbsTotalCurrentFuelConsumption { get; set; }
        public double? RelativeCurrentFuelConsumption { get; set; }
        public double? AbsTotalSailFuelConsumption { get; set; }
        public double? RelativeSailFuelConsumption { get; set; }

        public double? TotalCalmWaterResistanceFuelConsumption { get; set; }
        public double? TotalFuelConsumption { get; set; }
        public double? AverageFuelConsumptionRate { get; set; } 
        public double? FuelConsumptionRelative { get; set; }

        // Cost
        public double? AbsTotalWindCost { get; set; }
        public double? AbsTotalWaveCost { get; set; }
        public double? AbsTotalCurrentCost { get; set; }
        public double? AbsTotalSailCost { get; set; }

        public double? RelativeWindCost { get; set; }
        public double? RelativeWaveCost { get; set; }
        public double? RelativeCurrentCost { get; set; }
        public double? RelativeSailCost { get; set; }

        // Cost - Total values
        public double? TotalCalmWaterResistanceCost { get; set; }
        public double? TotalResistanceCost { get; set; }

        // Cost - Average rate (cost per hour)
        public double? AverageCostRate { get; set; }

        public double? CostRelative { get; set; }

        public bool IsLiveMode { get; set; }
    }
}
