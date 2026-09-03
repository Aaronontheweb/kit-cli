using FluentAssertions;
using KitCLI.Models;
using System.Text.Json;

namespace KitCLI.Tests.Models;

public class SequenceEmailTests
{
    [Fact]
    public void SequenceEmail_Should_Serialize_With_V4_Snake_Case_Fields()
    {
        // Arrange
        var email = new SequenceEmail
        {
            Id = 5,
            SequenceId = 42,
            Subject = "Welcome",
            PreviewText = "Hi there",
            EmailAddress = "team@example.com",
            EmailTemplateId = 77,
            Published = true,
            Position = 1,
            DelayValue = 3,
            DelayUnit = "days",
            SendDays = new[] { "monday", "wednesday" },
            Content = "<p>Hello</p>"
        };

        // Act
        var json = JsonSerializer.Serialize(email, KitJsonContext.Default.SequenceEmail);

        // Assert
        json.Should().Contain("\"delay_value\":3");
        json.Should().Contain("\"delay_unit\":\"days\"");
        json.Should().Contain("\"email_address\":\"team@example.com\"");
        json.Should().Contain("\"email_template_id\":77");
        json.Should().Contain("\"send_days\":[\"monday\",\"wednesday\"]");
        json.Should().NotContain("delay_days");
        json.Should().NotContain("delay_hours");
        json.Should().NotContain("from_name");
        json.Should().NotContain("from_email");
        json.Should().NotContain("\"template_id\"");
    }

    [Fact]
    public void SequenceEmail_Should_Deserialize_V4_Json_Correctly()
    {
        // Arrange
        var json = """
            {
                "id": 5,
                "sequence_id": 42,
                "subject": "Welcome",
                "preview_text": "Hi",
                "email_address": "team@example.com",
                "email_template_id": 77,
                "published": true,
                "position": 1,
                "delay_value": 3,
                "delay_unit": "days",
                "send_days": ["monday"],
                "content": "<p>Hello</p>"
            }
            """;

        // Act
        var email = JsonSerializer.Deserialize(json, KitJsonContext.Default.SequenceEmail);

        // Assert
        email.Should().NotBeNull();
        email!.EmailAddress.Should().Be("team@example.com");
        email.EmailTemplateId.Should().Be(77);
        email.Published.Should().BeTrue();
        email.Position.Should().Be(1);
        email.DelayValue.Should().Be(3);
        email.DelayUnit.Should().Be("days");
        email.SendDays.Should().Contain("monday");
        email.Content.Should().Be("<p>Hello</p>");
    }

    [Theory]
    [InlineData(3, "days", "3d")]
    [InlineData(5, "hours", "5h")]
    [InlineData(1, "days", "1d")]
    [InlineData(0, "days", "Immediately")]
    public void DelayFormatted_Should_Format_From_Delay_Value_And_Unit(int value, string unit, string expected)
    {
        // Arrange
        var email = new SequenceEmail { DelayValue = value, DelayUnit = unit };

        // Act & Assert
        email.DelayFormatted.Should().Be(expected);
    }

    [Fact]
    public void SendDaysFormatted_Should_Join_Send_Days()
    {
        // Arrange
        var email = new SequenceEmail { SendDays = new[] { "monday", "wednesday", "friday" } };

        // Act & Assert
        email.SendDaysFormatted.Should().Be("monday, wednesday, friday");
    }

    [Fact]
    public void SendDaysFormatted_Should_Return_Every_Day_When_Empty()
    {
        // Arrange
        var email = new SequenceEmail { SendDays = null };

        // Act & Assert
        email.SendDaysFormatted.Should().Be("Every day");
    }

    [Fact]
    public void SequenceEmailStats_Should_Deserialize_V4_Stats_Json()
    {
        // Arrange
        var json = """
            {
                "recipients": 100,
                "opens": 40,
                "clicks": 10,
                "email_unsubscribes": 2,
                "bounces": 1,
                "complaints": 0,
                "open_rate": 40.0,
                "click_rate": 10.0,
                "click_to_open_rate": 25.0,
                "unsubscribe_rate": 2.0,
                "bounce_rate": 1.0,
                "complaint_rate": 0.0
            }
            """;

        // Act
        var stats = JsonSerializer.Deserialize(json, KitJsonContext.Default.SequenceEmailStats);

        // Assert
        stats.Should().NotBeNull();
        stats!.Recipients.Should().Be(100);
        stats.Opens.Should().Be(40);
        stats.Clicks.Should().Be(10);
        stats.EmailUnsubscribes.Should().Be(2);
        stats.Bounces.Should().Be(1);
        stats.Complaints.Should().Be(0);
        stats.OpenRate.Should().Be(40.0);
        stats.ClickRate.Should().Be(10.0);
        stats.ClickToOpenRate.Should().Be(25.0);
        stats.UnsubscribeRate.Should().Be(2.0);
        stats.BounceRate.Should().Be(1.0);
        stats.ComplaintRate.Should().Be(0.0);
    }

    [Fact]
    public void SequenceEmailUpdateRequest_ForSubject_Should_Serialize_Only_Subject()
    {
        // Act
        var json = JsonSerializer.Serialize(
            SequenceEmailUpdateRequest.ForSubject("Hi {{ subscriber.first_name }}"),
            KitJsonContext.Default.SequenceEmailUpdateRequest);

        // Assert — exactly the subject field, nothing else that could reorder or send emails.
        json.Should().Contain("\"subject\":\"Hi {{ subscriber.first_name }}\"");
        json.Should().NotContain("content");
        json.Should().NotContain("position");
        json.Should().NotContain("published");
        json.Should().NotContain("delay_value");
        json.Should().NotContain("delay_unit");
        json.Should().NotContain("send_days");
        json.Should().NotContain("email_template_id");
        json.Should().NotContain("preview_text");
    }

    [Fact]
    public void SequenceEmailUpdateRequest_ForContent_Should_Serialize_Only_Content()
    {
        // Act
        var json = JsonSerializer.Serialize(
            SequenceEmailUpdateRequest.ForContent("<p>Hello {{ subscriber.first_name }}</p>"),
            KitJsonContext.Default.SequenceEmailUpdateRequest);

        // Assert — exactly one property (content), round-tripping to the exact HTML.
        // (System.Text.Json escapes < and > on the wire; assert via the parsed document, not raw bytes.)
        using var document = JsonDocument.Parse(json);
        document.RootElement.EnumerateObject().Should().ContainSingle();
        document.RootElement.GetProperty("content").GetString().Should().Be("<p>Hello {{ subscriber.first_name }}</p>");
    }

    [Fact]
    public void Sequence_Should_Deserialize_Object_Exclude_Subscriber_Sources()
    {
        // Regression for #155: Kit v4 returns exclude_subscriber_sources as structured objects, not
        // strings; modeling it as string[] broke `sequence get` and manifest generation.
        const string json = """
        {
          "sequence": {
            "id": 42,
            "name": "Bootcamp 2.0",
            "exclude_subscriber_sources": [
              { "type": "tag", "ids": [123, 456] }
            ]
          }
        }
        """;

        var response = JsonSerializer.Deserialize(json, KitJsonContext.Default.SequenceResponse);

        response.Should().NotBeNull();
        var sequence = response!.Sequence;
        sequence.Should().NotBeNull();
        sequence!.ExcludeSubscriberSources.Should().HaveCount(1);
        sequence.ExcludeSubscriberSources![0].Type.Should().Be("tag");
        sequence.ExcludeSubscriberSources[0].Ids.Should().Equal(123, 456);
    }
}
