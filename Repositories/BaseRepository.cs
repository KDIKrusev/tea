using Microsoft.Data.SqlClient;
using System.Text.Json;

namespace KSailCalc.Api.Repositories;

/// <summary>
/// Base class for repositories. Provides connection string and JSON deserialization.
/// </summary>
public abstract class BaseRepository
{
    protected readonly string ConnectionString;

    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    protected BaseRepository(IConfiguration configuration)
    {
        ConnectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    }

    protected static T? DeserializeJson<T>(string json) where T : class
    {
        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    protected static List<T> DeserializeJsonList<T>(string json)
    {
        return JsonSerializer.Deserialize<List<T>>(json, JsonOptions) ?? new List<T>();
    }
}