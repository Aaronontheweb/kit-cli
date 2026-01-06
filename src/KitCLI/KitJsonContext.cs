using System.Text.Json;
using System.Text.Json.Serialization;
using KitCLI.Models;
using KitCLI.Services;

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
[JsonSerializable(typeof(SubscribersResponse))]
[JsonSerializable(typeof(Broadcast))]
[JsonSerializable(typeof(Broadcast[]))]
[JsonSerializable(typeof(List<Broadcast>))]
[JsonSerializable(typeof(PaginatedResponse<Broadcast>))]
[JsonSerializable(typeof(SimplePaginatedResponse<Broadcast>))]
[JsonSerializable(typeof(BroadcastsResponse))]
[JsonSerializable(typeof(BroadcastStats))]
[JsonSerializable(typeof(BroadcastStats[]))]
[JsonSerializable(typeof(BroadcastAnalysis))]
[JsonSerializable(typeof(Tag))]
[JsonSerializable(typeof(Tag[]))]
[JsonSerializable(typeof(List<Tag>))]
[JsonSerializable(typeof(SimplePaginatedResponse<Tag>))]
[JsonSerializable(typeof(TagsResponse))]
[JsonSerializable(typeof(Segment))]
[JsonSerializable(typeof(Segment[]))]
[JsonSerializable(typeof(List<Segment>))]
[JsonSerializable(typeof(PaginatedResponse<Segment>))]
[JsonSerializable(typeof(SimplePaginatedResponse<Segment>))]
[JsonSerializable(typeof(SegmentsResponse))]
[JsonSerializable(typeof(SegmentFilter))]
[JsonSerializable(typeof(SegmentFilter[]))]
[JsonSerializable(typeof(SegmentCreateRequest))]
[JsonSerializable(typeof(Sequence))]
[JsonSerializable(typeof(Sequence[]))]
[JsonSerializable(typeof(List<Sequence>))]
[JsonSerializable(typeof(PaginatedResponse<Sequence>))]
[JsonSerializable(typeof(SimplePaginatedResponse<Sequence>))]
[JsonSerializable(typeof(SequencesResponse))]
[JsonSerializable(typeof(SequenceEmail))]
[JsonSerializable(typeof(SimplePaginatedResponse<SequenceEmail>))]
[JsonSerializable(typeof(SequenceEmail[]))]
[JsonSerializable(typeof(List<SequenceEmail>))]
[JsonSerializable(typeof(PaginatedResponse<SequenceEmail>))]
[JsonSerializable(typeof(SequenceEmailsResponse))]
[JsonSerializable(typeof(SequenceSubscriber))]
[JsonSerializable(typeof(SequenceSubscriber[]))]
[JsonSerializable(typeof(PaginatedResponse<SequenceSubscriber>))]
[JsonSerializable(typeof(SequenceSubscribersResponse))]
[JsonSerializable(typeof(SequenceStats))]
[JsonSerializable(typeof(Form))]
[JsonSerializable(typeof(Form[]))]
[JsonSerializable(typeof(List<Form>))]
[JsonSerializable(typeof(FormsResponse))]
[JsonSerializable(typeof(PaginatedResponse<Form>))]
[JsonSerializable(typeof(SimplePaginatedResponse<Form>))]
[JsonSerializable(typeof(IncentiveEmail))]
[JsonSerializable(typeof(ConfigFile))]
[JsonSerializable(typeof(KitConfig))]
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(object[]))]
[JsonSerializable(typeof(ErrorResponse))]
[JsonSerializable(typeof(GitHubRelease))]
[JsonSerializable(typeof(GitHubAsset))]
[JsonSerializable(typeof(GitHubAsset[]))]
[JsonSerializable(typeof(UpdateInfo))]
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
[JsonSerializable(typeof(BroadcastAnalysis))]
[JsonSerializable(typeof(Tag))]
[JsonSerializable(typeof(Tag[]))]
[JsonSerializable(typeof(Segment))]
[JsonSerializable(typeof(Segment[]))]
[JsonSerializable(typeof(Sequence))]
[JsonSerializable(typeof(Sequence[]))]
[JsonSerializable(typeof(SequenceEmail))]
[JsonSerializable(typeof(SequenceEmail[]))]
[JsonSerializable(typeof(SequenceSubscriber[]))]
[JsonSerializable(typeof(SequenceStats))]
[JsonSerializable(typeof(Form))]
[JsonSerializable(typeof(Form[]))]
[JsonSerializable(typeof(IncentiveEmail))]
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
        {
            return DateTimeOffset.MinValue;
        }

        // Try parsing different date formats
        if (DateTimeOffset.TryParse(value, out var result))
        {
            return result;
        }

        // Try Unix timestamp
        if (long.TryParse(value, out var unixTime))
        {
            return DateTimeOffset.FromUnixTimeSeconds(unixTime);
        }

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
