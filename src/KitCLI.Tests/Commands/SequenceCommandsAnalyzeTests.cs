using FluentAssertions;
using KitCLI.Commands;
using KitCLI.Models;
using KitCLI.Tests.Mocks;

namespace KitCLI.Tests.Commands;

/// <summary>
/// Tests for the sequence analyze command.
/// Uses Console.SetOut for capturing output, so must not run in parallel with other console tests.
/// </summary>
[Collection("Console Output Tests")]
public class SequenceCommandsAnalyzeTests : IDisposable
{
    private readonly TextWriter _originalOut;
    private readonly TextWriter _originalError;

    public SequenceCommandsAnalyzeTests()
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
    public async Task HandleAnalyze_Should_Render_OpenRate_On_0_100_Scale_As_Percentage()
    {
        // Arrange - Kit V4 stats are percentages on a 0-100 scale (e.g. 40.0 = 40%),
        // so 40.0 must render as "40.00%" not "4000.0%"
        var stats = new SequenceStats
        {
            SequenceId = 42,
            TotalSubscribers = 100,
            ActiveSubscribers = 60,
            CancelledSubscribers = 0,
            AverageOpenRate = 40.0,
            AverageClickRate = 10.0,
            EmailsSent = 1000
        };

        var mockClient = new MockKitApiClient
        {
            GetSequenceStatsAsyncFunc = (_, _) => Task.FromResult<SequenceStats?>(stats)
        };

        var writer = new StringWriter();
        Console.SetOut(writer);

        // Act
        var result = await SequenceCommands.HandleAnalyze(["42"], mockClient);

        // Assert
        result.Should().Be(0);
        var output = writer.ToString();
        output.Should().Contain("Average Open Rate: 40.00%");
        output.Should().NotContain("4000.0%");
        output.Should().Contain("Average Click Rate: 10.00%");
    }

    [Fact]
    public async Task HandleAnalyze_Should_Render_Zero_Percent_For_Zero_Total_Subscribers()
    {
        var stats = new SequenceStats
        {
            SequenceId = 42,
            TotalSubscribers = 0,
            ActiveSubscribers = 0
        };
        var mockClient = new MockKitApiClient
        {
            GetSequenceStatsAsyncFunc = (_, _) => Task.FromResult<SequenceStats?>(stats)
        };
        var writer = new StringWriter();
        Console.SetOut(writer);

        var result = await SequenceCommands.HandleAnalyze(["42"], mockClient);

        result.Should().Be(0);
        var output = writer.ToString();
        output.Should().MatchRegex(@"Active: 0 \(0\.0 ?%\)");
        output.Should().NotContain("NaN");
        output.Should().NotContain("Infinity");
    }

    [Fact]
    public async Task HandleStats_Should_Return_NotFound_Before_Requesting_Email_Pages()
    {
        var emailPagesRequested = false;
        var mockClient = new MockKitApiClient
        {
            GetSequenceAsyncFunc = (_, _) => Task.FromResult<Sequence?>(null),
            GetAllSequenceEmailsAsyncFunc = (_, _, _, _) =>
            {
                emailPagesRequested = true;
                return EmptyEmails();
            }
        };
        var writer = new StringWriter();
        Console.SetOut(writer);

        var result = await SequenceCommands.HandleStats(["42"], mockClient);

        result.Should().Be(1);
        emailPagesRequested.Should().BeFalse();
        writer.ToString().Should().Contain("Sequence not found: 42");
    }

    [Fact]
    public async Task HandleStats_Should_Weight_Email_Rates_By_Recipients()
    {
        var mockClient = new MockKitApiClient
        {
            GetSequenceAsyncFunc = (_, _) => Task.FromResult<Sequence?>(new Sequence { Id = 42, Name = "Welcome" }),
            GetAllSequenceEmailsAsyncFunc = (_, _, _, _) => ReturnEmails(
                new SequenceEmail { Stats = new SequenceEmailStats { Recipients = 100, OpenRate = 80, ClickRate = 40 } },
                new SequenceEmail { Stats = new SequenceEmailStats { Recipients = 900, OpenRate = 20, ClickRate = 5 } })
        };
        var writer = new StringWriter();
        Console.SetOut(writer);

        var result = await SequenceCommands.HandleStats(["42"], mockClient);

        result.Should().Be(0);
        writer.ToString().Should().Contain("Average Open Rate: 26.00%");
        writer.ToString().Should().Contain("Average Click Rate: 8.50%");
    }

    [Fact]
    public async Task HandleStats_Should_Render_Delivery_Status_From_Active_And_Hold_Separately()
    {
        var mockClient = new MockKitApiClient
        {
            GetSequenceAsyncFunc = (_, _) => Task.FromResult<Sequence?>(new Sequence
            {
                Id = 42,
                Name = "Welcome",
                Active = false,
                Hold = false
            }),
            GetAllSequenceEmailsAsyncFunc = (_, _, _, _) => EmptyEmails()
        };
        var writer = new StringWriter();
        Console.SetOut(writer);

        var result = await SequenceCommands.HandleStats(["42"], mockClient);

        result.Should().Be(0);
        var output = writer.ToString();
        output.Should().Contain("Status: Inactive");
        output.Should().Contain("On Hold: No");
        output.Should().NotContain("Status: Active");
    }

    private static async IAsyncEnumerable<SequenceEmail> EmptyEmails()
    {
        await Task.CompletedTask;
        yield break;
    }

    private static async IAsyncEnumerable<SequenceEmail> ReturnEmails(params SequenceEmail[] emails)
    {
        foreach (var email in emails)
        {
            yield return email;
        }

        await Task.CompletedTask;
    }
}
