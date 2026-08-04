using KSailCalc.Api.Models;

namespace KSailCalc.Api.Services.Interfaces;

public interface ISfocService
{
    /// <summary>
    /// Both curves a calculation needs, resolved once. Filtering and sorting happen here instead of
    /// on every SFOC lookup inside the Level 1 candidate loop and the Level 3 generator loop.
    ///
    /// This is the only way to obtain SFOC. The per-load lookup that used to sit beside it was
    /// removed once nothing called it: read a curve and interpolate against it.
    /// </summary>
    Task<EngineFuelCurves> GetCurvesAsync(CalculatorInput input);
}
