global using Xunit;

// The production service namespaces, imported once for the whole test project.
// Tests reach across every area (calculation, battery, catalog, validation) far more often than
// production code does, so importing them here keeps the per-file using blocks about the test's
// own subject rather than about where a service happens to live.
global using KSailCalc.Api.Services.Battery;
global using KSailCalc.Api.Services.Calculation;
global using KSailCalc.Api.Services.Catalog;
global using KSailCalc.Api.Services.Validation;

global using KSailCalc.Api.Models.Domain;
global using KSailCalc.Api.Models.Settings;

global using Microsoft.Extensions.Logging.Abstractions;
