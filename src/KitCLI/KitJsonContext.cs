using System.Text.Json;
using System.Text.Json.Serialization;
using KitCLI.Models;

namespace KitCLI;

// Main context for API communication (snake_case)
[JsonSourceGenerationOptions(
    WriteIndented = false,
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    Converters = [typeof(DateTimeOffsetConverter)])]
[JsonSerializable(typeof(Subscriber))]
[JsonSerializable(typeof(Subscriber[]))]
[JsonSerializable(typeof(List<Subscriber>))]
[JsonSerializable(typeof(PaginatedResponse<Subscriber>))]
[JsonSerializable(typeof(SimplePaginatedResponse<Subscriber>))]
[JsonSerializable(typeof(Broadcast))]
[JsonSerializable(typeof(Broadcast[]))]
[JsonSerializable(typeof(List<Broadcast>))]
[JsonSerializable(typeof(PaginatedResponse<Broadcast>))]
[JsonSerializable(typeof(SimplePaginatedResponse<Broadcast>))]
[JsonSerializable(typeof(BroadcastStats))]
[JsonSerializable(typeof(BroadcastStats[]))]
[JsonSerializable(typeof(Tag))]
[JsonSerializable(typeof(Tag[]))]
[JsonSerializable(typeof(List<Tag>))]
[JsonSerializable(typeof(ConfigFile))]
[JsonSerializable(typeof(KitConfig))]
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(ErrorResponse))]
public partial class KitJsonContext : JsonSerializerContext
{
}

// Separate context for formatted output (indented)
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(Subscriber))]
[JsonSerializable(typeof(Subscriber[]))]
[JsonSerializable(typeof(Broadcast))]
[JsonSerializable(typeof(Broadcast[]))]
[JsonSerializable(typeof(BroadcastStats))]
[JsonSerializable(typeof(Tag))]
[JsonSerializable(typeof(Tag[]))]
public partial class KitJsonIndentedContext : JsonSerializerContext
{
}

// Custom converters
public sealed class DateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (string.IsNullOrEmpty(value))
            return DateTimeOffset.MinValue;
        
        // Try parsing different date formats
        if (DateTimeOffset.TryParse(value, out var result))
            return result;
        
        // Try Unix timestamp
        if (long.TryParse(value, out var unixTime))
            return DateTimeOffset.FromUnixTimeSeconds(unixTime);
        
        return DateTimeOffset.MinValue;
    }
    
    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("yyyy-MM-dd'T'HH:mm:ss.fffK"));
    }
}

public sealed class ErrorResponse
{
    [JsonPropertyName("error")]
    public string? Error { get; set; }
    
    [JsonPropertyName("message")]
    public string? Message { get; set; }
    
    [JsonPropertyName("errors")]
    public Dictionary<string, string[]>? Errors { get; set; }
}