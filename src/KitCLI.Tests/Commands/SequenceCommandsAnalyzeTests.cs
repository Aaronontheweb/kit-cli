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
            CompletedSubscribers = 40,
            CancelledSubscribers = 0,
            AverageOpenRate = 40.0,
            AverageClickRate = 10.0,
            CompletionRate = 0.4,
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
        writer.ToString().Should().Contain("Active: 0 (0.0 %)");
        writer.ToString().Should().NotContain("NaN");
        writer.ToString().Should().NotContain("Infinity");
    }
}
