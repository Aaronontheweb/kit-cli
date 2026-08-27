using FluentAssertions;
using KitCLI.Commands;
using KitCLI.Models;
using KitCLI.Tests.Mocks;

namespace KitCLI.Tests.Commands;

/// <summary>
/// Tests for the form subscription commands (subscribe / subscribe-bulk).
/// Uses Console.SetOut/SetError for capturing output, so must not run in parallel with other console tests.
/// </summary>
[Collection("Console Output Tests")]
public class FormCommandsTests : IDisposable
{
    private readonly TextWriter _originalOut;
    private readonly TextWriter _originalError;

    public FormCommandsTests()
    {
        _originalOut = Console.Out;
        _originalError = Console.Error;
    }

    public void Dispose()
    {
        Console.SetOut(_originalOut);
        Console.SetError(_originalError);
    }

    [Fact]
    public async Task HandleSubscribe_Should_Subscribe_By_Email()
    {
        // Arrange
        long? capturedFormId = null;
        string? capturedEmail = null;
        string? capturedReferrer = null;

        var mockClient = new MockKitApiClient
        {
            AddSubscriberToFormAsyncFunc = (formId, email, referrer, ct) =>
            {
                capturedFormId = formId;
                capturedEmail = email;
                capturedReferrer = referrer;
                return Task.FromResult(true);
            }
        };

        var writer = new StringWriter();
        Console.SetOut(writer);
        var errorWriter = new StringWriter();
        Console.SetError(errorWriter);

        // Act
        var result = await FormCommands.HandleSubscribe(["123", "user@test.com", "--referrer", "https://example.com"], mockClient);

        // Assert
        result.Should().Be(0);
        writer.ToString().Should().Contain("✅ Subscribed user@test.com to form 123");
        capturedFormId.Should().Be(123);
        capturedEmail.Should().Be("user@test.com");
        capturedReferrer.Should().Be("https://example.com");
        errorWriter.ToString().Should().BeEmpty();
    }

    [Fact]
    public async Task HandleSubscribe_Should_Resolve_Email_By_Subscriber_Id()
    {
        // Arrange
        var mockClient = new MockKitApiClient
        {
            GetSubscriberAsyncFunc = (id, ct) => Task.FromResult<Subscriber?>(
                new Subscriber { Id = id, EmailAddress = "resolved@test.com" }),
            AddSubscriberToFormAsyncFunc = (formId, email, referrer, ct) => Task.FromResult(true)
        };

        var writer = new StringWriter();
        Console.SetOut(writer);

        // Act
        var result = await FormCommands.HandleSubscribe(["123", "456"], mockClient);

        // Assert
        result.Should().Be(0);
        writer.ToString().Should().Contain("✅ Subscribed resolved@test.com to form 123");
    }

    [Fact]
    public async Task HandleSubscribe_Should_Return_Error_When_Subscriber_Not_Found()
    {
        // Arrange
        var mockClient = new MockKitApiClient
        {
            GetSubscriberAsyncFunc = (id, ct) => Task.FromResult<Subscriber?>(null)
        };

        var writer = new StringWriter();
        Console.SetOut(writer);
        var errorWriter = new StringWriter();
        Console.SetError(errorWriter);

        // Act
        var result = await FormCommands.HandleSubscribe(["123", "999"], mockClient);

        // Assert
        result.Should().Be(1);
        errorWriter.ToString().Should().Contain("Subscriber not found: 999");
    }

    [Fact]
    public async Task HandleSubscribe_Should_Return_Error_When_Form_Not_Found()
    {
        // Arrange
        var mockClient = new MockKitApiClient
        {
            AddSubscriberToFormAsyncFunc = (formId, email, referrer, ct) => Task.FromResult(false)
        };

        var writer = new StringWriter();
        Console.SetOut(writer);
        var errorWriter = new StringWriter();
        Console.SetError(errorWriter);

        // Act
        var result = await FormCommands.HandleSubscribe(["123", "user@test.com"], mockClient);

        // Assert
        result.Should().Be(1);
        errorWriter.ToString().Should().Contain("Failed to subscribe user@test.com to form 123");
    }

    [Fact]
    public async Task HandleSubscribeBulk_Should_Subscribe_All_Emails_From_File()
    {
        // Arrange - file contains comments and blank lines that should be filtered out
        var tempFile = Path.Combine(Path.GetTempPath(), $"kitcli-form-bulk-{Guid.NewGuid():N}.txt");
        await File.WriteAllLinesAsync(tempFile,
        [
            "one@test.com",
            "# a comment line",
            "",
            "two@test.com",
            "three@test.com"
        ]);

        var subscribed = new List<string>();
        var mockClient = new MockKitApiClient
        {
            AddSubscriberToFormAsyncFunc = (formId, email, referrer, ct) =>
            {
                subscribed.Add(email);
                return Task.FromResult(true);
            }
        };

        var writer = new StringWriter();
        Console.SetOut(writer);

        try
        {
            // Act
            var result = await FormCommands.HandleSubscribeBulk(["123", tempFile, "--force"], mockClient);

            // Assert
            result.Should().Be(0);
            subscribed.Should().BeEquivalentTo(["one@test.com", "two@test.com", "three@test.com"]);
            var output = writer.ToString();
            output.Should().Contain("Loaded 3 email address(es) for form 123");
            output.Should().Contain("Subscribed: 3, Failed: 0");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task HandleSubscribeBulk_Should_Return_Exit_Code_1_When_Some_Records_Fail()
    {
        // Arrange
        var mockClient = new MockKitApiClient
        {
            AddSubscriberToFormAsyncFunc = (formId, email, referrer, ct) =>
                Task.FromResult(!email.Equals("bad@test.com", StringComparison.OrdinalIgnoreCase))
        };

        var writer = new StringWriter();
        Console.SetOut(writer);
        var errorWriter = new StringWriter();
        Console.SetError(errorWriter);

        // Act
        var result = await FormCommands.HandleSubscribeBulk(["123", "good@test.com,bad@test.com", "--force"], mockClient);

        // Assert
        result.Should().Be(1);
        writer.ToString().Should().Contain("Subscribed: 1, Failed: 1");
        errorWriter.ToString().Should().Contain("Failed to subscribe bad@test.com");
    }

    [Fact]
    public async Task RouteFormSubcommand_Should_Block_Subscribe_In_Read_Only_Mode()
    {
        // Arrange
        var calls = 0;
        var mockClient = new MockKitApiClient
        {
            AddSubscriberToFormAsyncFunc = (formId, email, referrer, ct) =>
            {
                calls++;
                return Task.FromResult(true);
            }
        };

        var writer = new StringWriter();
        Console.SetOut(writer);
        var errorWriter = new StringWriter();
        Console.SetError(errorWriter);

        // Act
        var result = await FormCommandRouter.RouteFormSubcommand(["subscribe", "123", "user@test.com"], true, mockClient);

        // Assert
        result.Should().Be(1);
        calls.Should().Be(0);
        errorWriter.ToString().Should().Contain("'form subscribe' is not allowed in read-only mode");
        writer.ToString().Should().NotContain("Subscribed");
    }

    [Fact]
    public async Task RouteFormSubcommand_Should_Block_SubscribeBulk_In_Read_Only_Mode()
    {
        // Arrange
        var calls = 0;
        var mockClient = new MockKitApiClient
        {
            AddSubscriberToFormAsyncFunc = (formId, email, referrer, ct) =>
            {
                calls++;
                return Task.FromResult(true);
            }
        };

        var errorWriter = new StringWriter();
        Console.SetError(errorWriter);

        // Act
        var result = await FormCommandRouter.RouteFormSubcommand(["subscribe-bulk", "123", "a@test.com,b@test.com"], true, mockClient);

        // Assert
        result.Should().Be(1);
        calls.Should().Be(0);
        errorWriter.ToString().Should().Contain("'form subscribe-bulk' is not allowed in read-only mode");
    }

    [Fact]
    public async Task HandleSubscribeBulk_Should_Error_When_File_Not_Found_And_Not_Call_Api()
    {
        // Arrange - a filename that does not exist and does not look like an inline email list
        var missingFile = Path.Combine(Path.GetTempPath(), $"kitcli-missing-{Guid.NewGuid():N}.csv");
        var calls = 0;
        var mockClient = new MockKitApiClient
        {
            AddSubscriberToFormAsyncFunc = (formId, email, referrer, ct) =>
            {
                calls++;
                return Task.FromResult(true);
            }
        };

        var writer = new StringWriter();
        Console.SetOut(writer);
        var errorWriter = new StringWriter();
        Console.SetError(errorWriter);

        // Act
        var result = await FormCommands.HandleSubscribeBulk(["123", missingFile, "--force"], mockClient);

        // Assert
        result.Should().Be(1);
        calls.Should().Be(0);
        errorWriter.ToString().Should().Contain($"File not found: {missingFile}");
        writer.ToString().Should().NotContain("Subscribed");
    }

    [Fact]
    public async Task RouteFormSubcommand_Should_Return_Help_When_No_Subcommand()
    {
        // Arrange
        var calls = 0;
        var mockClient = new MockKitApiClient
        {
            AddSubscriberToFormAsyncFunc = (formId, email, referrer, ct) =>
            {
                calls++;
                return Task.FromResult(true);
            }
        };

        var writer = new StringWriter();
        Console.SetOut(writer);

        // Act
        var result = await FormCommandRouter.RouteFormSubcommand([], false, mockClient);

        // Assert
        result.Should().Be(0);
        writer.ToString().Should().Contain("Usage: kit form <subcommand> [options]");
        calls.Should().Be(0);
    }
}
