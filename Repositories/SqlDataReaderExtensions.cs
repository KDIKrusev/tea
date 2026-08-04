using Microsoft.Data.SqlClient;

namespace KSailCalc.Api.Repositories;

/// <summary>
/// Read result columns by name instead of by ordinal.
///
/// Ordinal access (<c>reader.GetInt32(6)</c>) is correct only for as long as nobody edits the
/// SELECT list. Insert a column in the middle and every read after it silently shifts to the wrong
/// value — no exception, just wrong data, and this repository has no automated coverage to catch it
/// (the loaders need a live SQL Server).
///
/// The column names passed here are the ones the SELECT statements already list verbatim, so a
/// wrong name cannot slip through: the query itself would fail first.
/// </summary>
internal static class SqlDataReaderExtensions
{
    internal static int GetInt32(this SqlDataReader reader, string column)
        => reader.GetInt32(reader.GetOrdinal(column));

    internal static decimal GetDecimal(this SqlDataReader reader, string column)
        => reader.GetDecimal(reader.GetOrdinal(column));

    internal static string GetString(this SqlDataReader reader, string column)
        => reader.GetString(reader.GetOrdinal(column));

    internal static bool GetBoolean(this SqlDataReader reader, string column)
        => reader.GetBoolean(reader.GetOrdinal(column));

    internal static string? GetStringOrNull(this SqlDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    internal static decimal? GetDecimalOrNull(this SqlDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetDecimal(ordinal);
    }

    internal static int? GetInt32OrNull(this SqlDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
    }
}
