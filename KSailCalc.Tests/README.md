# KSailCalc Unit Tests

This test project contains comprehensive unit tests for the iEMS calculator service.

## Test Coverage

### CalculatorServiceTests (43 tests)

#### 1. SFOC Interpolation Tests (7 tests)
- ✅ Correct SFOC at boundary points (0%, 25%, 50%, 75%, 90%, 100%)
- ✅ SFOC interpolation between data points
- ✅ All defined load points for ME and AE

#### 2. Power Distribution Tests (5 tests)
- ✅ Shaft Generator usage when covering all hotel load
- ✅ Auxiliary Engine usage when hotel load exceeds SG capacity
- ✅ Main Engine power calculation including sea margin
- ✅ SG power capped at maximum capacity
- ✅ Correct power routing through all three systems (ME, SG, AE)

#### 3. Variant and Efficiency Tests (8 tests)
- ✅ Correct efficiency factors for all variants:
  - Advanced: 97% (3% reduction)
  - Pro: 95.5% (4.5% reduction)
  - Premium: 94% (6% reduction)
- ✅ Additional 0.5% efficiency with SAIL system
- ✅ Additional 0.5% efficiency with battery
- ✅ Combined efficiency with both SAIL and battery
- ✅ Correct investment calculations per variant

#### 4. Fuel Consumption Tests (4 tests)
- ✅ Correct fuel savings calculations
- ✅ Fuel breakdown by engine type (ME vs AE)
- ✅ Fuel consumption scaling with annual hours
- ✅ Optimized vs baseline FOC

#### 5. CO2 Emissions Tests (2 tests)
- ✅ CO2 emissions using correct factor (3.206 kg/kg)
- ✅ CO2 reduction percentage matching fuel reduction

#### 6. Financial Calculation Tests (5 tests)
- ✅ Annual cost savings based on fuel price
- ✅ Payback period calculation
- ✅ ROI over 10 years
- ✅ NPV with 5% discount rate
- ✅ Manual NPV verification

#### 7. Edge Cases and Validation Tests (10 tests)
- ✅ Zero main engine capacity
- ✅ Zero auxiliary engine capacity
- ✅ Zero auxiliary engine count
- ✅ Invalid variant exception
- ✅ Zero hotel load
- ✅ Zero propulsion power (anchored vessel)
- ✅ Maximum annual hours (8760)
- ✅ High sea margin (50%)
- ✅ No battery capacity
- ✅ Power demands structure validation

#### 8. Integration Tests (2 tests)
- ✅ Realistic results for typical vessel configuration
- ✅ Premium variant outperforms Advanced variant

## Running the Tests

### Run all tests
```bash
dotnet test KSailCalc.Tests\KSailCalc.Tests.csproj
```

### Run with detailed output
```bash
dotnet test KSailCalc.Tests\KSailCalc.Tests.csproj --verbosity normal
```

### Run specific test
```bash
dotnet test --filter "FullyQualifiedName~CalculatorServiceTests.CalculateIEMSSavings_ShouldCalculateCorrectFuelSavings"
```

### Run tests by category
```bash
dotnet test --filter "Category=SFOC"
```

## Test Structure

```
KSailCalc.Tests/
├── Services/
│   └── CalculatorServiceTests.cs      # Main calculator tests
├── TestHelpers/
│   └── CalculatorInputBuilder.cs      # Builder pattern for test data
└── README.md
```

## Test Data Builder

The `CalculatorInputBuilder` class provides a fluent interface for creating test input data:

```csharp
var input = CalculatorInputBuilder.Default()
    .WithIemsVariant("Pro")
    .WithMainEngines(5000, 2)
    .WithSail(true)
    .WithBattery(500)
    .Build();
```

## Dependencies

- **xUnit**: Testing framework
- **FluentAssertions**: Fluent assertion library for more readable tests
- **KSailCalc.Api**: Reference to the main API project

## Test Results

All 43 tests passing ✅

**Execution Time**: ~5.8 seconds
