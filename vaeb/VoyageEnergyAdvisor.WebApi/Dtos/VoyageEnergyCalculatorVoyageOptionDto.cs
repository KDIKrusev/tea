namespace VoyageEnergyAdvisor.WebApi.Dtos
{

    public class VoyageEnergyAdvisorVoyageOptionDto
    {
        public long Etd { get; set; }
        public long Eta { get; set; }
        public bool IsValid { get; set; }
        public double? AverageSpeed { get; set; }   // Todo Shall not be nullable
        public double? DurationInSeconds { get; set; } // Todo Shall not be nullable
        public List<VoyageEnergyAdvisorVoyageOptionRouteSegmentDto> RouteSegments { get; set; } = new();

        // Energy
        public double? TotalEnergyConsumption { get; set; }
        public double? TotalCalmWaterResistanceEnergyConsumption { get; set; }
        public double? TotalWindEnergyConsumption { get; set; }
        public double? TotalWaveEnergyConsumption { get; set; }
        public double? TotalCurrentEnergyConsumption { get; set; }
        public double? TotalSailEnergyConsumption { get; set; }
        public double? RelativeWindEnergyConsumption { get; set; }
        public double? RelativeWaveEnergyConsumption { get; set; }
        public double? RelativeCurrentEnergyConsumption { get; set; }
        public double? RelativeSailEnergyConsumption { get; set; }

        public double? AveragePower { get; set; }   // Todo Shall not be nullable
        public double? EnergyConsumptionRelative { get; set; } // Todo Shall not be nullable

        // Fuel
        public double? TotalResistanceFuelConsumption { get; set; }
        public double? TotalCalmWaterResistanceFuelConsumption { get; set; }
        public double? TotalWindFuelConsumption { get; set; }
        public double? TotalWaveFuelConsumption { get; set; }
        public double? TotalCurrentFuelConsumption { get; set; }
        public double? TotalSailFuelConsumption { get; set; }
        public double? RelativeWindFuelConsumption { get; set; }
        public double? RelativeWaveFuelConsumption { get; set; }
        public double? RelativeCurrentFuelConsumption { get; set; }
        public double? RelativeSailFuelConsumption { get; set; }
        public double? AverageFuelConsumptionRate { get; set; }
        public double? FuelConsumptionRelative { get; set; }

        // Cost
        public double? TotalResistanceCost { get; set; }
        public double? TotalCalmWaterResistanceCost { get; set; }
        public double? TotalWindCost { get; set; }
        public double? TotalWaveCost { get; set; }
        public double? TotalCurrentCost { get; set; }
        public double? TotalSailCost { get; set; }
        public double? AbsTotalWindCost { get; set; }
        public double? AbsTotalWaveCost { get; set; }
        public double? AbsTotalCurrentCost { get; set; }
        public double? AbsTotalSailCost { get; set; }
        public double? RelativeWindCost { get; set; }
        public double? RelativeWaveCost { get; set; }
        public double? RelativeCurrentCost { get; set; }
        public double? RelativeSailCost { get; set; }
        public double? AverageCostRate { get; set; }
        public double? CostRelative { get; set; }


    }
}
