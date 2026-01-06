using FluentAssertions;
using KitCLI.Models;
using System.Text.Json;

namespace KitCLI.Tests.Models;

public class SubscriberTests
{
    [Fact]
    public void Subscriber_Should_Deserialize_Correctly()
    {
        // Arrange
        var json = """
            {
                "id": 12345,
                "email_address": "test@example.com",
                "first_name": "John",
                "state": "active",
                "created_at": "2024-01-15T10:30:00Z",
                "tags": [
                    {"id": 1, "name": "newsletter"},
                    {"id": 2, "name": "customer"}
                ]
            }
            """;

        // Act
        var subscriber = JsonSerializer.Deserialize(json, KitJsonContext.Default.Subscriber);

        // Assert
        subscriber.Should().NotBeNull();
        subscriber!.Id.Should().Be(12345);
        subscriber.EmailAddress.Should().Be("test@example.com");
        subscriber.FirstName.Should().Be("John");
        subscriber.State.Should().Be("active");
        subscriber.Tags.Should().HaveCount(2);
        subscriber.Tags![0].Name.Should().Be("newsletter");
    }

    [Theory]
    [InlineData("active", "active")]
    [InlineData("cancelled", "cancelled")]
    [InlineData("bounced", "bounced")]
    [InlineData("complained", "complained")]
    public void State_Should_Be_Set_Correctly(string state, string expected)
    {
        // Arrange
        var subscriber = new Subscriber { State = state };

        // Act & Assert
        subscriber.State.Should().Be(expected);
    }

    [Fact]
    public void DisplayName_Should_Return_FirstName_When_Available()
    {
        // Arrange
        var subscriber = new Subscriber
        {
            FirstName = "John",
            EmailAddress = "john@example.com"
        };

        // Act & Assert
        subscriber.DisplayName.Should().Be("John");
    }

    [Fact]
    public void DisplayName_Should_Return_Email_When_FirstName_Is_Null()
    {
        // Arrange
        var subscriber = new Subscriber
        {
            FirstName = null,
            EmailAddress = "john@example.com"
        };

        // Act & Assert
        subscriber.DisplayName.Should().Be("john");
    }

    [Fact]
    public void TagList_Should_Return_Comma_Separated_Tags()
    {
        // Arrange
        var subscriber = new Subscriber
        {
            Tags = new[]
            {
                new Tag { Name = "newsletter" },
                new Tag { Name = "vip" },
                new Tag { Name = "customer" }
            }
        };

        // Act & Assert
        subscriber.TagList.Should().Be("newsletter, vip, customer");
    }

    [Fact]
    public void TagList_Should_Return_Empty_String_When_No_Tags()
    {
        // Arrange
        var subscriber = new Subscriber { Tags = null };

        // Act & Assert
        subscriber.TagList.Should().BeEmpty();
    }

    /// <summary>
    /// Regression test for #115: JSON serialization error with Fields property.
    /// The Fields property must be Dictionary&lt;string, JsonElement&gt; for AOT compatibility.
    /// </summary>
    [Fact]
    public void Subscriber_Should_Serialize_With_Fields_Property()
    {
        // Arrange - create subscriber with fields containing various JSON types
        var json = """
            {
                "id": 12345,
                "email_address": "test@example.com",
                "first_name": "John",
                "state": "active",
                "created_at": "2024-01-15T10:30:00Z",
                "fields": {
                    "custom_field": "value",
                    "number_field": 42,
                    "bool_field": true
                }
            }
            """;

        // Act - deserialize and re-serialize (this is what --format json does)
        var subscriber = JsonSerializer.Deserialize(json, KitJsonContext.Default.Subscriber);
        subscriber.Should().NotBeNull();
        subscriber!.Fields.Should().NotBeNull();
        subscriber.Fields!.Should().HaveCount(3);

        // This was failing in #115 because Dictionary<string, object> can't be serialized with AOT
        var serialized = JsonSerializer.Serialize(subscriber, KitJsonIndentedContext.Default.Subscriber);
        serialized.Should().NotBeNullOrEmpty();

        // Verify the fields are in the output
        serialized.Should().Contain("custom_field");
        serialized.Should().Contain("number_field");
    }

    /// <summary>
    /// Regression test for #115: Ensure array output format works with Fields.
    /// </summary>
    [Fact]
    public void SubscriberArray_Should_Serialize_With_Fields_Property()
    {
        // Arrange
        var json = """
            [
                {
                    "id": 1,
                    "email_address": "test1@example.com",
                    "state": "active",
                    "created_at": "2024-01-15T10:30:00Z",
                    "fields": {"company": "Acme Inc"}
                },
                {
                    "id": 2,
                    "email_address": "test2@example.com",
                    "state": "active",
                    "created_at": "2024-01-15T10:30:00Z",
                    "fields": null
                }
            ]
            """;

        // Act
        var subscribers = JsonSerializer.Deserialize(json, KitJsonContext.Default.SubscriberArray);
        subscribers.Should().NotBeNull();
        subscribers!.Should().HaveCount(2);

        // This should not throw (was causing #115)
        var serialized = JsonSerializer.Serialize(subscribers, KitJsonIndentedContext.Default.SubscriberArray);
        serialized.Should().NotBeNullOrEmpty();
        serialized.Should().Contain("test1@example.com");
        serialized.Should().Contain("test2@example.com");
    }
}
