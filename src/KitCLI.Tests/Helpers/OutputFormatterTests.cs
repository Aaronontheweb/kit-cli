using FluentAssertions;
using KitCLI.Helpers;
using KitCLI.Models;

namespace KitCLI.Tests.Helpers;

[Collection("Console Output Tests")]
public class OutputFormatterTests
{
    [Fact]
    public void PrintSubscribers_Should_Handle_Empty_List()
    {
        // Arrange
        var originalOut = Console.Out;
        try
        {
            var subscribers = Array.Empty<Subscriber>();
            var writer = new StringWriter();
            Console.SetOut(writer);

            // Act
            OutputFormatter.PrintSubscribers(subscribers, "table");

            // Assert
            var output = writer.ToString();
            output.Should().Contain("No subscribers found");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void PrintSubscribers_Should_Format_Table_Correctly()
    {
        // Arrange
        var originalOut = Console.Out;
        try
        {
            var subscribers = new[]
            {
                new Subscriber
                {
                    Id = 1,
                    EmailAddress = "test@example.com",
                    FirstName = "John",
                    State = "active",
                    CreatedAt = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc),
                    Tags = new[] { new Tag { Name = "vip" } }
                }
            };

            var writer = new StringWriter();
            Console.SetOut(writer);

            // Act
            OutputFormatter.PrintSubscribers(subscribers, "table");

            // Assert
            var output = writer.ToString();
            output.Should().Contain("test@example.com");
            output.Should().Contain("John");
            output.Should().Contain("active");
            output.Should().Contain("vip");
            output.Should().Contain("2024-01-15");
            output.Should().Contain("Total: 1 subscriber(s)");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void PrintSubscribers_Should_Format_Json_Correctly()
    {
        // Arrange
        var originalOut = Console.Out;
        try
        {
            var subscribers = new[]
            {
                new Subscriber
                {
                    Id = 1,
                    EmailAddress = "test@example.com",
                    State = "active"
                }
            };

            var writer = new StringWriter();
            Console.SetOut(writer);

            // Act
            OutputFormatter.PrintSubscribers(subscribers, "json");

            // Assert
            var output = writer.ToString();
            output.Should().Contain("\"id\": 1");
            output.Should().Contain("\"email_address\": \"test@example.com\"");
            output.Should().Contain("\"state\": \"active\"");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void PrintSubscribers_Should_Format_Csv_Correctly()
    {
        // Arrange
        var originalOut = Console.Out;
        try
        {
            var subscribers = new[]
            {
                new Subscriber
                {
                    Id = 1,
                    EmailAddress = "test@example.com",
                    FirstName = "John",
                    State = "active",
                    CreatedAt = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc),
                    Tags = new[] { new Tag { Name = "vip" }, new Tag { Name = "customer" } }
                }
            };

            var writer = new StringWriter();
            Console.SetOut(writer);

            // Act
            OutputFormatter.PrintSubscribers(subscribers, "csv");

            // Assert
            var output = writer.ToString();
            output.Should().NotBeNullOrEmpty();

            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.TrimEnd('\r')) // Handle Windows line endings
                .ToArray();

            lines.Should().HaveCountGreaterOrEqualTo(2, "CSV output should have header and at least one data row");
            lines[0].Should().Be("id,email_address,first_name,state,tags,created_at");
            lines[1].Should().Contain("1,test@example.com,John,active,\"vip, customer\",2024-01-15T10:30:00Z");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void PrintBroadcastStats_Should_Display_All_Metrics()
    {
        // Arrange
        var originalOut = Console.Out;
        try
        {
            var stats = new BroadcastStats
            {
                Recipients = 1000,
                EmailsOpened = 400,
                OpenRate = 0.4,
                TotalClicks = 100,
                ClickRate = 0.1,
                Unsubscribes = 5,
                Status = "completed"
            };

            var writer = new StringWriter();
            Console.SetOut(writer);

            // Act
            OutputFormatter.PrintBroadcastStats(stats, 123);

            // Assert
            var output = writer.ToString();
            output.Should().Contain("Broadcast Statistics (ID: 123)");
            output.Should().Contain("Recipients:      1,000");
            // Check for percentage with or without space (varies by culture)
            output.Should().Match(s => s.Contains("Opens:           400 (40.0%)") || s.Contains("Opens:           400 (40.0 %)"));
            output.Should().Match(s => s.Contains("Clicks:          100 (10.0%)") || s.Contains("Clicks:          100 (10.0 %)"));
            output.Should().Contain("Unsubscribes:    5");
            output.Should().Contain("Status:          completed");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Theory]
    [InlineData("test,value", "\"test,value\"")]
    [InlineData("test\"value", "\"test\"\"value\"")]
    [InlineData("test\nvalue", "\"test\nvalue\"")]
    [InlineData("simple", "simple")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void EscapeCsvField_Should_Handle_Special_Characters(string? input, string expected)
    {
        // Use reflection to test the private method
        var method = typeof(OutputFormatter)
            .GetMethod("EscapeCsvField",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        // Act
        var result = method?.Invoke(null, new object?[] { input });

        // Assert
        result.Should().Be(expected);
    }
}
