export interface Weather {
    windSpeed: number;
    windDirection: number;
    waveHeight: number;
    wavePeakPeriod: number;
    waveDirection: number;
    currentSpeed: number;
    currentDirection: number;

    airTemperature: number;      
    airPressure: number;     
    relativeHumidity: number;
    cloudCoverage: number;     
    favorableWeatherIndex: number;
    avgNetWeatherResistancePower: number;
    avgTotalResistanceFuelConsumption: number;
}
