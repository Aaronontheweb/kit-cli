using System.Text.Json;
using FluentAssertions;
using KitCLI.Commands;
using KitCLI.Models;
using KitCLI.Tests.Mocks;

namespace KitCLI.Tests.Commands;

[Collection("Console Output Tests")]
public class SequenceEmailBatchCommandTests : IDisposable
{
    private readonly TextWriter _originalOut = Console.Out;
    private readonly TextWriter _originalError = Console.Error;
    private readonly List<string> _tempFiles = new();
    private readonly List<string> _tempDirs = new();

    public void Dispose()
    {
        Console.SetOut(_originalOut);
        Console.SetError(_originalError);
        foreach (var f in _tempFiles)
        {
            try { if (File.Exists(f)) File.Delete(f); } catch { /* best effort */ }
        }

        foreach (var d in _tempDirs)
        {
            try { if (Directory.Exists(d)) Directory.Delete(d, recursive: true); } catch { /* best effort */ }
        }
    }

    // ---- Dry-run and preflight -------------------------------------------------------------

    [Fact]
    public async Task Batch_DryRun_Should_Preflight_Without_Writing()
    {
        var store = new List<SequenceEmail> { MakeEmail(7, 42, "Old subject") };
        var putCount = 0;
        var mock = MakeMock("Bootcamp 2.0", store, () => putCount++);
        var manifest = WriteManifest(SubjectManifest((42, "Bootcamp 2.0", 7, "New subject", "Old subject")));
        var writer = new StringWriter();
        Console.SetOut(writer);

        var result = await SequenceCommands.HandleEmailUpdateBatch([manifest], mock);

        result.Should().Be(0);
        putCount.Should().Be(0);
        var output = writer.ToString();
        output.Should().Contain("DRY RUN");
        output.Should().Contain("Sequence 42");
    }

    [Fact]
    public async Task Batch_Apply_Without_Confirm_Should_Fail_With_No_Writes()
    {
        var store = new List<SequenceEmail> { MakeEmail(7, 42, "Old subject") };
        var putCount = 0;
        var mock = MakeMock("Bootcamp 2.0", store, () => putCount++);
        var manifest = WriteManifest(SubjectManifest((42, "Bootcamp 2.0", 7, "New subject", "Old subject")));
        var writer = new StringWriter();
        Console.SetOut(writer);

        var result = await SequenceCommands.HandleEmailUpdateBatch([manifest, "--apply"], mock);

        result.Should().Be(1);
        putCount.Should().Be(0);
        writer.ToString().Should().Contain("--confirm-field-scope");
    }

    [Fact]
    public async Task Batch_Apply_Should_Put_One_Field_Per_Row_And_Verify()
    {
        var store = new List<SequenceEmail>
        {
            MakeEmail(7, 42, "Old A"),
            MakeEmail(8, 42, "Old B")
        };
        var putCount = 0;
        var mock = MakeMock("Bootcamp 2.0", store, () => putCount++);
        var manifest = WriteManifest(SubjectManifest(
            (42, "Bootcamp 2.0", 7, "New A", "Old A"),
            (42, "Bootcamp 2.0", 8, "New B", "Old B")));
        var writer = new StringWriter();
        Console.SetOut(writer);

        var result = await SequenceCommands.HandleEmailUpdateBatch(
            [manifest, "--apply", "--confirm-field-scope"], mock);

        result.Should().Be(0);
        putCount.Should().Be(2);
        store.Single(e => e.Id == 7).Subject.Should().Be("New A");
        store.Single(e => e.Id == 8).Subject.Should().Be("New B");
        writer.ToString().Should().Contain("Applied and verified");
    }

    [Fact]
    public async Task Batch_Guard_Mismatch_Should_Abort_Whole_Batch_With_No_Writes()
    {
        var store = new List<SequenceEmail>
        {
            MakeEmail(7, 42, "Old A"),
            MakeEmail(8, 42, "Actual B") // does not match the manifest's expect_subject
        };
        var putCount = 0;
        var mock = MakeMock("Bootcamp 2.0", store, () => putCount++);
        var manifest = WriteManifest(SubjectManifest(
            (42, "Bootcamp 2.0", 7, "New A", "Old A"),
            (42, "Bootcamp 2.0", 8, "New B", "Expected B")));
        var writer = new StringWriter();
        Console.SetOut(writer);

        var result = await SequenceCommands.HandleEmailUpdateBatch(
            [manifest, "--apply", "--confirm-field-scope"], mock);

        result.Should().Be(1);
        putCount.Should().Be(0); // preflight fails => zero writes across the entire batch
        var output = writer.ToString();
        output.Should().Contain("Preflight failed");
        output.Should().Contain("expect_subject mismatch");
    }

    [Fact]
    public async Task Batch_Already_Applied_Row_Should_Be_NoChange_Not_Guard_Failure()
    {
        // Live subject already equals the replacement (e.g. a prior crashed run applied it before its
        // progress was recorded). Even though expect_subject no longer matches live, this must be a
        // no-op — not a preflight abort — so re-runs and resume are idempotent.
        var store = new List<SequenceEmail> { MakeEmail(7, 42, "New subject") };
        var putCount = 0;
        var mock = MakeMock("Bootcamp 2.0", store, () => putCount++);
        var manifest = WriteManifest(SubjectManifest((42, "Bootcamp 2.0", 7, "New subject", "Old subject")));
        var writer = new StringWriter();
        Console.SetOut(writer);

        var result = await SequenceCommands.HandleEmailUpdateBatch(
            [manifest, "--apply", "--confirm-field-scope"], mock);

        result.Should().Be(0);
        putCount.Should().Be(0);
        writer.ToString().Should().Contain("no-change");
    }

    [Fact]
    public async Task Batch_Already_Applied_Row_Should_Still_Abort_On_Position_Drift()
    {
        // The row's content is already at target (live subject == replacement), but the sequence
        // reordered. The publish/position drift must still abort the whole batch.
        var store = new List<SequenceEmail> { MakeEmail(7, 42, "New subject", position: 5) };
        var putCount = 0;
        var mock = MakeMock("Bootcamp 2.0", store, () => putCount++);
        var json = """
        {
          "schema_version": 1,
          "items": [
            { "sequence_id": 42, "expected_sequence_name": "Bootcamp 2.0", "email_id": 7,
              "field": "subject", "replacement": "New subject", "expect_subject": "Old subject",
              "expected_position": 1 }
          ]
        }
        """;
        var manifest = WriteManifest(json);
        var writer = new StringWriter();
        Console.SetOut(writer);

        var result = await SequenceCommands.HandleEmailUpdateBatch([manifest, "--apply", "--confirm-field-scope"], mock);

        result.Should().Be(1);
        putCount.Should().Be(0);
        writer.ToString().Should().Contain("expected_position mismatch");
    }

    [Fact]
    public async Task Batch_Apply_Time_Idempotency_Should_NoOp_When_Value_Already_At_Target()
    {
        // Preflight sees the old value (guard matches, row is 'changed'); between preflight and the
        // PUT the live value drifts to exactly the replacement. Apply-time idempotency must treat it
        // as a verified no-op, not a guard failure that halts the batch.
        var getCalls = 0;
        var putCount = 0;
        var mock = new MockKitApiClient
        {
            GetSequenceAsyncFunc = (id, _) => Task.FromResult<Sequence?>(new Sequence { Id = id, Name = "Bootcamp 2.0" }),
            GetSequenceEmailAsyncFunc = (sid, eid, _) =>
            {
                getCalls++;
                var subject = getCalls == 1 ? "Old subject" : "New subject";
                return Task.FromResult<SequenceEmail?>(MakeEmail(eid, sid, subject));
            },
            UpdateSequenceEmailAsyncFunc = (_, _, _, _) =>
            {
                putCount++;
                return Task.FromResult<SequenceEmail?>(MakeEmail(7, 42, "New subject"));
            }
        };
        var manifest = WriteManifest(SubjectManifest((42, "Bootcamp 2.0", 7, "New subject", "Old subject")));
        Console.SetOut(new StringWriter());

        var result = await SequenceCommands.HandleEmailUpdateBatch([manifest, "--apply", "--confirm-field-scope"], mock);

        result.Should().Be(0);
        putCount.Should().Be(0); // value already at target at apply time -> no PUT, no halt
    }

    [Fact]
    public async Task Batch_Expected_Published_Mismatch_Should_Abort()
    {
        var store = new List<SequenceEmail> { MakeEmail(7, 42, "Old subject", published: true) };
        var mock = MakeMock("Bootcamp 2.0", store);
        // expected_published=false but live is true
        var json = $$"""
        {
          "schema_version": 1,
          "name": "t",
          "items": [
            { "sequence_id": 42, "expected_sequence_name": "Bootcamp 2.0", "email_id": 7,
              "field": "subject", "replacement": "New", "expect_subject": "Old subject",
              "expected_published": false }
          ]
        }
        """;
        var manifest = WriteManifest(json);
        var writer = new StringWriter();
        Console.SetOut(writer);

        var result = await SequenceCommands.HandleEmailUpdateBatch([manifest], mock);

        result.Should().Be(1);
        writer.ToString().Should().Contain("expected_published mismatch");
    }

    [Fact]
    public async Task Batch_Sequence_Name_Mismatch_Should_Abort()
    {
        var store = new List<SequenceEmail> { MakeEmail(7, 42, "Old subject") };
        var mock = MakeMock("Different Name", store);
        var manifest = WriteManifest(SubjectManifest((42, "Bootcamp 2.0", 7, "New", "Old subject")));
        var writer = new StringWriter();
        Console.SetOut(writer);

        var result = await SequenceCommands.HandleEmailUpdateBatch([manifest], mock);

        result.Should().Be(1);
        writer.ToString().Should().Contain("expected_sequence_name mismatch");
    }

    // ---- Verification and error handling ---------------------------------------------------

    [Fact]
    public async Task Batch_ReadBack_Protected_Field_Drift_Should_Fail_Row()
    {
        var email = MakeEmail(7, 42, "Old subject", published: true);
        var store = new List<SequenceEmail> { email };
        var mock = new MockKitApiClient
        {
            GetSequenceAsyncFunc = (id, _) => Task.FromResult<Sequence?>(new Sequence { Id = id, Name = "Bootcamp 2.0" }),
            GetSequenceEmailAsyncFunc = (sid, eid, _) =>
            {
                var e = store.FirstOrDefault(x => x.Id == eid && x.SequenceId == sid);
                return Task.FromResult<SequenceEmail?>(e == null ? null : Clone(e));
            },
            UpdateSequenceEmailAsyncFunc = (_, _, req, _) =>
            {
                email.Subject = req.Subject ?? email.Subject;
                email.Published = !email.Published; // unexpected protected-field drift
                return Task.FromResult<SequenceEmail?>(Clone(email));
            }
        };
        var manifest = WriteManifest(SubjectManifest((42, "Bootcamp 2.0", 7, "New", "Old subject")));
        var writer = new StringWriter();
        Console.SetOut(writer);

        var result = await SequenceCommands.HandleEmailUpdateBatch(
            [manifest, "--apply", "--confirm-field-scope"], mock);

        result.Should().Be(1);
        writer.ToString().Should().Contain("published changed unexpectedly");
    }

    [Fact]
    public async Task Batch_StopOnError_Should_Skip_Rows_After_A_Failure()
    {
        var store = new List<SequenceEmail>
        {
            MakeEmail(7, 42, "Old A"),
            MakeEmail(8, 42, "Old B"),
            MakeEmail(9, 42, "Old C")
        };
        var putCount = 0;
        var mock = new MockKitApiClient
        {
            GetSequenceAsyncFunc = (id, _) => Task.FromResult<Sequence?>(new Sequence { Id = id, Name = "Bootcamp 2.0" }),
            GetSequenceEmailAsyncFunc = (sid, eid, _) =>
            {
                var e = store.FirstOrDefault(x => x.Id == eid && x.SequenceId == sid);
                return Task.FromResult<SequenceEmail?>(e == null ? null : Clone(e));
            },
            UpdateSequenceEmailAsyncFunc = (_, eid, req, _) =>
            {
                putCount++;
                if (eid == 8)
                {
                    throw new HttpRequestException("boom on row 8");
                }

                var e = store.First(x => x.Id == eid);
                e.Subject = req.Subject ?? e.Subject;
                return Task.FromResult<SequenceEmail?>(Clone(e));
            }
        };
        var manifest = WriteManifest(SubjectManifest(
            (42, "Bootcamp 2.0", 7, "New A", "Old A"),
            (42, "Bootcamp 2.0", 8, "New B", "Old B"),
            (42, "Bootcamp 2.0", 9, "New C", "Old C")));
        var writer = new StringWriter();
        Console.SetOut(writer);

        var result = await SequenceCommands.HandleEmailUpdateBatch(
            [manifest, "--apply", "--confirm-field-scope"], mock);

        result.Should().Be(1);
        putCount.Should().Be(2); // row 7 applied, row 8 failed, row 9 never attempted
        var output = writer.ToString();
        output.Should().Contain("failed 1");
        output.Should().Contain("skipped 1");
    }

    [Fact]
    public async Task Batch_ContinueOnError_Should_Attempt_All_Rows()
    {
        var store = new List<SequenceEmail>
        {
            MakeEmail(7, 42, "Old A"),
            MakeEmail(8, 42, "Old B"),
            MakeEmail(9, 42, "Old C")
        };
        var putCount = 0;
        var mock = new MockKitApiClient
        {
            GetSequenceAsyncFunc = (id, _) => Task.FromResult<Sequence?>(new Sequence { Id = id, Name = "Bootcamp 2.0" }),
            GetSequenceEmailAsyncFunc = (sid, eid, _) =>
            {
                var e = store.FirstOrDefault(x => x.Id == eid && x.SequenceId == sid);
                return Task.FromResult<SequenceEmail?>(e == null ? null : Clone(e));
            },
            UpdateSequenceEmailAsyncFunc = (_, eid, req, _) =>
            {
                putCount++;
                if (eid == 8)
                {
                    throw new HttpRequestException("boom on row 8");
                }

                var e = store.First(x => x.Id == eid);
                e.Subject = req.Subject ?? e.Subject;
                return Task.FromResult<SequenceEmail?>(Clone(e));
            }
        };
        var manifest = WriteManifest(SubjectManifest(
            (42, "Bootcamp 2.0", 7, "New A", "Old A"),
            (42, "Bootcamp 2.0", 8, "New B", "Old B"),
            (42, "Bootcamp 2.0", 9, "New C", "Old C")));
        var writer = new StringWriter();
        Console.SetOut(writer);

        var result = await SequenceCommands.HandleEmailUpdateBatch(
            [manifest, "--apply", "--confirm-field-scope", "--continue-on-error"], mock);

        result.Should().Be(1);
        putCount.Should().Be(3); // all three attempted despite the middle failure
        var output = writer.ToString();
        output.Should().Contain("updated 2");
        output.Should().Contain("failed 1");
    }

    // ---- Manifest validation ---------------------------------------------------------------

    [Fact]
    public async Task Batch_Should_Reject_Unknown_Manifest_Key()
    {
        // "published" at item level must be rejected (JsonUnmappedMemberHandling.Disallow) so a manifest
        // can never broaden the mutation scope.
        var mock = MakeMock("Bootcamp 2.0", new List<SequenceEmail> { MakeEmail(7, 42, "Old") });
        var json = """
        {
          "schema_version": 1,
          "items": [
            { "sequence_id": 42, "email_id": 7, "field": "subject", "replacement": "x", "published": true }
          ]
        }
        """;
        var manifest = WriteManifest(json);
        var writer = new StringWriter();
        Console.SetOut(writer);

        var result = await SequenceCommands.HandleEmailUpdateBatch([manifest], mock);

        result.Should().Be(1);
        writer.ToString().Should().Contain("Invalid manifest JSON");
    }

    [Theory]
    [InlineData("{ \"schema_version\": 2, \"items\": [ { \"sequence_id\": 1, \"email_id\": 1, \"field\": \"subject\", \"replacement\": \"x\" } ] }", "schema_version")]
    [InlineData("{ \"schema_version\": 1, \"items\": [] }", "no items")]
    [InlineData("{ \"schema_version\": 1, \"items\": null }", "no items")]
    [InlineData("{ \"schema_version\": 1, \"items\": [ { \"sequence_id\": 1, \"email_id\": 1, \"field\": \"bogus\", \"replacement\": \"x\" } ] }", "must be 'subject' or 'content'")]
    [InlineData("{ \"schema_version\": 1, \"items\": [ { \"sequence_id\": 1, \"email_id\": 1, \"field\": \"subject\" } ] }", "requires a non-empty 'replacement'")]
    [InlineData("{ \"schema_version\": 1, \"items\": [ { \"sequence_id\": 1, \"email_id\": 1, \"field\": \"content\" } ] }", "requires 'content_file'")]
    [InlineData("{ \"schema_version\": 1, \"items\": [ { \"sequence_id\": 1, \"email_id\": 1, \"field\": \"subject\", \"replacement\": \"x\" } ] }", "requires 'expect_subject'")]
    [InlineData("{ \"schema_version\": 1, \"items\": [ { \"sequence_id\": 1, \"email_id\": 1, \"field\": \"content\", \"content_file\": \"body.html\" } ] }", "requires 'expect_content_sha256'")]
    public async Task Batch_Should_Reject_Invalid_Manifest(string json, string expectedMessage)
    {
        var mock = MakeMock("x", new List<SequenceEmail>());
        var manifest = WriteManifest(json);
        var writer = new StringWriter();
        Console.SetOut(writer);

        var result = await SequenceCommands.HandleEmailUpdateBatch([manifest], mock);

        result.Should().Be(1);
        writer.ToString().Should().Contain(expectedMessage);
    }

    [Fact]
    public async Task Batch_Should_Reject_Duplicate_Rows()
    {
        var mock = MakeMock("Bootcamp 2.0", new List<SequenceEmail> { MakeEmail(7, 42, "Old") });
        var manifest = WriteManifest(SubjectManifest(
            (42, "Bootcamp 2.0", 7, "New A", "Old"),
            (42, "Bootcamp 2.0", 7, "New B", "Old")));
        var writer = new StringWriter();
        Console.SetOut(writer);

        var result = await SequenceCommands.HandleEmailUpdateBatch([manifest], mock);

        result.Should().Be(1);
        writer.ToString().Should().Contain("duplicate");
    }

    // ---- Report and resume -----------------------------------------------------------------

    [Fact]
    public async Task Batch_Should_Write_Report_With_Manifest_Hash()
    {
        var store = new List<SequenceEmail> { MakeEmail(7, 42, "Old subject") };
        var mock = MakeMock("Bootcamp 2.0", store);
        var manifest = WriteManifest(SubjectManifest((42, "Bootcamp 2.0", 7, "New", "Old subject")));
        var reportPath = TempPath(".json");
        Console.SetOut(new StringWriter());

        var result = await SequenceCommands.HandleEmailUpdateBatch(
            [manifest, "--report", reportPath, "--apply", "--confirm-field-scope"], mock);

        result.Should().Be(0);
        File.Exists(reportPath).Should().BeTrue();
        using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(reportPath));
        doc.RootElement.GetProperty("manifest_sha256").GetString().Should().NotBeNullOrEmpty();
        doc.RootElement.GetProperty("updated").GetInt32().Should().Be(1);
        doc.RootElement.GetProperty("scope_statement").GetString().Should().Contain("publication-state");
    }

    [Fact]
    public async Task Batch_Resume_Should_Skip_Already_Applied_Rows()
    {
        var store = new List<SequenceEmail>
        {
            MakeEmail(7, 42, "Old A"),
            MakeEmail(8, 42, "Old B")
        };
        var putCount = 0;
        var mock = MakeMock("Bootcamp 2.0", store, () => putCount++);
        var manifest = WriteManifest(SubjectManifest(
            (42, "Bootcamp 2.0", 7, "New A", "Old A"),
            (42, "Bootcamp 2.0", 8, "New B", "Old B")));

        // A prior report (with matching manifest provenance) marking email 7 as already applied.
        var priorReport = new SequenceEmailBatchReport
        {
            ManifestSha256 = ManifestSha(manifest),
            Items = new[]
            {
                new SequenceEmailBatchItemReport { SequenceId = 42, EmailId = 7, Field = "subject", Status = "applied" }
            }
        };
        var resumePath = TempPath(".json");
        await File.WriteAllTextAsync(resumePath, JsonSerializer.Serialize(priorReport, KitJsonIndentedContext.Default.SequenceEmailBatchReport));
        Console.SetOut(new StringWriter());

        var result = await SequenceCommands.HandleEmailUpdateBatch(
            [manifest, "--resume", resumePath, "--apply", "--confirm-field-scope"], mock);

        result.Should().Be(0);
        putCount.Should().Be(1); // only row 8 is written; row 7 is skipped via resume
        store.Single(e => e.Id == 7).Subject.Should().Be("Old A"); // untouched
        store.Single(e => e.Id == 8).Subject.Should().Be("New B");
    }

    [Fact]
    public async Task Batch_Preflight_Failed_Apply_Should_Label_Report_Mode_Not_Apply()
    {
        var store = new List<SequenceEmail> { MakeEmail(7, 42, "Actual") };
        var putCount = 0;
        var mock = MakeMock("Bootcamp 2.0", store, () => putCount++);
        // expect_subject won't match live "Actual" -> preflight fails, zero writes.
        var manifest = WriteManifest(SubjectManifest((42, "Bootcamp 2.0", 7, "New", "Expected old")));
        var reportPath = TempPath(".json");
        Console.SetOut(new StringWriter());

        var result = await SequenceCommands.HandleEmailUpdateBatch(
            [manifest, "--report", reportPath, "--apply", "--confirm-field-scope"], mock);

        result.Should().Be(1);
        putCount.Should().Be(0);
        using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(reportPath));
        doc.RootElement.GetProperty("mode").GetString().Should().Be("preflight-failed");
    }

    [Fact]
    public async Task Batch_Report_Write_Failure_Should_Yield_Nonzero_Exit()
    {
        var store = new List<SequenceEmail> { MakeEmail(7, 42, "Old subject") };
        var mock = MakeMock("Bootcamp 2.0", store);
        var manifest = WriteManifest(SubjectManifest((42, "Bootcamp 2.0", 7, "New", "Old subject")));
        // A report path inside a non-existent directory cannot be written.
        var badReport = Path.Combine(Path.GetTempPath(), $"kit-cli-nodir-{Guid.NewGuid():N}", "r.json");
        Console.SetOut(new StringWriter());
        Console.SetError(new StringWriter());

        var result = await SequenceCommands.HandleEmailUpdateBatch(
            [manifest, "--report", badReport, "--apply", "--confirm-field-scope"], mock);

        // The edit succeeded, but the requested audit report could not be written -> non-zero.
        result.Should().Be(1);
        store.Single().Subject.Should().Be("New");
    }

    [Fact]
    public async Task Batch_Resume_Should_Reject_Report_From_A_Different_Manifest()
    {
        var store = new List<SequenceEmail> { MakeEmail(7, 42, "Old A") };
        var putCount = 0;
        var mock = MakeMock("Bootcamp 2.0", store, () => putCount++);
        var manifest = WriteManifest(SubjectManifest((42, "Bootcamp 2.0", 7, "New A", "Old A")));

        var priorReport = new SequenceEmailBatchReport
        {
            ManifestSha256 = "deadbeefdeadbeefdeadbeefdeadbeefdeadbeefdeadbeefdeadbeefdeadbeef",
            Items = new[] { new SequenceEmailBatchItemReport { SequenceId = 42, EmailId = 7, Field = "subject", Status = "applied" } }
        };
        var resumePath = TempPath(".json");
        await File.WriteAllTextAsync(resumePath, JsonSerializer.Serialize(priorReport, KitJsonIndentedContext.Default.SequenceEmailBatchReport));
        Console.SetOut(new StringWriter());
        var errWriter = new StringWriter();
        Console.SetError(errWriter);

        var result = await SequenceCommands.HandleEmailUpdateBatch(
            [manifest, "--resume", resumePath, "--apply", "--confirm-field-scope"], mock);

        result.Should().Be(1);
        putCount.Should().Be(0);
        errWriter.ToString().Should().Contain("different manifest");
    }

    [Fact]
    public async Task Batch_Resume_Missing_File_Should_Error_Not_Silently_Continue()
    {
        var store = new List<SequenceEmail> { MakeEmail(7, 42, "Old A") };
        var putCount = 0;
        var mock = MakeMock("Bootcamp 2.0", store, () => putCount++);
        var manifest = WriteManifest(SubjectManifest((42, "Bootcamp 2.0", 7, "New A", "Old A")));
        Console.SetOut(new StringWriter());
        var errWriter = new StringWriter();
        Console.SetError(errWriter);

        var result = await SequenceCommands.HandleEmailUpdateBatch(
            [manifest, "--resume", "/no/such/run.json", "--apply", "--confirm-field-scope"], mock);

        result.Should().Be(1);
        putCount.Should().Be(0);
        errWriter.ToString().Should().Contain("resume report not found");
    }

    [Fact]
    public async Task Batch_DryRun_With_Resume_Should_Show_All_Rows()
    {
        // A dry-run must show the full planned state even with --resume, so the operator can review
        // everything before applying.
        var store = new List<SequenceEmail>
        {
            MakeEmail(7, 42, "Old A"),
            MakeEmail(8, 42, "Old B")
        };
        var putCount = 0;
        var mock = MakeMock("Bootcamp 2.0", store, () => putCount++);
        var manifest = WriteManifest(SubjectManifest(
            (42, "Bootcamp 2.0", 7, "New A", "Old A"),
            (42, "Bootcamp 2.0", 8, "New B", "Old B")));

        var priorReport = new SequenceEmailBatchReport
        {
            Items = new[] { new SequenceEmailBatchItemReport { SequenceId = 42, EmailId = 7, Field = "subject", Status = "applied" } }
        };
        var resumePath = TempPath(".json");
        await File.WriteAllTextAsync(resumePath, JsonSerializer.Serialize(priorReport, KitJsonIndentedContext.Default.SequenceEmailBatchReport));
        var writer = new StringWriter();
        Console.SetOut(writer);

        var result = await SequenceCommands.HandleEmailUpdateBatch([manifest, "--resume", resumePath], mock);

        result.Should().Be(0);
        putCount.Should().Be(0);
        var output = writer.ToString();
        output.Should().Contain("DRY RUN");
        output.Should().Contain("email 7"); // resumed row still shown in a dry-run preview
        output.Should().Contain("email 8");
        output.Should().NotContain("resumed");
    }

    // ---- generate-manifest -----------------------------------------------------------------

    [Fact]
    public async Task GenerateManifest_Subject_Should_Prefill_Guards_From_Live_State()
    {
        var emails = new[]
        {
            MakeEmail(7, 42, "Subject One", published: true, position: 0),
            MakeEmail(8, 42, "Subject Two", published: false, position: 1)
        };
        var mock = new MockKitApiClient
        {
            GetSequenceAsyncFunc = (id, _) => Task.FromResult<Sequence?>(new Sequence { Id = id, Name = "Bootcamp 2.0" }),
            GetAllSequenceEmailsAsyncFunc = (_, _, _, _) => ReturnEmails(emails),
            GetSequenceEmailAsyncFunc = (sid, eid, _) =>
                Task.FromResult<SequenceEmail?>(emails.FirstOrDefault(e => e.Id == eid && e.SequenceId == sid))
        };
        var outPath = TempPath(".json");
        Console.SetOut(new StringWriter());

        var result = await SequenceCommands.HandleEmailGenerateManifest(
            ["42", "--field", "subject", "--out", outPath], mock);

        result.Should().Be(0);
        var manifest = JsonSerializer.Deserialize(await File.ReadAllTextAsync(outPath), KitJsonContext.Default.SequenceEmailBatchManifest);
        manifest.Should().NotBeNull();
        manifest!.SchemaVersion.Should().Be(1);
        manifest.Items.Should().HaveCount(2);
        var first = manifest.Items!.Single(i => i.EmailId == 7);
        first.ExpectSubject.Should().Be("Subject One");
        first.Replacement.Should().Be("Subject One");
        first.ExpectedSequenceName.Should().Be("Bootcamp 2.0");
        first.ExpectedPosition.Should().Be(0);
        first.ExpectedPublished.Should().BeTrue();
    }

    [Fact]
    public async Task GenerateManifest_Content_Should_Store_ContentFile_Relative_To_Manifest_Dir()
    {
        // The manifest and the HTML bodies live in different directories; content_file must be
        // stored relative to the manifest's directory so update-batch can resolve it.
        var emails = new[] { MakeEmail(7, 42, "Subj", content: "<p>Body 7</p>") };
        var mock = new MockKitApiClient
        {
            GetSequenceAsyncFunc = (id, _) => Task.FromResult<Sequence?>(new Sequence { Id = id, Name = "Bootcamp 2.0" }),
            GetAllSequenceEmailsAsyncFunc = (_, _, _, _) => ReturnEmails(emails),
            // generate-manifest re-reads the authoritative body via the single-email endpoint.
            GetSequenceEmailAsyncFunc = (sid, eid, _) =>
                Task.FromResult<SequenceEmail?>(emails.FirstOrDefault(e => e.Id == eid && e.SequenceId == sid))
        };

        var baseDir = Path.Combine(Path.GetTempPath(), $"kit-cli-gen-{Guid.NewGuid():N}");
        Directory.CreateDirectory(baseDir);
        _tempDirs.Add(baseDir);
        var outPath = Path.Combine(baseDir, "out", "remediation.json");
        Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
        var contentDir = Path.Combine(baseDir, "bodies");
        Console.SetOut(new StringWriter());

        var result = await SequenceCommands.HandleEmailGenerateManifest(
            ["42", "--field", "content", "--out", outPath, "--content-dir", contentDir], mock);

        result.Should().Be(0);
        var manifest = JsonSerializer.Deserialize(await File.ReadAllTextAsync(outPath), KitJsonContext.Default.SequenceEmailBatchManifest);
        var item = manifest!.Items!.Single();
        item.ContentFile.Should().Be("../bodies/seq-42-email-7.html");
        // The stored relative path, resolved against the manifest's own directory, must exist.
        var manifestDir = Path.GetDirectoryName(outPath)!;
        File.Exists(Path.Combine(manifestDir, item.ContentFile!)).Should().BeTrue();
        item.ExpectContentSha256.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GenerateManifest_Should_Accept_Dash_o_Alias_After_Sequence_Ids()
    {
        var emails = new[] { MakeEmail(7, 42, "Subject One") };
        var mock = new MockKitApiClient
        {
            GetSequenceAsyncFunc = (id, _) => Task.FromResult<Sequence?>(new Sequence { Id = id, Name = "Bootcamp 2.0" }),
            GetAllSequenceEmailsAsyncFunc = (_, _, _, _) => ReturnEmails(emails),
            GetSequenceEmailAsyncFunc = (sid, eid, _) =>
                Task.FromResult<SequenceEmail?>(emails.FirstOrDefault(e => e.Id == eid && e.SequenceId == sid))
        };
        var outPath = TempPath(".json");
        var writer = new StringWriter();
        Console.SetOut(writer);

        // -o placed right after the positional sequence id must be treated as --out, not an ID.
        var result = await SequenceCommands.HandleEmailGenerateManifest(
            ["42", "-o", outPath, "--field", "subject"], mock);

        result.Should().Be(0);
        writer.ToString().Should().NotContain("Invalid sequence ID");
        File.Exists(outPath).Should().BeTrue();
    }

    [Fact]
    public async Task GenerateManifest_Content_Should_Error_And_Emit_Nothing_When_No_Bodies()
    {
        var noBody = MakeEmail(7, 42, "Subj");
        noBody.Content = null;
        var emails = new[] { noBody };
        var mock = new MockKitApiClient
        {
            GetSequenceAsyncFunc = (id, _) => Task.FromResult<Sequence?>(new Sequence { Id = id, Name = "Bootcamp 2.0" }),
            GetAllSequenceEmailsAsyncFunc = (_, _, _, _) => ReturnEmails(emails),
            GetSequenceEmailAsyncFunc = (_, eid, _) => Task.FromResult<SequenceEmail?>(emails.FirstOrDefault(e => e.Id == eid))
        };
        var outPath = TempPath(".json");
        var writer = new StringWriter();
        Console.SetOut(writer);

        var result = await SequenceCommands.HandleEmailGenerateManifest(
            ["42", "--field", "content", "--out", outPath, "--content-dir", Path.GetTempPath()], mock);

        result.Should().Be(1);
        writer.ToString().Should().Contain("No manifest emitted");
        File.Exists(outPath).Should().BeFalse(); // no manifest emitted
    }

    // ---- helpers ---------------------------------------------------------------------------

    private static SequenceEmail MakeEmail(long id, long sequenceId, string subject, bool published = true, int position = 1, string content = "<p>Body</p>") => new()
    {
        Id = id,
        SequenceId = sequenceId,
        Subject = subject,
        Content = content,
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

    private static MockKitApiClient MakeMock(string sequenceName, List<SequenceEmail> store, Action? onPut = null) => new()
    {
        GetSequenceAsyncFunc = (id, _) => Task.FromResult<Sequence?>(new Sequence { Id = id, Name = sequenceName }),
        GetSequenceEmailAsyncFunc = (sid, eid, _) =>
        {
            var e = store.FirstOrDefault(x => x.Id == eid && x.SequenceId == sid);
            return Task.FromResult<SequenceEmail?>(e == null ? null : Clone(e));
        },
        UpdateSequenceEmailAsyncFunc = (sid, eid, req, _) =>
        {
            onPut?.Invoke();
            var e = store.First(x => x.Id == eid && x.SequenceId == sid);
            if (req.Subject != null) e.Subject = req.Subject;
            if (req.Content != null) e.Content = req.Content;
            return Task.FromResult<SequenceEmail?>(Clone(e));
        }
    };

    private static string SubjectManifest(params (long seqId, string seqName, long emailId, string replacement, string expect)[] rows)
    {
        var items = rows.Select(r => $$"""
            {
              "sequence_id": {{r.seqId}},
              "expected_sequence_name": {{JsonSerializer.Serialize(r.seqName)}},
              "email_id": {{r.emailId}},
              "field": "subject",
              "replacement": {{JsonSerializer.Serialize(r.replacement)}},
              "expect_subject": {{JsonSerializer.Serialize(r.expect)}}
            }
            """);
        return $$"""
        {
          "schema_version": 1,
          "name": "test remediation",
          "items": [ {{string.Join(",", items)}} ]
        }
        """;
    }

    private static async IAsyncEnumerable<SequenceEmail> ReturnEmails(params SequenceEmail[] emails)
    {
        foreach (var email in emails)
        {
            yield return email;
        }

        await Task.CompletedTask;
    }

    private string TempPath(string ext)
    {
        var p = Path.Combine(Path.GetTempPath(), $"kit-cli-batch-{Guid.NewGuid():N}{ext}");
        _tempFiles.Add(p);
        return p;
    }

    private string WriteManifest(string json)
    {
        var p = TempPath(".json");
        File.WriteAllText(p, json);
        return p;
    }

    // Matches the CLI's manifest provenance hash (lowercase hex SHA-256 of the file bytes).
    private static string ManifestSha(string path)
    {
        var hash = System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(path));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
