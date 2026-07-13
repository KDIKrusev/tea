# K-Sail Calculator - Backend API

.NET 8 Web API backend for the K-Sail iEMS Savings Calculator. This API extracts all calculation logic from the Angular frontend, providing RESTful endpoints for vessel efficiency calculations.

## Features

- **RESTful API**: Clean HTTP endpoints for calculations and validation
- **Exact Port**: All calculation logic ported directly from TypeScript to C#
- **Validation**: Input validation and system capacity checks
- **Swagger/OpenAPI**: Built-in API documentation
- **CORS Support**: Configured for Angular frontend communication
- **Docker Ready**: Includes Dockerfile for containerization

## Technology Stack

- .NET 8.0
- ASP.NET Core Web API
- Swagger/OpenAPI for documentation
- JSON serialization with camelCase naming

## Project Structure

```
KSailCalc.Api/
├── Controllers/
│   └── CalculatorController.cs    # API endpoints
├── Models/
│   ├── CalculatorInput.cs         # Input DTO
│   ├── CalculationResult.cs       # Result DTOs
│   ├── PowerDemands.cs
│   ├── FuelBreakdown.cs
│   └── ValidationWarning.cs
├── Services/
│   ├── CalculatorService.cs       # Core calculation logic
│   ├── ValidationService.cs       # Input validation
│   └── Interfaces/
├── Program.cs                      # App configuration
└── KSailCalc.Api.csproj
```

## Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- (Optional) Docker for containerized deployment

### Running Locally

1. Navigate to the project directory:
```bash
cd KSailCalc.Api
```

2. Restore dependencies:
```bash
dotnet restore
```

3. Run the application:
```bash
dotnet run
```

The API will start on http://localhost:5000

### Accessing Swagger UI

Once running, navigate to:
- Swagger UI: http://localhost:5000/swagger
- API Docs: http://localhost:5000/swagger/v1/swagger.json

## API Endpoints

### Calculate iEMS Savings

**POST** /api/calculator/calculate

Calculate fuel savings, emissions reduction, and ROI for iEMS installation.

**Request Body:**
```json
{
  "iemsVariant": "Advanced",
  "propulsionPower": 20000,
  "hotelLoad": 3000,
  "seaMargin": 5,
  "meCapacityPerEngine": 20000,
  "meCount": 2,
  "sgCapacityPerEngine": 2500,
  "aeCapacityPerEngine": 1200,
  "aeCount": 3,
  "sailInstalled": false,
  "batteryCapacity": 0,
  "fuelPrice": 800,
  "annualHours": 8000
}
```

**Response:**
```json
{
  "baselineFOC": 6051.44,
  "optimizedFOC": 4239.03,
  "fuelSavings": 1812.41,
  "fuelSavingsPercentage": 29.95,
  "baselineCO2": 19402.82,
  "optimizedCO2": 13586.34,
  "co2Reduction": 5816.48,
  "annualCostSavings": 1449928.0,
  "totalInvestment": 110000.0,
  "paybackPeriod": 0.076,
  "roi": 1218.1,
  "npv": 11087765.2,
  "efficiencyFactor": 0.97,
  "breakdown": {
    "baselineME": 5820.22,
    "baselineAE": 231.22,
    "optimizedME": 5645.61,
    "optimizedAE": 224.28
  },
  "powerDemands": {
    "mainEnginePowerKw": 23500,
    "shaftGeneratorPowerKw": 2500,
    "auxiliaryEnginePowerKw": 500,
    "totalPowerKw": 26500,
    "totalEnergyKwh": 212000000
  }
}
```

### Validate Input

**POST** /api/calculator/validate

Validate calculator input and check system capacity without performing calculations.

**Request Body:** Same as calculate endpoint

**Response:**
```json
{
  "valid": true,
  "errors": [],
  "warnings": []
}
```

### Health Check

**GET** /api/calculator/health

Simple health check endpoint.

**Response:**
```json
{
  "status": "healthy",
  "timestamp": "2025-12-08T10:30:00Z"
}
```

## Configuration

### CORS Settings

The API is configured to accept requests from:
- http://localhost:4200 (Angular dev server)
- http://localhost:80 (Docker)
- http://localhost

Modify Program.cs to adjust CORS settings for production.

### Port Configuration

Default port: 5000

To change the port, set the ASPNETCORE_URLS environment variable:
```bash
set ASPNETCORE_URLS=http://+:8080
dotnet run
```

## Docker Deployment

### Build Image

```bash
docker build -t ksail-calculator-api:latest .
```

### Run Container

```bash
docker run -d -p 5000:5000 --name ksail-api ksail-calculator-api:latest
```

### Using Docker Compose

From the client directory:
```bash
docker-compose up -d
```

This will start both the API and Angular frontend.

## Development

### Building

```bash
dotnet build
```

### Running Tests

```bash
dotnet test
```

### Publishing

```bash
dotnet publish -c Release -o ./publish
```

## Calculation Logic

The API implements the exact same calculation logic as the original TypeScript version:

1. **Power Distribution Calculation**
   - Main Engine power (propulsion + shaft generator drive)
   - Shaft Generator power (limited by hotel load or capacity)
   - Auxiliary Engine power (residual hotel load)

2. **SFOC Interpolation**
   - Uses pre-defined SFOC curves for Main and Auxiliary engines
   - Linear interpolation based on engine load percentage

3. **Fuel Consumption**
   - Baseline (current) consumption
   - Optimized (with iEMS) consumption
   - Efficiency factors: Advanced (3%), Pro (4.5%), Premium (6%)
   - Additional factors: SAIL (+0.5%), Battery (+0.5%)

4. **Financial Calculations**
   - Annual cost savings
   - Simple payback period
   - ROI (10-year analysis)
   - NPV (5% discount rate)

## API Integration

The Angular frontend calls this API instead of performing local calculations:

**Before:**
```typescript
// Local calculation
const result = calculateIEMSSavings(input);
```

**After:**
```typescript
// API call
this.http.post<ExtendedCalculationResult>(
  'http://localhost:5000/api/calculator/calculate', 
  input
);
```

## Error Handling

The API returns appropriate HTTP status codes:

- 200 OK: Successful calculation
- 400 Bad Request: Invalid input (with validation details)
- 500 Internal Server Error: Server error

## License

Part of the K-Sail Calculator project.

## Support

For issues or questions, please refer to the main project repository.
