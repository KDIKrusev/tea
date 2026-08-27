using VoyageEnergyAdvisor.Core.Models.VoyageEnergyAdvisor;
using VoyageEnergyAdvisor.Core.Services.VoyageEnergyAdvisorService.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VoyageEnergyAdvisor.Core.CommonModels;

namespace VoyageEnergyAdvisor.Core.Services.VoyageEnergyAdvisorService
{
    public interface IVoyageEnergyAdvisorVoyageOptionsBuilder
    {
        Task<IEnumerable<VoyageEnergyAdvisorVoyageOption>> PrepareVoyageOptions(VoyageEnergyAdvisorRequest request);

        Task<IEnumerable<VoyageEnergyAdvisorVoyageOption>> PopulateVoyageOptions(
            IEnumerable<VoyageEnergyAdvisorVoyageOption> validOptions, Route route);

        VoyageEnergyAdvisorRequest? ToValidRequest(VoyageEnergyAdvisorRequest request);

        IEnumerable<VoyageEnergyAdvisorVoyageOption> GetVoyageOptionsArray(VoyageEnergyAdvisorRequest request);

        IEnumerable<VoyageEnergyAdvisorVoyageOption> FilterOnSpeed(
            IEnumerable<VoyageEnergyAdvisorVoyageOption> voyageOptions, double minSpeed, double maxSpeed);

        IEnumerable<VoyageEnergyAdvisorVoyageOption> AddRouteSegments(
            IEnumerable<VoyageEnergyAdvisorVoyageOption> voyageOptions, Route route);

        IEnumerable<VoyageEnergyAdvisorVoyageOption> AddTimeToRouteSegments(
            IEnumerable<VoyageEnergyAdvisorVoyageOption> voyageOptions);

        IEnumerable<VoyageEnergyAdvisorVoyageOption> AddCourseToRouteSegments(
            IEnumerable<VoyageEnergyAdvisorVoyageOption> voyageOptions);

        Task<IEnumerable<VoyageEnergyAdvisorVoyageOption>> AddTrueWeatherToRouteSegments(
            IEnumerable<VoyageEnergyAdvisorVoyageOption> voyageOptions);

        IEnumerable<VoyageEnergyAdvisorVoyageOption> AddApparentWeatherToRouteSegments(
            IEnumerable<VoyageEnergyAdvisorVoyageOption> voyageOptions);

        IEnumerable<VoyageEnergyAdvisorVoyageOption> AddCalmWaterPowerToRouteSegments(
            IEnumerable<VoyageEnergyAdvisorVoyageOption> voyageOptions);

        IEnumerable<VoyageEnergyAdvisorVoyageOption> AddWindPowerToRouteSegments(
            IEnumerable<VoyageEnergyAdvisorVoyageOption> voyageOptions);

        IEnumerable<VoyageEnergyAdvisorVoyageOption> AddCurrentPowerToRouteSegments(
            IEnumerable<VoyageEnergyAdvisorVoyageOption> voyageOptions);

        IEnumerable<VoyageEnergyAdvisorVoyageOption> AddSailPowerToRouteSegments(
            IEnumerable<VoyageEnergyAdvisorVoyageOption> voyageOptions);

        IEnumerable<VoyageEnergyAdvisorVoyageOption> AddWavePowerToRouteSegments(
            IEnumerable<VoyageEnergyAdvisorVoyageOption> voyageOptions);

        IEnumerable<VoyageEnergyAdvisorVoyageOption> AddTotalPowerToRouteSegments(
            IEnumerable<VoyageEnergyAdvisorVoyageOption> voyageOptions);

        IEnumerable<VoyageEnergyAdvisorVoyageOption> AddTotalPowerAndEnergyToVoyageOptions(
            IEnumerable<VoyageEnergyAdvisorVoyageOption> voyageOptions);

        IEnumerable<VoyageEnergyAdvisorVoyageOption> AddFavorableWeatherIndexToVoyageOptions(
            IEnumerable<VoyageEnergyAdvisorVoyageOption> voyageOptions);

        string BuildValidationMessage(VoyageEnergyAdvisorRequest request);

        double CalculateRequiredAverageSpeed(double voyageDistance, DateTime etd, DateTime eta);

        double CalculateSegmentPowerBalance(
            VoyageEnergyAdvisorVoyageOptionRouteSegment segment, double constantPropulsionPower, double candidateSpeed);

        double SolveSegmentSpeedForConstantPower(
            VoyageEnergyAdvisorVoyageOptionRouteSegment segment, double constantPropulsionPower, double speedMin, double speedMax);

        Task<VoyageEnergyAdvisorVoyageOption> BuildOptimalVoyageOption(
            VoyageEnergyAdvisorOptimalVoyageRequest request, double requiredAverageSpeed);
    }
}
