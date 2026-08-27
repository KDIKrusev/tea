namespace VoyageEnergyCalculatorService.Test.Data
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;
    using System.Security.Claims;
    using VoyageEnergyAdvisor.Core.Repositories;
    using VoyageEnergyAdvisor.Data.DataRepositories;
    using VoyageEnergyAdvisor.Data.Entities;
    using VoyageEnergyAdvisor.Data;
    using Xunit;

    public class RouteRepositoryTests
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IRouteRepository _repository;

        public RouteRepositoryTests()
        {
            var serviceProvider = TestHelper.BuildTestServiceProvider();

            _dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();

            // Set vessel claim for HttpContext
            httpContextAccessor.HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                new Claim("VesselId", "1")
            }, "TestAuth"))
            };

            SeedTestData(_dbContext);
            _repository = new RouteRepository(_dbContext, httpContextAccessor);
        }

        [Fact]
        public async Task GetRoutesListAsync_Should_Return_RouteNames()
        {
            var result = await _repository.GetRoutesListAsync();

            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Contains("TestRoute", result);
        }

        [Fact]
        public async Task GetRouteAsync_Should_Return_DeserializedRoute()
        {
            var route = await _repository.GetRouteAsync("TestRoute");

            Assert.NotNull(route);
            Assert.Equal("TestRoute", route.RouteName);
        }

        [Fact]
        public async Task GetRouteAsync_Should_Return_Null_If_Not_Exist()
        {
            var route = await _repository.GetRouteAsync("NonExisting");

            Assert.Null(route);
        }

        private void SeedTestData(ApplicationDbContext db)
        {
            var sampleRtzXml = @"<?xml version='1.0' encoding='UTF-8'?>
            <route xmlns='http://www.cirm.org/RTZ/1/1' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' version='1.1'>
                <routeInfo routeName='TestRoute'/>
                <waypoints>
                    <waypoint id='1'>
                        <position lat='60.12345' lon='24.54321'/>
                        <leg/>
                    </waypoint>
                </waypoints>
            </route>";

           var route = new Route
            {
                Id = 1,
                RouteName = "TestRoute",
                RouteXml = sampleRtzXml
           };

            var vessel = new Vessel
            {
                Id = 1,
                Name = "TestVessel",
                VesselNumber = "123"
            };

            var vesselRoute = new VesselRoute
            {
                VesselId = 1,
                RouteId = 1
            };

            db.Routes.Add(route);
            db.Vessels.Add(vessel);
            db.VesselRoutes.Add(vesselRoute);
            db.SaveChanges();
        }

        
    }
}
