using FluentAssertions;
using KitCLI.Commands;
using KitCLI.Models;
using KitCLI.Tests.Mocks;

namespace KitCLI.Tests.Commands;

[Collection("Console Output Tests")]
public class SequenceEmailLifecycleCommandTests : IDisposable
{
    private readonly TextWriter _originalOut = Console.Out;

    public void Dispose()
    {
        Console.SetOut(_originalOut);
    }

    // ---- publish / unpublish ---------------------------------------------------------------

    [Fact]
    public async Task Publish_DryRun_Should_Not_Write()
    {
        var putCount = 0;
        var mock = PublishMock(MakeEmail(7, 42, published: false, position: 3), () => putCount++);
        var writer = new StringWriter();
        Console.SetOut(writer);

        var result = await SequenceCommands.HandleEmailPublish(["42", "7"], mock);

        result.Should().Be(0);
        putCount.Should().Be(0);
        var output = writer.ToString();
        output.Should().Contain("DRY RUN");
        output.Should().Contain("published: false -> true");
    }

    [Fact]
    public async Task Publish_Apply_Without_Confirm_Should_Fail_With_No_Write()
    {
        var putCount = 0;
        var mock = PublishMock(MakeEmail(7, 42, published: false, position: 3), () => putCount++);
        var writer = new StringWriter();
        Console.SetOut(writer);

        var result = await SequenceCommands.HandleEmailPublish(["42", "7", "--apply"], mock);

        result.Should().Be(1);
        putCount.Should().Be(0);
        writer.ToString().Should().Contain("--confirm-publish");
    }

    [Fact]
    public async Task Publish_Apply_Should_Set_Published_And_Verify()
    {
        var putCount = 0;
        var mock = PublishMock(MakeEmail(7, 42, published: false, position: 3), () => putCount++);
        var writer = new StringWriter();
        Console.SetOut(writer);

        var result = await SequenceCommands.HandleEmailPublish(
            ["42", "7", "--apply", "--confirm-publish"], mock);

        result.Should().Be(0);
        putCount.Should().Be(1);
        writer.ToString().Should().Contain("Applied and verified");
    }

    [Fact]
    public async Task Publish_Position_Zero_Requires_Extra_Confirmation()
    {
        var putCount = 0;
        var mock = PublishMock(MakeEmail(7, 42, published: false, position: 0), () => putCount++);
        var writer = new StringWriter();
        Console.SetOut(writer);

        var result = await SequenceCommands.HandleEmailPublish(
            ["42", "7", "--apply", "--confirm-publish"], mock);

        result.Should().Be(1);
        putCount.Should().Be(0);
        writer.ToString().Should().Contain("--confirm-position-zero");
    }

    [Fact]
    public async Task Publish_Position_Zero_With_Extra_Confirmation_Writes()
    {
        var putCount = 0;
        var mock = PublishMock(MakeEmail(7, 42, published: false, position: 0), () => putCount++);
        Console.SetOut(new StringWriter());

        var result = await SequenceCommands.HandleEmailPublish(
            ["42", "7", "--apply", "--confirm-publish", "--confirm-position-zero"], mock);

        result.Should().Be(0);
        putCount.Should().Be(1);
    }

    [Fact]
    public async Task Publish_Should_NoOp_When_Already_Published()
    {
        var putCount = 0;
        var mock = PublishMock(MakeEmail(7, 42, published: true, position: 3), () => putCount++);
        var writer = new StringWriter();
        Console.SetOut(writer);

        var result = await SequenceCommands.HandleEmailPublish(
            ["42", "7", "--apply", "--confirm-publish"], mock);

        result.Should().Be(0);
        putCount.Should().Be(0);
        writer.ToString().Should().Contain("already published");
    }

    [Fact]
    public async Task Publish_Should_Fail_Verification_When_Protected_Field_Changes()
    {
        var email = MakeEmail(7, 42, published: false, position: 3);
        var mock = new MockKitApiClient
        {
            GetSequenceEmailAsyncFunc = (_, _, _) => Task.FromResult<SequenceEmail?>(Clone(email)),
            SetSequenceEmailPublishedAsyncFunc = (_, _, pub, _) =>
            {
                email.Published = pub;
                email.Subject = "server mangled the subject"; // unexpected protected-field drift
                return Task.FromResult<SequenceEmail?>(Clone(email));
            }
        };
        var writer = new StringWriter();
        Console.SetOut(writer);

        var result = await SequenceCommands.HandleEmailPublish(
            ["42", "7", "--apply", "--confirm-publish"], mock);

        result.Should().Be(1);
        writer.ToString().Should().Contain("subject changed unexpectedly");
    }

    [Fact]
    public async Task Unpublish_Apply_Should_Set_Unpublished()
    {
        var putCount = 0;
        var mock = PublishMock(MakeEmail(7, 42, published: true, position: 3), () => putCount++);
        var writer = new StringWriter();
        Console.SetOut(writer);

        var result = await SequenceCommands.HandleEmailUnpublish(
            ["42", "7", "--apply", "--confirm-unpublish"], mock);

        result.Should().Be(0);
        putCount.Should().Be(1);
        writer.ToString().Should().Contain("published: true -> false");
    }

    // ---- reorder ---------------------------------------------------------------------------

    [Fact]
    public async Task Reorder_Should_Reject_Non_Permutation()
    {
        var mock = ReorderMock(new[] { MakeEmail(1, 42, position: 0), MakeEmail(2, 42, position: 1) });
        var writer = new StringWriter();
        Console.SetOut(writer);

        // 99 is not in the sequence; 2 is missing.
        var result = await SequenceCommands.HandleEmailReorder(["42", "--order", "1,99"], mock);

        result.Should().Be(1);
        var output = writer.ToString();
        output.Should().Contain("permutation");
    }

    [Fact]
    public async Task Reorder_DryRun_Should_Show_Moves_Without_Writing()
    {
        var setCount = 0;
        var mock = ReorderMock(
            new[] { MakeEmail(1, 42, position: 0), MakeEmail(2, 42, position: 1), MakeEmail(3, 42, position: 2) },
            () => setCount++);
        var writer = new StringWriter();
        Console.SetOut(writer);

        var result = await SequenceCommands.HandleEmailReorder(["42", "--order", "3,1,2"], mock);

        result.Should().Be(0);
        setCount.Should().Be(0);
        writer.ToString().Should().Contain("DRY RUN");
    }

    [Fact]
    public async Task Reorder_Apply_Without_Confirm_Should_Fail()
    {
        var setCount = 0;
        var mock = ReorderMock(
            new[] { MakeEmail(1, 42, position: 0), MakeEmail(2, 42, position: 1) },
            () => setCount++);
        var writer = new StringWriter();
        Console.SetOut(writer);

        var result = await SequenceCommands.HandleEmailReorder(["42", "--order", "2,1", "--apply"], mock);

        result.Should().Be(1);
        setCount.Should().Be(0);
        writer.ToString().Should().Contain("--confirm-reorder");
    }

    [Fact]
    public async Task Reorder_Should_NoOp_When_Already_In_Order()
    {
        var setCount = 0;
        var mock = ReorderMock(
            new[] { MakeEmail(1, 42, position: 0), MakeEmail(2, 42, position: 1) },
            () => setCount++);
        var writer = new StringWriter();
        Console.SetOut(writer);

        var result = await SequenceCommands.HandleEmailReorder(
            ["42", "--order", "1,2", "--apply", "--confirm-reorder"], mock);

        result.Should().Be(0);
        setCount.Should().Be(0);
        writer.ToString().Should().Contain("already in the requested order");
    }

    [Fact]
    public async Task Reorder_Apply_Should_Move_And_Verify_Final_Order()
    {
        var setCount = 0;
        var mock = ReorderMock(
            new[] { MakeEmail(1, 42, position: 0), MakeEmail(2, 42, position: 1), MakeEmail(3, 42, position: 2) },
            () => setCount++);
        var writer = new StringWriter();
        Console.SetOut(writer);

        var result = await SequenceCommands.HandleEmailReorder(
            ["42", "--order", "3,1,2", "--apply", "--confirm-reorder"], mock);

        result.Should().Be(0);
        setCount.Should().BeGreaterThan(0);
        writer.ToString().Should().Contain("Reordered and verified");
    }

    [Fact]
    public async Task Reorder_Should_Fail_When_Final_Order_Does_Not_Match()
    {
        // A mock whose position sets are ignored, so the final order never matches the target.
        var emails = new[] { MakeEmail(1, 42, position: 0), MakeEmail(2, 42, position: 1) };
        var mock = new MockKitApiClient
        {
            GetAllSequenceEmailsAsyncFunc = (_, _, _, _) => ReturnEmails(emails.Select(Clone).ToArray()),
            SetSequenceEmailPositionAsyncFunc = (_, eid, _, _) =>
                Task.FromResult<SequenceEmail?>(Clone(emails.First(e => e.Id == eid))) // ignores the new position
        };
        var writer = new StringWriter();
        Console.SetOut(writer);

        var result = await SequenceCommands.HandleEmailReorder(
            ["42", "--order", "2,1", "--apply", "--confirm-reorder"], mock);

        result.Should().Be(1);
        writer.ToString().Should().Contain("Verification failed");
    }

    [Fact]
    public async Task Reorder_Should_Abort_When_Order_Drifts_Between_Preview_And_Apply()
    {
        var listCalls = 0;
        var mock = new MockKitApiClient
        {
            GetAllSequenceEmailsAsyncFunc = (_, _, _, _) =>
            {
                listCalls++;
                // First read: [1@0, 2@1]. Recheck read: order drifted to [2@0, 1@1].
                return listCalls == 1
                    ? ReturnEmails(MakeEmail(1, 42, position: 0), MakeEmail(2, 42, position: 1))
                    : ReturnEmails(MakeEmail(2, 42, position: 0), MakeEmail(1, 42, position: 1));
            },
            SetSequenceEmailPositionAsyncFunc = (_, _, _, _) => Task.FromResult<SequenceEmail?>(null)
        };
        var writer = new StringWriter();
        Console.SetOut(writer);

        var result = await SequenceCommands.HandleEmailReorder(
            ["42", "--order", "2,1", "--apply", "--confirm-reorder"], mock);

        result.Should().Be(1);
        writer.ToString().Should().Contain("changed between preview and apply");
    }

    // ---- helpers ---------------------------------------------------------------------------

    private static SequenceEmail MakeEmail(long id, long sequenceId, bool published = true, int position = 1, string subject = "Subject") => new()
    {
        Id = id,
        SequenceId = sequenceId,
        Subject = subject,
        Content = "<p>Body</p>",
        EmailAddress = "team@example.com",
        Published = published,
        Position = position,
        DelayValue = 3,
        DelayUnit = "days",
        SendDays = ["monday"]
    };

    private static SequenceEmail Clone(SequenceEmail e) => new()
    {
        Id = e.Id,
        SequenceId = e.SequenceId,
        Subject = e.Subject,
        PreviewText = e.PreviewText,
        EmailAddress = e.EmailAddress,
        EmailTemplateId = e.EmailTemplateId,
        Published = e.Published,
        Position = e.Position,
        DelayValue = e.DelayValue,
        DelayUnit = e.DelayUnit,
        SendDays = e.SendDays,
        Content = e.Content
    };

    private static MockKitApiClient PublishMock(SequenceEmail state, Action onPut) => new()
    {
        GetSequenceEmailAsyncFunc = (_, _, _) => Task.FromResult<SequenceEmail?>(Clone(state)),
        SetSequenceEmailPublishedAsyncFunc = (_, _, pub, _) =>
        {
            onPut();
            state.Published = pub;
            return Task.FromResult<SequenceEmail?>(Clone(state));
        }
    };

    // A reorder mock that applies position sets literally (final positions become the target indices).
    private static MockKitApiClient ReorderMock(SequenceEmail[] emails, Action? onSet = null)
    {
        var store = emails.ToList();
        return new MockKitApiClient
        {
            GetAllSequenceEmailsAsyncFunc = (_, _, _, _) => ReturnEmails(store.Select(Clone).ToArray()),
            SetSequenceEmailPositionAsyncFunc = (_, eid, pos, _) =>
            {
                onSet?.Invoke();
                var e = store.First(x => x.Id == eid);
                e.Position = pos;
                return Task.FromResult<SequenceEmail?>(Clone(e));
            }
        };
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
