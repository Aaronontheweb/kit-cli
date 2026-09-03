using System.Text.Json;
using FluentAssertions;
using KitCLI.Models;

namespace KitCLI.Tests.Models;

public class FormTests
{
    [Fact]
    public void Form_Should_Deserialize_When_UpdatedAt_Is_Explicit_Null()
    {
        // Regression: deserializing a JSON null into a non-nullable DateTime (value type) throws.
        // The Kit v4 forms response can omit or null updated_at, so the field must be nullable.
        const string json = """
        {"form":{"id":7,"name":"Newsletter","type":"embed","format":"inline","created_at":"2023-02-17T11:43:55Z","updated_at":null}}
        """;

        var response = JsonSerializer.Deserialize(json, KitJsonContext.Default.FormResponse);

        response.Should().NotBeNull();
        response!.Form.Should().NotBeNull();
        response.Form!.Id.Should().Be(7);
        response.Form.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Form_Should_Deserialize_When_UpdatedAt_Is_Omitted()
    {
        const string json = """
        {"form":{"id":7,"name":"Newsletter","created_at":"2023-02-17T11:43:55Z"}}
        """;

        var response = JsonSerializer.Deserialize(json, KitJsonContext.Default.FormResponse);

        response!.Form!.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Form_Should_Deserialize_Present_UpdatedAt()
    {
        const string json = """
        {"form":{"id":7,"name":"Newsletter","created_at":"2023-02-17T11:43:55Z","updated_at":"2023-02-18T11:43:55Z"}}
        """;

        var response = JsonSerializer.Deserialize(json, KitJsonContext.Default.FormResponse);

        response!.Form!.UpdatedAt.Should().NotBeNull();
        response.Form.UpdatedAt!.Value.Should().Be(new DateTime(2023, 2, 18, 11, 43, 55));
    }
}
