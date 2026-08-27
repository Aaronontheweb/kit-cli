using FluentAssertions;
using KitCLI.Commands;
using KitCLI.Models;
using KitCLI.Tests.Mocks;

namespace KitCLI.Tests.Commands;

[Collection("Console Output Tests")]
public class SequenceEmailCommandTests : IDisposable
{
    private readonly TextWriter _originalOut = Console.Out;
    private readonly TextWriter _originalError = Console.Error;

    public void Dispose()
    {
        Console.SetOut(_originalOut);
        Console.SetError(_originalError);
    }

    [Fact]
    public async Task HandleEmails_Should_Forward_Include_Flags_And_Format_Json()
    {
        bool includeContent = false;
        bool includeStats = false;
        var mockClient = new MockKitApiClient
        {
            GetSequenceEmailsAsyncFunc = (_, _, _, content, stats, _) =>
            {
                includeContent = content;
                includeStats = stats;
                return Task.FromResult(new PaginatedResponse<SequenceEmail>
                {
                    Data = [new SequenceEmail { Id = 7, Subject = "Welcome" }],
                    Pagination = new PaginationInfo { HasNextPage = false }
                });
            }
        };
        var writer = new StringWriter();
        Console.SetOut(writer);

        var result = await SequenceCommands.HandleEmails(
            ["42", "--include-content", "--include-stats", "--format", "json"],
            mockClient);

        result.Should().Be(0);
        includeContent.Should().BeTrue();
        includeStats.Should().BeTrue();
        writer.ToString().Should().Contain("\"subject\": \"Welcome\"");
    }

    [Fact]
    public async Task HandleEmailGet_Should_Render_Json_For_Found_Email()
    {
        var mockClient = new MockKitApiClient
        {
            GetSequenceEmailAsyncFunc = (_, _, _) => Task.FromResult<SequenceEmail?>(
                new SequenceEmail { Id = 7, Subject = "Welcome" })
        };
        var writer = new StringWriter();
        Console.SetOut(writer);

        var result = await SequenceCommands.HandleEmailGet(["42", "7", "--format", "json"], mockClient);

        result.Should().Be(0);
        writer.ToString().Should().Contain("\"subject\": \"Welcome\"");
    }

    [Fact]
    public async Task HandleEmailGet_Should_Reject_Invalid_Ids()
    {
        var writer = new StringWriter();
        Console.SetOut(writer);

        var result = await SequenceCommands.HandleEmailGet(["invalid", "7"], new MockKitApiClient());

        result.Should().Be(1);
        writer.ToString().Should().Contain("Invalid sequence ID");
    }

    [Fact]
    public async Task HandleEmailGet_Should_Return_Error_When_Email_Is_Not_Found()
    {
        var mockClient = new MockKitApiClient
        {
            GetSequenceEmailAsyncFunc = (_, _, _) => Task.FromResult<SequenceEmail?>(null)
        };
        var writer = new StringWriter();
        Console.SetOut(writer);

        var result = await SequenceCommands.HandleEmailGet(["42", "7"], mockClient);

        result.Should().Be(1);
        writer.ToString().Should().Contain("Email not found: 7 in sequence 42");
    }
}
