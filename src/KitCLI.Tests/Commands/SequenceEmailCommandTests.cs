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
    public async Task HandleEmails_Should_Render_All_Pages_In_Table_Output()
    {
        var pagesRequested = 0;
        var mockClient = new MockKitApiClient
        {
            GetSequenceEmailsAsyncFunc = (_, _, after, _, _, _) =>
            {
                pagesRequested++;
                return Task.FromResult(after is null
                    ? new PaginatedResponse<SequenceEmail>
                    {
                        Data =
                        [
                            new SequenceEmail { Id = 1, Position = 1, Subject = "Welcome" },
                            new SequenceEmail { Id = 2, Position = 2, Subject = "Follow up" }
                        ],
                        Pagination = new PaginationInfo { HasNextPage = true, EndCursor = "page-2" }
                    }
                    : new PaginatedResponse<SequenceEmail>
                    {
                        Data = [new SequenceEmail { Id = 3, Position = 3, Subject = "Last chance" }],
                        Pagination = new PaginationInfo { HasNextPage = false }
                    });
            }
        };
        var writer = new StringWriter();
        Console.SetOut(writer);

        var result = await SequenceCommands.HandleEmails(["42"], mockClient);

        result.Should().Be(0);
        pagesRequested.Should().Be(2);
        var output = writer.ToString().ReplaceLineEndings("\n");
        output.Should().Contain("Found 3 emails in sequence");
        output.Should().Contain("Welcome");
        output.Should().Contain("Follow up");
        output.Should().Contain("Last chance");
    }

    [Fact]
    public async Task HandleEmails_Should_Render_All_Pages_In_Json_Output()
    {
        var pagesRequested = 0;
        var mockClient = new MockKitApiClient
        {
            GetSequenceEmailsAsyncFunc = (_, _, after, _, _, _) =>
            {
                pagesRequested++;
                return Task.FromResult(after is null
                    ? new PaginatedResponse<SequenceEmail>
                    {
                        Data = [new SequenceEmail { Id = 1, Position = 1, Subject = "Welcome" }],
                        Pagination = new PaginationInfo { HasNextPage = true, EndCursor = "page-2" }
                    }
                    : new PaginatedResponse<SequenceEmail>
                    {
                        Data =
                        [
                            new SequenceEmail { Id = 2, Position = 2, Subject = "Follow up" },
                            new SequenceEmail { Id = 3, Position = 3, Subject = "Last chance" }
                        ],
                        Pagination = new PaginationInfo { HasNextPage = false }
                    });
            }
        };
        var writer = new StringWriter();
        Console.SetOut(writer);

        var result = await SequenceCommands.HandleEmails(["42", "--format", "json"], mockClient);

        result.Should().Be(0);
        pagesRequested.Should().Be(2);
        var output = writer.ToString();
        output.Should().Contain("Found 3 emails in sequence");
        output.Should().Contain("\"id\": 1");
        output.Should().Contain("\"id\": 2");
        output.Should().Contain("\"id\": 3");
    }

    [Fact]
    public async Task HandleEmails_Should_Delimit_Content_In_Table_Output()
    {
        var mockClient = new MockKitApiClient
        {
            GetAllSequenceEmailsAsyncFunc = (_, includeContent, _, _) =>
            {
                includeContent.Should().BeTrue();
                return ReturnEmails(new SequenceEmail
                {
                    Id = 7,
                    Position = 1,
                    Subject = "Welcome",
                    Content = "<p>Hello</p>\n<p>World</p>"
                });
            }
        };
        var writer = new StringWriter();
        Console.SetOut(writer);

        var result = await SequenceCommands.HandleEmails(["42", "--include-content"], mockClient);

        result.Should().Be(0);
        writer.ToString().ReplaceLineEndings("\n")
            .Should().Contain("   Content:\n   ┌─\n   │ <p>Hello</p>\n   │ <p>World</p>\n   └─");
    }

    [Fact]
    public async Task HandleEmails_Should_Safely_Render_Kit_Control_Sequences_In_All_Human_Output()
    {
        const string subject = "Subject\u001b[31mred\u001b]0;bel-title\abel\u001b]0;st-title\u001b\\st\u001b]0;c1-title\u009cc1\u0001\r\nnext";
        const string sender = "sender\u001b[32m\u001b]0;bel-address\asafe\u001b]0;st-address\u001b\\mail\u001b]0;c1-address\u009c@example.com\u0001\r\ninvalid";
        const string content = "Body\r\nline\u001b[31mred\u001b]0;bel-title\avisible\u001b]0;st-title\u001b\\also\u001b]0;c1-title\u009cdone\u0001\r\nlast\u009b31m";
        var mockClient = new MockKitApiClient
        {
            GetAllSequenceEmailsAsyncFunc = (_, _, _, _) => ReturnEmails(new SequenceEmail
            {
                Id = 7,
                Position = 1,
                Subject = subject,
                EmailAddress = sender,
                Content = content
            })
        };
        var writer = new StringWriter();
        Console.SetOut(writer);

        var result = await SequenceCommands.HandleEmails(["42", "--include-content"], mockClient);

        result.Should().Be(0);
        var output = writer.ToString().ReplaceLineEndings("\n");
        output.Should().Contain("1. Subjectredbelstc1next");
        output.Should().Contain("Sender: sendersafemail@example.cominvalid");
        output.Should().Contain("Content:\n   ┌─\n   │ Body\n   │ lineredvisiblealsodone\n   │ last\n   └─");
        output.Should().NotContain("\u001b");
        output.Should().NotContain("\u009b");
        output.Should().NotContain("\u009d");
        output.Should().NotContain("\u009c");
        output.Should().NotContain("\r");
        output.Should().NotContain("\u0001");
        output.Should().NotContain("bel-title");
        output.Should().NotContain("st-title");
        output.Should().NotContain("c1-title");
    }

    [Fact]
    public async Task Sequence_Human_Output_Should_Remove_Terminal_Control_Sequences_From_All_Api_Controlled_Fields()
    {
        const string malicious = "shown\u001b[31mred\u001b]0;osc-bel\ashown\u001b]0;osc-st\u001b\\shown\u009dosc-c1\u009cshown\u0001\rbare-cr";
        var sequence = new Sequence { Id = 42, Name = malicious, CreatedAt = DateTimeOffset.UnixEpoch };
        var subscriber = new SequenceSubscriber
        {
            EmailAddress = malicious,
            State = malicious,
            AddedAt = DateTimeOffset.UnixEpoch
        };
        var email = new SequenceEmail
        {
            Id = 7,
            Position = 1,
            Subject = malicious,
            EmailAddress = malicious,
            DelayUnit = malicious,
            SendDays = [malicious]
        };
        var mockClient = new MockKitApiClient
        {
            GetSequencesAsyncFunc = (_, _, _) => Task.FromResult(new PaginatedResponse<Sequence> { Data = [sequence] }),
            GetSequenceAsyncFunc = (_, _) => Task.FromResult<Sequence?>(sequence),
            GetSequenceSubscribersAsyncFunc = (_, _, _, _, _) => Task.FromResult(
                new PaginatedResponse<SequenceSubscriber> { Data = [subscriber] }),
            GetAllSequenceEmailsAsyncFunc = (_, _, _, _) => ReturnEmails(email)
        };

        var listOutput = await CaptureOutput(() => SequenceCommands.HandleList(["--format", "table"], mockClient));
        var getOutput = await CaptureOutput(() => SequenceCommands.HandleGet(["42", "--format", "table"], mockClient));
        var subscriberOutput = await CaptureOutput(() => SequenceCommands.HandleSubscribers(["42", "--format", "table"], mockClient));
        var emailOutput = await CaptureOutput(() => SequenceCommands.HandleEmails(["42"], mockClient));

        foreach (var output in new[] { listOutput, getOutput, subscriberOutput, emailOutput })
        {
            AssertContainsNoTerminalControls(output);
            output.Should().Contain("shownshown");
            output.Should().NotContain("osc-bel");
            output.Should().NotContain("osc-st");
            output.Should().NotContain("osc-c1");
        }
    }

    [Fact]
    public async Task HandleList_Should_Render_Api_Supplied_Subscriber_And_Email_Counts()
    {
        var mockClient = new MockKitApiClient
        {
            GetSequencesAsyncFunc = (_, _, _) => Task.FromResult(new PaginatedResponse<Sequence>
            {
                Data = [new Sequence
                {
                    Id = 42,
                    Name = "Welcome",
                    SubscriberCount = 1_234,
                    EmailCount = 12,
                    CreatedAt = DateTimeOffset.UnixEpoch
                }]
            })
        };

        var output = await CaptureOutput(() => SequenceCommands.HandleList([], mockClient));

        output.Should().Contain("Subscribers");
        output.Should().Contain("Emails");
        output.Should().Contain("1,234");
        output.Should().Contain("12");
        output.Should().Contain("email performance details");
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
    public async Task HandleEmailGet_Should_Safely_Render_Subject_In_Progress_Output()
    {
        var mockClient = new MockKitApiClient
        {
            GetSequenceEmailAsyncFunc = (_, _, _) => Task.FromResult<SequenceEmail?>(
                new SequenceEmail { Id = 7, Subject = "Welcome\u001b]0;forged\u001b\\\r\nnext" })
        };
        var writer = new StringWriter();
        Console.SetOut(writer);

        var result = await SequenceCommands.HandleEmailGet(["42", "7", "--format", "table"], mockClient);

        result.Should().Be(0);
        var output = writer.ToString().ReplaceLineEndings("\n");
        output.Should().Contain("Found email: Welcomenext");
        output.Should().NotContain("\u001b");
        output.Should().NotContain("forged");
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

    [Fact]
    public async Task HandleEmailUpdate_Subject_DryRun_Should_Not_Call_Update()
    {
        var putCount = 0;
        var mockClient = SubjectMock("Old subject", out _, () => putCount++);
        var writer = new StringWriter();
        Console.SetOut(writer);

        var result = await SequenceCommands.HandleEmailUpdate(["42", "7", "--subject", "New subject"], mockClient);

        result.Should().Be(0);
        putCount.Should().Be(0);
        var output = writer.ToString();
        output.Should().Contain("DRY RUN");
        output.Should().Contain("Field: subject");
    }

    [Fact]
    public async Task HandleEmailUpdate_Apply_Without_Confirm_Should_Fail_With_No_Get_Or_Put()
    {
        var getCount = 0;
        var putCount = 0;
        var mockClient = new MockKitApiClient
        {
            GetSequenceEmailAsyncFunc = (_, _, _) => { getCount++; return Task.FromResult<SequenceEmail?>(NewEmail()); },
            UpdateSequenceEmailAsyncFunc = (_, _, _, _) => { putCount++; return Task.FromResult<SequenceEmail?>(NewEmail()); }
        };
        var writer = new StringWriter();
        Console.SetOut(writer);

        var result = await SequenceCommands.HandleEmailUpdate(["42", "7", "--subject", "New subject", "--apply"], mockClient);

        result.Should().Be(1);
        getCount.Should().Be(0);
        putCount.Should().Be(0);
        writer.ToString().Should().Contain("--confirm-field-scope");
    }

    [Fact]
    public async Task HandleEmailUpdate_Subject_Apply_Should_Put_Once_And_Verify()
    {
        var putCount = 0;
        var mockClient = SubjectMock("Old subject", out _, () => putCount++);
        var writer = new StringWriter();
        Console.SetOut(writer);

        var result = await SequenceCommands.HandleEmailUpdate(
            ["42", "7", "--subject", "New subject", "--apply", "--confirm-field-scope"], mockClient);

        result.Should().Be(0);
        putCount.Should().Be(1);
        writer.ToString().Should().Contain("Applied and verified");
    }

    [Fact]
    public async Task HandleEmailUpdate_Should_Detect_No_Op_And_Skip_Put()
    {
        var putCount = 0;
        var mockClient = SubjectMock("Same subject", out _, () => putCount++);
        var writer = new StringWriter();
        Console.SetOut(writer);

        var result = await SequenceCommands.HandleEmailUpdate(
            ["42", "7", "--subject", "Same subject", "--apply", "--confirm-field-scope"], mockClient);

        result.Should().Be(0);
        putCount.Should().Be(0);
        writer.ToString().Should().Contain("No change needed");
    }

    public static IEnumerable<object[]> InvalidUpdateArguments =>
    [
        [new[] { "42", "7" }], // neither field
        [new[] { "42", "7", "--subject", "x", "--content-file", "y" }], // both fields
        [new[] { "42", "7", "--subject", "   " }], // whitespace subject
        [new[] { "42", "7", "--subject", "x", "--bogus" }], // unknown flag
        [new[] { "invalid", "7", "--subject", "x" }], // bad sequence id
        [new[] { "42", "notnum", "--subject", "x" }] // bad email id
    ];

    [Theory]
    [MemberData(nameof(InvalidUpdateArguments))]
    public async Task HandleEmailUpdate_Should_Reject_Invalid_Arguments(string[] args)
    {
        var putCount = 0;
        var mockClient = new MockKitApiClient
        {
            GetSequenceEmailAsyncFunc = (_, _, _) => Task.FromResult<SequenceEmail?>(NewEmail()),
            UpdateSequenceEmailAsyncFunc = (_, _, _, _) => { putCount++; return Task.FromResult<SequenceEmail?>(NewEmail()); }
        };
        Console.SetOut(new StringWriter());

        var result = await SequenceCommands.HandleEmailUpdate(args, mockClient);

        result.Should().Be(1);
        putCount.Should().Be(0);
    }

    [Fact]
    public async Task HandleEmailUpdate_Should_Reject_Missing_Content_File()
    {
        var mockClient = new MockKitApiClient();
        Console.SetOut(new StringWriter());

        var result = await SequenceCommands.HandleEmailUpdate(
            ["42", "7", "--content-file", "/nonexistent/path/body.html"], mockClient);

        result.Should().Be(1);
    }

    [Fact]
    public async Task HandleEmailUpdate_Should_Fail_Precondition_On_Expect_Subject_Mismatch()
    {
        var putCount = 0;
        var mockClient = SubjectMock("Actual subject", out _, () => putCount++);
        var writer = new StringWriter();
        Console.SetOut(writer);

        var result = await SequenceCommands.HandleEmailUpdate(
            ["42", "7", "--subject", "New subject", "--expect-subject", "Wrong old", "--apply", "--confirm-field-scope"],
            mockClient);

        result.Should().Be(1);
        putCount.Should().Be(0);
        writer.ToString().Should().Contain("Precondition failed");
    }

    [Fact]
    public async Task HandleEmailUpdate_Should_Fail_Verification_When_Protected_Field_Changes()
    {
        // The server applies the subject but also drifts a protected field (published); the follow-up
        // GET must reveal the drift, so the command fails and never writes again.
        var putCount = 0;
        var baseline = NewEmail();
        baseline.Subject = "Old subject";
        var mockClient = new MockKitApiClient
        {
            GetSequenceEmailAsyncFunc = (_, _, _) => Task.FromResult<SequenceEmail?>(Clone(baseline)),
            UpdateSequenceEmailAsyncFunc = (_, _, req, _) =>
            {
                putCount++;
                baseline.Subject = req.Subject ?? baseline.Subject; // intended change applied
                baseline.Published = !baseline.Published;           // unexpected protected-field drift
                return Task.FromResult<SequenceEmail?>(Clone(baseline));
            }
        };
        var writer = new StringWriter();
        Console.SetOut(writer);

        var result = await SequenceCommands.HandleEmailUpdate(
            ["42", "7", "--subject", "New subject", "--apply", "--confirm-field-scope"], mockClient);

        result.Should().Be(1);
        putCount.Should().Be(1); // exactly one PUT, no compensating write
        writer.ToString().Should().Contain("Verification failed");
    }

    [Fact]
    public async Task HandleEmailUpdate_Content_Op_Should_Report_Hash_And_Not_Dump_Body()
    {
        const string body = "<p>Brand new body with SECRET_MARKER</p>";
        var file = Path.Combine(Path.GetTempPath(), $"kit-cli-body-{Guid.NewGuid():N}.html");
        await File.WriteAllTextAsync(file, body);
        try
        {
            var baseline = NewEmail();
            baseline.Content = "<p>Old body</p>";
            var mockClient = new MockKitApiClient
            {
                GetSequenceEmailAsyncFunc = (_, _, _) => Task.FromResult<SequenceEmail?>(Clone(baseline)),
                UpdateSequenceEmailAsyncFunc = (_, _, req, _) =>
                {
                    // Mutate the baseline so the verification GET reflects the applied change.
                    baseline.Content = req.Content ?? baseline.Content;
                    return Task.FromResult<SequenceEmail?>(Clone(baseline));
                }
            };
            var writer = new StringWriter();
            Console.SetOut(writer);

            var result = await SequenceCommands.HandleEmailUpdate(
                ["42", "7", "--content-file", file, "--apply", "--confirm-field-scope"], mockClient);

            result.Should().Be(0);
            var output = writer.ToString();
            output.Should().Contain("sha256");
            output.Should().Contain("bytes");
            output.Should().NotContain("SECRET_MARKER");
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public async Task HandleEmailUpdate_NoOp_Should_Emit_Valid_Json_When_Requested()
    {
        var mockClient = SubjectMock("Same subject", out _, () => { });
        var writer = new StringWriter();
        Console.SetOut(writer);

        var result = await SequenceCommands.HandleEmailUpdate(
            ["42", "7", "--subject", "Same subject", "--format", "json"], mockClient);

        result.Should().Be(0);
        using var document = System.Text.Json.JsonDocument.Parse(writer.ToString());
        document.RootElement.GetProperty("status").GetString().Should().Be("no-change");
        document.RootElement.GetProperty("changed").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task HandleEmailUpdate_Should_Report_Unknown_When_Verification_Get_Fails_After_Put()
    {
        // The PUT succeeds, then the follow-up verification GET fails. The command must NOT claim the
        // write failed — it must surface an UNKNOWN outcome so the operator does not blindly re-apply.
        var getCalls = 0;
        var baseline = NewEmail();
        baseline.Subject = "Old subject";
        var mockClient = new MockKitApiClient
        {
            GetSequenceEmailAsyncFunc = (_, _, _) =>
            {
                getCalls++;
                if (getCalls == 1)
                {
                    return Task.FromResult<SequenceEmail?>(Clone(baseline));
                }

                throw new HttpRequestException("network blip during verification");
            },
            UpdateSequenceEmailAsyncFunc = (_, _, req, _) =>
            {
                baseline.Subject = req.Subject ?? baseline.Subject;
                return Task.FromResult<SequenceEmail?>(Clone(baseline));
            }
        };
        var writer = new StringWriter();
        Console.SetOut(writer);

        var result = await SequenceCommands.HandleEmailUpdate(
            ["42", "7", "--subject", "New subject", "--apply", "--confirm-field-scope"], mockClient);

        result.Should().Be(1);
        var output = writer.ToString();
        output.Should().Contain("UNKNOWN");
        output.Should().NotContain("Update failed");
    }

    private static SequenceEmail NewEmail() => new()
    {
        Id = 7,
        SequenceId = 42,
        Subject = "Old subject",
        Content = "<p>Body</p>",
        EmailAddress = "team@example.com",
        Published = true,
        Position = 1,
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

    // Builds a mock whose stored email starts with <currentSubject>; PUT mutates the stored subject
    // and both GET and PUT return independent clones so preflight/verify snapshots stay separate.
    private static MockKitApiClient SubjectMock(string currentSubject, out SequenceEmail stored, Action onPut)
    {
        var state = NewEmail();
        state.Subject = currentSubject;
        stored = state;
        return new MockKitApiClient
        {
            GetSequenceEmailAsyncFunc = (_, _, _) => Task.FromResult<SequenceEmail?>(Clone(state)),
            UpdateSequenceEmailAsyncFunc = (_, _, req, _) =>
            {
                onPut();
                if (req.Subject != null)
                {
                    state.Subject = req.Subject;
                }

                if (req.Content != null)
                {
                    state.Content = req.Content;
                }

                return Task.FromResult<SequenceEmail?>(Clone(state));
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

    private static async Task<string> CaptureOutput(Func<Task<int>> command)
    {
        var writer = new StringWriter();
        Console.SetOut(writer);

        var result = await command();

        result.Should().Be(0);
        return writer.ToString();
    }

    private static void AssertContainsNoTerminalControls(string output)
    {
        output.Should().NotContain("\u001b");
        output.Should().NotContain("\u009b");
        output.Should().NotContain("\u009d");
        output.Should().NotContain("\u009c");
        output.Should().NotContain("\u0001");
        output.ReplaceLineEndings("\n").Should().NotContain("\r");
    }
}
