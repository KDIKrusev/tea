namespace VoyageEnergyAdvisorService.Test
{
    using VoyageEnergyAdvisor.Core.Services.WeatherService.Helpers.WaveResistanceHelper;
    using Xunit;

    public class VoyageEnergyAdvisorWaveResistanceHelperTest
    {
        [Fact]
        public void StaWave1CanGetPowerWhenConditionsOk()
        {
            var context = new WaveResistanceCalculationContext(new StaWave1(new StaWave1Config(20, 50, 15)));
            var res = context.PerformCalculation(10, 1.5, 50);
            Assert.Equal(326.10779806904634, res);
        }

        [Fact]
        public void StaWave1CanNotGetPowerWhenWavesTooHigh()
        {
            var context = new WaveResistanceCalculationContext(new StaWave1(new StaWave1Config(20, 50, 15)));
            var res = context.PerformCalculation(10, 2, 30);
            Assert.Null(res);
        }

        [Fact]
        public void StaWave1CanNotGetPowerWhenInvalidWaveDirection()
        {
            var context = new WaveResistanceCalculationContext(new StaWave1(new StaWave1Config(20, 50, 15)));
            var res = context.PerformCalculation(10, 2, 30);
            Assert.Null(res);
        }
    }
}
