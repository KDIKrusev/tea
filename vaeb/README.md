
# Introduction 

The basic functionality of the Voyage Energy Advisor Service summarizes as follows:

1. Receive VoyageEnergyCalculatorRequest containing:
   - Max/min speed
   - Max/min departure and arrival times (ETD/ETA)
   - Route waypoints
2. Suggest various voyage options satisfying the requested ETD, ETA and speed conditions
3. Add time and speed information to the suggested voyage options
4. Divide each voyage option into smaller **route segments**, and for all these route segments:
 	1. Add time, speed and vessel heading information 
	6. Use Weather Data Service to get weather forecast 
	7. Add *apparent weather* to all route segments (based on true weather, vessel speed and vessel heading)
	8. Estimate *calm water power* for all route segments based on vessel calm water speed/power curve (configuration)
	9. Estimate *wind resistance power* based on apparent wind and vessel specific wind configuration 
	10. Estimate *current resistance power* based on apparent current and vessel specific current configuration 
	11. Estimate  *wave resistance power* for all route segments based on apparent wave data and vessel specific wave configuration 
	12. Estimate  *sail contribution power* for all route segments based on apparent wind data and vessel specific sail configuration
12. Summarize data
13. Return power and energy estimates in VoyageEnergyCalculatorResponse model.

# Conventions for wind, current and wave directions

We use the meteorological convention, that is the direction from where where wind, wave or current is comming.

**True weather**
- **0°**: Wind/wave/current comming from north
- **90°**: Wind/wave/current comming from east
- **180°**: Wind/wave/current comming from south
- **270°**: Wind/wave/current moving from west

**Apparent weather**
Apparent weather defines the weather as experienced onboard a moving vessel. These values include components from both the true weather and the movement of the vessel.
- **0°**: Wind/wave/current moving from bow to stern
- **90°**: Wind/wave/current moving from starboard to port
- **180°**: Wind/wave/current moving from stern to bow
- **270°**: Wind/wave/current moving from port to starboard

# Units
All models (both internally and externally) uses the SI default units, with the following exceptions:
- Power given as kW
- Energy given as kWh
- Force given as kN

# Stormglass weather provider

API credentials са managed през secure configuration (dotnet user-secrets в dev, Azure Pipeline variable group в CI/prod).

Config key path: `StormglassWeatherProviderConfig:ApiKey`

Dev setup:
```bash
cd VoyageEnergyAdvisor.App
dotnet user-secrets set "StormglassWeatherProviderConfig:ApiKey" "<your-key>"
```

**Never commit credentials to source.**
