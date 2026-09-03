using KitCLI.Helpers;
using KitCLI.Models;
using KitCLI.Services;

namespace KitCLI.Commands;

public static class SequenceCommands
{
    public static async Task<int> HandleList(string[] args, IKitApiClient client)
    {
        string format = "table";
        int limit = 50;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--format":
                case "-f":
                    if (i + 1 < args.Length)
                    {
                        format = args[++i];
                    }

                    break;
                case "--limit":
                case "-l":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out var l))
                    {
                        limit = l;
                    }

                    break;
            }
        }

        using var progress = new ProgressIndicator("Fetching sequences");

        var response = await client.GetSequencesAsync(limit);
        var sequences = response.Data;

        progress.Complete($"Found {sequences.Length:N0} sequences");

        PrintSequences(sequences, format);
        return 0;
    }

    public static async Task<int> HandleGet(string[] args, IKitApiClient client)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: kit sequence get <id>");
            return 1;
        }

        if (!long.TryParse(args[0], out var id))
        {
            Console.WriteLine("Invalid sequence ID. Please provide a numeric ID.");
            return 1;
        }

        string format = "json";

        for (int i = 1; i < args.Length; i++)
        {
            if ((args[i] == "--format" || args[i] == "-f") && i + 1 < args.Length)
            {
                format = args[++i];
            }
        }

        using var progress = new ProgressIndicator($"Fetching sequence {id}");

        var sequence = await client.GetSequenceAsync(id);

        if (sequence == null)
        {
            progress.Complete($"Sequence not found: {id}");
            return 1;
        }

        progress.Complete($"Found sequence: {TerminalText.RenderSingleLine(sequence.Name)}");

        if (format == "json")
        {
            var json = System.Text.Json.JsonSerializer.Serialize(
                sequence,
                KitJsonIndentedContext.Default.Sequence);
            Console.WriteLine(json);
        }
        else
        {
            PrintSequences([sequence], format);
        }

        return 0;
    }

    public static async Task<int> HandleEmails(string[] args, IKitApiClient client)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: kit sequence emails <id> [options]");
            Console.WriteLine("Options:");
            Console.WriteLine("  --format, -f <format>  Output format (table, json)");
            Console.WriteLine("  --include-content      Include email HTML content in response");
            Console.WriteLine("  --include-stats        Include email performance stats");
            return 1;
        }

        if (!long.TryParse(args[0], out var sequenceId))
        {
            Console.WriteLine("Invalid sequence ID. Please provide a numeric ID.");
            return 1;
        }

        string format = "table";
        bool includeContent = false;
        bool includeStats = false;

        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--format":
                case "-f":
                    if (i + 1 < args.Length)
                    {
                        format = args[++i];
                    }

                    break;
                case "--include-content":
                    includeContent = true;
                    break;
                case "--include-stats":
                    includeStats = true;
                    break;
            }
        }

        using var progress = new ProgressIndicator($"Fetching emails for sequence {sequenceId}");

        var emails = new List<SequenceEmail>();
        await foreach (var email in client.GetAllSequenceEmailsAsync(
                           sequenceId,
                           includeContent: includeContent,
                           includeStats: includeStats))
        {
            emails.Add(email);
        }

        progress.Complete($"Found {emails.Count:N0} emails in sequence");

        if (format == "json")
        {
            var json = System.Text.Json.JsonSerializer.Serialize(
                emails.ToArray(),
                KitJsonIndentedContext.Default.SequenceEmailArray);
            Console.WriteLine(json);
        }
        else
        {
            PrintSequenceEmails(emails);
        }

        return 0;
    }

    public static async Task<int> HandleEmailGet(string[] args, IKitApiClient client)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: kit sequence email get <sequence-id> <email-id> [options]");
            Console.WriteLine("Options:");
            Console.WriteLine("  --format, -f <format>  Output format (table, json)");
            return 1;
        }

        if (!long.TryParse(args[0], out var sequenceId))
        {
            Console.WriteLine("Invalid sequence ID. Please provide a numeric ID.");
            return 1;
        }

        if (!long.TryParse(args[1], out var emailId))
        {
            Console.WriteLine("Invalid email ID. Please provide a numeric ID.");
            return 1;
        }

        string format = "json";

        for (int i = 2; i < args.Length; i++)
        {
            if ((args[i] == "--format" || args[i] == "-f") && i + 1 < args.Length)
            {
                format = args[++i];
            }
        }

        using var progress = new ProgressIndicator($"Fetching email {emailId} for sequence {sequenceId}");

        var email = await client.GetSequenceEmailAsync(sequenceId, emailId);

        if (email == null)
        {
            progress.Complete($"Email not found: {emailId} in sequence {sequenceId}");
            return 1;
        }

        progress.Complete($"Found email: {TerminalText.RenderSingleLine(email.Subject)}");

        if (format == "json")
        {
            var json = System.Text.Json.JsonSerializer.Serialize(
                email,
                KitJsonIndentedContext.Default.SequenceEmail);
            Console.WriteLine(json);
        }
        else
        {
            PrintSequenceEmails([email]);
        }

        return 0;
    }

    /// <summary>
    /// Safely updates the subject OR the HTML content of a single existing sequence email.
    /// Never transmits position, published, delay, send_days, template, or preview fields.
    /// Dry-run is the default; a write requires --apply plus --confirm-field-scope and is
    /// verified against the server both in the PUT response and a follow-up GET.
    /// </summary>
    public static async Task<int> HandleEmailUpdate(string[] args, IKitApiClient client)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: kit sequence email update <sequence-id> <email-id> (--subject <text> | --content-file <path>) [options]");
            Console.WriteLine("Options:");
            Console.WriteLine("  --subject <text>                Replace the email subject");
            Console.WriteLine("  --content-file <path>           Replace the email HTML body with the file contents");
            Console.WriteLine("  --apply                         Issue the PUT (default is a dry-run preview)");
            Console.WriteLine("  --confirm-field-scope           Required with --apply; acknowledges only one field is sent");
            Console.WriteLine("  --expect-subject <old>          Abort unless the current subject matches (subject ops)");
            Console.WriteLine("  --expect-content-sha256 <hex>   Abort unless the current content SHA-256 matches (content ops)");
            Console.WriteLine("  --format, -f <format>           Output format: text (default), json");
            return 1;
        }

        if (!long.TryParse(args[0], out var sequenceId))
        {
            Console.WriteLine("Invalid sequence ID. Please provide a numeric ID.");
            return 1;
        }

        if (!long.TryParse(args[1], out var emailId))
        {
            Console.WriteLine("Invalid email ID. Please provide a numeric ID.");
            return 1;
        }

        string? subject = null;
        string? contentFile = null;
        string? expectSubject = null;
        string? expectContentSha256 = null;
        bool apply = false;
        bool confirmFieldScope = false;
        string format = "text";

        for (int i = 2; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--subject":
                    if (i + 1 >= args.Length)
                    { Console.WriteLine("Missing value for --subject."); return 1; }
                    if (subject != null)
                    { Console.WriteLine("Duplicate --subject flag."); return 1; }
                    subject = args[++i];
                    break;
                case "--content-file":
                    if (i + 1 >= args.Length)
                    { Console.WriteLine("Missing value for --content-file."); return 1; }
                    if (contentFile != null)
                    { Console.WriteLine("Duplicate --content-file flag."); return 1; }
                    contentFile = args[++i];
                    break;
                case "--expect-subject":
                    if (i + 1 >= args.Length)
                    { Console.WriteLine("Missing value for --expect-subject."); return 1; }
                    expectSubject = args[++i];
                    break;
                case "--expect-content-sha256":
                    if (i + 1 >= args.Length)
                    { Console.WriteLine("Missing value for --expect-content-sha256."); return 1; }
                    expectContentSha256 = args[++i];
                    break;
                case "--apply":
                    apply = true;
                    break;
                case "--confirm-field-scope":
                    confirmFieldScope = true;
                    break;
                case "--format":
                case "-f":
                    if (i + 1 >= args.Length)
                    { Console.WriteLine("Missing value for --format."); return 1; }
                    format = args[++i];
                    break;
                default:
                    Console.WriteLine($"Unknown option: {args[i]}");
                    return 1;
            }
        }

        if (subject != null && contentFile != null)
        {
            Console.WriteLine("Specify only one of --subject or --content-file, never both.");
            return 1;
        }

        if (subject == null && contentFile == null)
        {
            Console.WriteLine("One of --subject or --content-file is required.");
            return 1;
        }

        if (format != "text" && format != "json")
        {
            Console.WriteLine("Invalid --format. Use 'text' or 'json'.");
            return 1;
        }

        bool isSubjectOp = subject != null;
        string field = isSubjectOp ? "subject" : "content";

        string? newSubject = null;
        string? newContent = null;

        if (isSubjectOp)
        {
            if (string.IsNullOrWhiteSpace(subject))
            {
                Console.WriteLine("--subject must not be empty or whitespace.");
                return 1;
            }

            newSubject = subject;

            if (expectContentSha256 != null)
            {
                Console.WriteLine("--expect-content-sha256 only applies to content updates (--content-file).");
                return 1;
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(contentFile))
            {
                Console.WriteLine("--content-file requires a file path.");
                return 1;
            }

            if (!File.Exists(contentFile))
            {
                Console.WriteLine($"Content file not found: {contentFile}");
                return 1;
            }

            try
            {
                newContent = await File.ReadAllTextAsync(contentFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not read content file: {ex.Message}");
                return 1;
            }

            if (string.IsNullOrEmpty(newContent))
            {
                Console.WriteLine("Content file is empty. Clearing content is not supported.");
                return 1;
            }

            if (expectSubject != null)
            {
                Console.WriteLine("--expect-subject only applies to subject updates (--subject).");
                return 1;
            }
        }

        if (apply && !confirmFieldScope)
        {
            Console.WriteLine("--apply requires --confirm-field-scope to acknowledge that only the "
                + $"{field} field will be sent. Re-run with --confirm-field-scope, or omit --apply for a dry-run.");
            return 1;
        }

        // Preflight GET — always fetch the exact target.
        var before = await client.GetSequenceEmailAsync(sequenceId, emailId);
        if (before == null)
        {
            Console.WriteLine($"Email not found: {emailId} in sequence {sequenceId}");
            return 1;
        }

        if (before.Id != emailId || before.SequenceId != sequenceId)
        {
            Console.WriteLine($"Verification failed: server returned email {before.Id} in sequence {before.SequenceId}, "
                + $"expected {emailId} in sequence {sequenceId}. Aborting with no write.");
            return 1;
        }

        if (!isSubjectOp && before.Content == null)
        {
            Console.WriteLine("Cannot verify content: the preflight GET returned no body for this email. "
                + "Aborting to avoid a blind overwrite.");
            return 1;
        }

        string? beforeContentSha = before.Content != null ? Sha256Hex(before.Content) : null;

        // Concurrency guards.
        if (expectSubject != null && !string.Equals(before.Subject, expectSubject, StringComparison.Ordinal))
        {
            Console.WriteLine("Precondition failed: the current subject does not match --expect-subject. Aborting (no PUT).");
            return 1;
        }

        if (expectContentSha256 != null && !string.Equals(beforeContentSha, expectContentSha256, StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("Precondition failed: the current content SHA-256 does not match --expect-content-sha256. Aborting (no PUT).");
            return 1;
        }

        // No-op detection.
        string? newContentSha = isSubjectOp ? null : Sha256Hex(newContent!);
        bool changed = isSubjectOp
            ? !string.Equals(before.Subject, newSubject, StringComparison.Ordinal)
            : !string.Equals(beforeContentSha, newContentSha, StringComparison.Ordinal);

        var report = new SequenceEmailUpdateReport
        {
            SequenceId = sequenceId,
            EmailId = emailId,
            Field = field,
            Mode = apply ? "apply" : "dry-run",
            Changed = changed
        };

        if (isSubjectOp)
        {
            report.SubjectBefore = before.Subject;
            report.SubjectAfter = newSubject;
        }
        else
        {
            report.ContentBytesBefore = System.Text.Encoding.UTF8.GetByteCount(before.Content!);
            report.ContentBytesAfter = System.Text.Encoding.UTF8.GetByteCount(newContent!);
            report.ContentSha256Before = beforeContentSha;
            report.ContentSha256After = newContentSha;
        }

        if (!changed)
        {
            report.Status = "no-change";
            if (format == "json")
            {
                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(report, KitJsonIndentedContext.Default.SequenceEmailUpdateReport));
            }
            else
            {
                Console.WriteLine($"No change needed — the {field} already matches the requested value. No PUT issued.");
            }

            return 0;
        }

        // Dry-run is the default: preview only, no write.
        if (!apply)
        {
            report.Status = "dry-run";
            RenderUpdateReport(report, format, dryRun: true);
            return 0;
        }

        // Apply: exactly one field-scoped PUT.
        var request = isSubjectOp
            ? SequenceEmailUpdateRequest.ForSubject(newSubject!)
            : SequenceEmailUpdateRequest.ForContent(newContent!);

        // The PUT is the only mutating call. A failure here means no verified change was made.
        SequenceEmail? after;
        try
        {
            // Suppress the spinner in JSON mode so its status line cannot precede/corrupt the report.
            using var progress = ProgressIndicatorFactory.Create(
                $"Updating {field} for email {emailId} in sequence {sequenceId}",
                enabled: format != "json");
            after = await client.UpdateSequenceEmailAsync(sequenceId, emailId, request);
            progress.Complete(after == null ? "Email not found" : "PUT complete");
        }
        catch (Exception ex)
        {
            // Broad catch: an HttpClient timeout surfaces as TaskCanceledException, not HttpRequestException.
            Console.WriteLine($"Update failed: {ex.Message}");
            return 1;
        }

        if (after == null)
        {
            Console.WriteLine($"Update failed: email {emailId} in sequence {sequenceId} was not found "
                + "(it may have been deleted). No changes applied.");
            return 1;
        }

        // Authoritative verification: re-read the target and compare against the preflight snapshot.
        // We verify GET-vs-GET (both the same representation) rather than trusting the PUT response,
        // which may be a leaner or normalized representation than a GET. Any failure BELOW this point
        // is a post-write outcome: the PUT already succeeded, so we never resend or compensate.
        SequenceEmail? confirm;
        try
        {
            confirm = await client.GetSequenceEmailAsync(sequenceId, emailId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"The update was sent, but the follow-up verification GET failed: {ex.Message}. "
                + "Treat the outcome as UNKNOWN — re-read the email before retrying; do not blindly re-apply.");
            return 1;
        }

        if (confirm == null)
        {
            Console.WriteLine("The update was sent, but the follow-up GET returned no email. "
                + "Treat the outcome as UNKNOWN — re-read the email before retrying; do not blindly re-apply.");
            return 1;
        }

        if (confirm.Id != emailId || confirm.SequenceId != sequenceId)
        {
            Console.WriteLine($"The update was sent, but the follow-up GET returned email {confirm.Id} in sequence {confirm.SequenceId}, "
                + $"expected {emailId} in sequence {sequenceId}. Treat the outcome as UNKNOWN.");
            return 1;
        }

        var verifyError = VerifyUpdate(before, confirm, field, newSubject, newContentSha);
        if (verifyError != null)
        {
            Console.WriteLine($"Verification failed: {verifyError}. The write was sent but the server state does not match "
                + "the intended field-only change. No compensating write performed.");
            return 1;
        }

        report.Status = "applied";
        report.Applied = true;
        report.Verified = true;
        if (isSubjectOp)
        {
            report.SubjectAfter = confirm.Subject;
        }
        else
        {
            report.ContentBytesAfter = confirm.Content != null ? System.Text.Encoding.UTF8.GetByteCount(confirm.Content) : 0;
            report.ContentSha256After = confirm.Content != null ? Sha256Hex(confirm.Content) : null;
        }

        RenderUpdateReport(report, format, dryRun: false);
        return 0;
    }

    /// <summary>
    /// Confirms the requested field changed to the intended value and every protected field is
    /// byte-identical between the two GET snapshots (<paramref name="before"/> = preflight,
    /// <paramref name="after"/> = follow-up). Comparing GET against GET keeps the representation
    /// consistent, so server-side normalization cannot cause a false protected-field diff.
    /// Returns null when the update is verified, or a short reason string when it is not.
    /// </summary>
    private static string? VerifyUpdate(SequenceEmail before, SequenceEmail after, string field, string? expectedSubject, string? expectedContentSha)
    {
        if (field == "subject")
        {
            if (!string.Equals(after.Subject, expectedSubject, StringComparison.Ordinal))
            {
                return "subject was not updated to the requested value";
            }

            if (!string.Equals(before.Content, after.Content, StringComparison.Ordinal))
            {
                return "content changed unexpectedly";
            }
        }
        else
        {
            var afterSha = after.Content != null ? Sha256Hex(after.Content) : null;
            if (!string.Equals(afterSha, expectedContentSha, StringComparison.Ordinal))
            {
                return "content was not updated to the requested value";
            }

            if (!string.Equals(before.Subject, after.Subject, StringComparison.Ordinal))
            {
                return "subject changed unexpectedly";
            }
        }

        if (before.Position != after.Position)
        {
            return "position changed unexpectedly";
        }

        if (before.Published != after.Published)
        {
            return "published changed unexpectedly";
        }

        if (before.DelayValue != after.DelayValue)
        {
            return "delay_value changed unexpectedly";
        }

        if (!string.Equals(before.DelayUnit, after.DelayUnit, StringComparison.Ordinal))
        {
            return "delay_unit changed unexpectedly";
        }

        if (before.EmailTemplateId != after.EmailTemplateId)
        {
            return "email_template_id changed unexpectedly";
        }

        if (!string.Equals(before.EmailAddress, after.EmailAddress, StringComparison.Ordinal))
        {
            return "email_address changed unexpectedly";
        }

        if (!string.Equals(before.PreviewText, after.PreviewText, StringComparison.Ordinal))
        {
            return "preview_text changed unexpectedly";
        }

        if (!SendDaysEqual(before.SendDays, after.SendDays))
        {
            return "send_days changed unexpectedly";
        }

        return null;
    }

    private static bool SendDaysEqual(string[]? a, string[]? b)
    {
        if (a == null && b == null)
        {
            return true;
        }

        if (a == null || b == null)
        {
            return false;
        }

        if (a.Length != b.Length)
        {
            return false;
        }

        for (int i = 0; i < a.Length; i++)
        {
            if (!string.Equals(a[i], b[i], StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static string Sha256Hex(string value) => Sha256HexBytes(System.Text.Encoding.UTF8.GetBytes(value));

    private static void RenderUpdateReport(SequenceEmailUpdateReport report, string format, bool dryRun)
    {
        if (format == "json")
        {
            var json = System.Text.Json.JsonSerializer.Serialize(report, KitJsonIndentedContext.Default.SequenceEmailUpdateReport);
            Console.WriteLine(json);
            return;
        }

        Console.WriteLine($"Sequence email update — sequence {report.SequenceId}, email {report.EmailId}");
        Console.WriteLine($"Field: {report.Field}");
        if (report.Field == "subject")
        {
            Console.WriteLine($"  Before: {TerminalText.RenderSingleLine(report.SubjectBefore ?? string.Empty)}");
            Console.WriteLine($"  After:  {TerminalText.RenderSingleLine(report.SubjectAfter ?? string.Empty)}");
        }
        else
        {
            Console.WriteLine($"  Before: {report.ContentBytesBefore} bytes, sha256 {report.ContentSha256Before}");
            Console.WriteLine($"  After:  {report.ContentBytesAfter} bytes, sha256 {report.ContentSha256After}");
        }

        Console.WriteLine(dryRun
            ? "DRY RUN — no PUT issued. Re-run with --apply --confirm-field-scope to write."
            : "Applied and verified ✓");
    }

    // ---- Batch field-scoped remediation (kit sequence email update-batch) --------------------
    //
    // Reuses the exact per-row guard/verify leaves as the single-email `update` command
    // (Sha256Hex, VerifyUpdate, SequenceEmailUpdateRequest.ForSubject/ForContent) so both paths
    // enforce identical safety invariants: one field per PUT, concurrency guards vs live state,
    // and a post-write read-back that rejects any protected-field drift. It can never transmit
    // position, published, delay, send_days, template, sender, or preview fields.

    private const string BatchScopeStatement =
        "No sends, enrollment, tags, forms, delays, scheduling, templates, sender settings, "
        + "preview text, position, or publication-state changes were requested or observed.";

    private sealed class BatchRow
    {
        public required SequenceEmailBatchManifestItem Item { get; init; }
        public required SequenceEmailBatchItemReport ItemReport { get; init; }
        public SequenceEmail? Before { get; set; }
        public string Field { get; set; } = string.Empty;
        public string? NewSubject { get; set; }
        public string? NewContent { get; set; }
        public string? NewContentSha { get; set; }
        public bool Changed { get; set; }
        public bool ResumeSkipped { get; set; }
    }

    /// <summary>
    /// Applies a reviewed manifest of single-field sequence-email edits. Dry-run is the default;
    /// a full preflight must pass with zero mismatches before any write, and writing requires
    /// --apply plus --confirm-field-scope.
    /// </summary>
    public static async Task<int> HandleEmailUpdateBatch(string[] args, IKitApiClient client, string? profile = null)
    {
        if (args.Length < 1 || args[0] is "--help" or "-h" or "help")
        {
            PrintBatchUsage();
            return args.Length < 1 ? 1 : 0;
        }

        string manifestPath = args[0];
        bool apply = false;
        bool confirmFieldScope = false;
        bool stopOnError = true;
        string format = "text";
        string? reportPath = null;
        string? resumePath = null;

        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--apply":
                    apply = true;
                    break;
                case "--confirm-field-scope":
                    confirmFieldScope = true;
                    break;
                case "--stop-on-error":
                    stopOnError = true;
                    break;
                case "--continue-on-error":
                    stopOnError = false;
                    break;
                case "--format":
                case "-f":
                    if (i + 1 >= args.Length)
                    { Console.WriteLine("Missing value for --format."); return 1; }
                    format = args[++i];
                    break;
                case "--report":
                    if (i + 1 >= args.Length)
                    { Console.WriteLine("Missing value for --report."); return 1; }
                    reportPath = args[++i];
                    break;
                case "--resume":
                    if (i + 1 >= args.Length)
                    { Console.WriteLine("Missing value for --resume."); return 1; }
                    resumePath = args[++i];
                    break;
                default:
                    Console.WriteLine($"Unknown option: {args[i]}");
                    return 1;
            }
        }

        if (format != "text" && format != "json")
        {
            Console.WriteLine("Invalid --format. Use 'text' or 'json'.");
            return 1;
        }

        if (!File.Exists(manifestPath))
        {
            Console.WriteLine($"Manifest not found: {manifestPath}");
            return 1;
        }

        byte[] manifestBytes;
        try
        {
            manifestBytes = await File.ReadAllBytesAsync(manifestPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Could not read manifest: {ex.Message}");
            return 1;
        }

        string manifestSha = Sha256HexBytes(manifestBytes);

        SequenceEmailBatchManifest? manifest;
        try
        {
            manifest = System.Text.Json.JsonSerializer.Deserialize(manifestBytes, KitJsonContext.Default.SequenceEmailBatchManifest);
        }
        catch (System.Text.Json.JsonException ex)
        {
            Console.WriteLine($"Invalid manifest JSON: {ex.Message}");
            return 1;
        }

        if (manifest == null)
        {
            Console.WriteLine("Manifest deserialized to null.");
            return 1;
        }

        var validationError = ValidateBatchManifest(manifest);
        if (validationError != null)
        {
            Console.WriteLine($"Manifest validation failed: {validationError}");
            return 1;
        }

        if (apply && !confirmFieldScope)
        {
            Console.WriteLine("--apply requires --confirm-field-scope to acknowledge that only the "
                + "target subject/content field of each row will be sent. Re-run with --confirm-field-scope, "
                + "or omit --apply for a dry-run.");
            return 1;
        }

        string manifestDir = Path.GetDirectoryName(Path.GetFullPath(manifestPath)) ?? ".";

        // --resume only affects an --apply run; a dry-run must always preview the full manifest, so
        // do not load (or validate the provenance of) a resume report in dry-run mode.
        var resumeVerified = new HashSet<(long, long)>();
        if (apply)
        {
            var (set, resumeError) = await LoadResumeSetAsync(resumePath, manifestSha);
            if (resumeError != null)
            {
                Console.Error.WriteLine(resumeError);
                return 1;
            }

            resumeVerified = set;

            // If resuming without an explicit --report, record progress back to the resume report
            // itself, so this run's applied rows are visible to a later resume (otherwise the run
            // would leave no record and a follow-up resume would re-attempt already-applied rows).
            if (resumePath != null && reportPath == null)
            {
                reportPath = resumePath;
                Console.Error.WriteLine($"No --report given; recording progress to the --resume report: {resumePath}");
            }
        }

        var report = new SequenceEmailBatchReport
        {
            ManifestSha256 = manifestSha,
            ManifestName = manifest.Name,
            ToolVersion = typeof(SequenceCommands).Assembly.GetName().Version?.ToString(),
            Profile = profile,
            Mode = apply ? "apply" : "dry-run",
            StartedAt = DateTimeOffset.UtcNow,
            ScopeStatement = BatchScopeStatement
        };

        // ---- Preflight pass: read and verify every row before any write ----
        var seqCache = new Dictionary<long, Sequence?>();
        var rows = new List<BatchRow>();
        bool preflightFailed = false;

        // Non-null here: ValidateBatchManifest already rejected null/empty Items.
        foreach (var item in manifest.Items!)
        {
            string field = item.Field.ToLowerInvariant();
            var itemReport = new SequenceEmailBatchItemReport
            {
                SequenceId = item.SequenceId,
                EmailId = item.EmailId,
                Field = field
            };
            var row = new BatchRow { Item = item, ItemReport = itemReport, Field = field };

            // Resume only takes effect for an actual --apply run: a dry-run must show the full
            // planned state, including rows a prior run already applied.
            if (apply && resumeVerified.Contains((item.SequenceId, item.EmailId)))
            {
                itemReport.Status = "resumed";
                itemReport.FailureReason = "already applied in --resume report";
                row.ResumeSkipped = true;
                report.Skipped++;
                rows.Add(row);
                continue;
            }

            report.Preflighted++;

            // Resolve the intended new value.
            if (field == "subject")
            {
                row.NewSubject = item.Replacement;
                itemReport.SubjectAfter = item.Replacement;
            }
            else
            {
                string contentPath = Path.IsPathRooted(item.ContentFile!)
                    ? item.ContentFile!
                    : Path.Combine(manifestDir, item.ContentFile!);
                if (!File.Exists(contentPath))
                {
                    FailPreflight(row, report, ref preflightFailed, $"content_file not found: {contentPath}");
                    rows.Add(row);
                    continue;
                }

                try
                {
                    row.NewContent = await File.ReadAllTextAsync(contentPath);
                }
                catch (Exception ex)
                {
                    FailPreflight(row, report, ref preflightFailed, $"could not read content_file: {ex.Message}");
                    rows.Add(row);
                    continue;
                }

                if (string.IsNullOrEmpty(row.NewContent))
                {
                    FailPreflight(row, report, ref preflightFailed, "content_file is empty; clearing content is not supported");
                    rows.Add(row);
                    continue;
                }

                row.NewContentSha = Sha256Hex(row.NewContent);
            }

            // Read the parent sequence (cached) and verify its name.
            if (!seqCache.TryGetValue(item.SequenceId, out var sequence))
            {
                sequence = await client.GetSequenceAsync(item.SequenceId);
                seqCache[item.SequenceId] = sequence;
            }

            if (sequence == null)
            {
                FailPreflight(row, report, ref preflightFailed, $"sequence {item.SequenceId} not found");
                rows.Add(row);
                continue;
            }

            if (item.ExpectedSequenceName != null
                && !string.Equals(sequence.Name, item.ExpectedSequenceName, StringComparison.Ordinal))
            {
                FailPreflight(row, report, ref preflightFailed,
                    $"expected_sequence_name mismatch (live '{sequence.Name}')");
                rows.Add(row);
                continue;
            }

            // Read the target email and verify identity + guards + delivery invariants.
            SequenceEmail? before;
            try
            {
                before = await client.GetSequenceEmailAsync(item.SequenceId, item.EmailId);
            }
            catch (Exception ex)
            {
                FailPreflight(row, report, ref preflightFailed, $"GET email failed: {ex.Message}");
                rows.Add(row);
                continue;
            }

            if (before == null)
            {
                FailPreflight(row, report, ref preflightFailed, $"email {item.EmailId} not found in sequence {item.SequenceId}");
                rows.Add(row);
                continue;
            }

            if (before.Id != item.EmailId || before.SequenceId != item.SequenceId)
            {
                FailPreflight(row, report, ref preflightFailed,
                    $"identity mismatch (server returned email {before.Id} in sequence {before.SequenceId})");
                rows.Add(row);
                continue;
            }

            row.Before = before;

            if (field == "content" && before.Content == null)
            {
                FailPreflight(row, report, ref preflightFailed,
                    "preflight GET returned no body; aborting to avoid a blind overwrite");
                rows.Add(row);
                continue;
            }

            string? beforeSha = before.Content != null ? Sha256Hex(before.Content) : null;

            // Delivery-state drift checks run BEFORE the idempotency short-circuit: a publish/position
            // change is a signal the sequence itself moved unexpectedly, and must abort the whole batch
            // even for a row whose content is already at its target value.
            if (item.ExpectedPublished.HasValue && before.Published != item.ExpectedPublished.Value)
            {
                FailPreflight(row, report, ref preflightFailed,
                    $"expected_published mismatch (live {before.Published.ToString().ToLowerInvariant()})");
                rows.Add(row);
                continue;
            }

            if (item.ExpectedPosition.HasValue && before.Position != item.ExpectedPosition.Value)
            {
                FailPreflight(row, report, ref preflightFailed,
                    $"expected_position mismatch (live {before.Position})");
                rows.Add(row);
                continue;
            }

            // Idempotency: if the live value already equals the intended replacement, the edit is
            // already applied — a re-run of the same manifest, or a prior run that crashed before its
            // progress was recorded. Treat it as a no-op (not a guard mismatch), so --resume and plain
            // re-runs are safe regardless of checkpoint timing. This is checked before the concurrency
            // guard because an already-applied live value will not match expect_subject/sha.
            bool alreadyApplied = field == "subject"
                ? string.Equals(before.Subject, row.NewSubject, StringComparison.Ordinal)
                : string.Equals(beforeSha, row.NewContentSha, StringComparison.Ordinal);

            if (alreadyApplied)
            {
                if (field == "subject")
                {
                    itemReport.SubjectBefore = before.Subject;
                    itemReport.SubjectAfter = row.NewSubject;
                }
                else
                {
                    itemReport.ContentBytesBefore = System.Text.Encoding.UTF8.GetByteCount(before.Content!);
                    itemReport.ContentBytesAfter = itemReport.ContentBytesBefore;
                    itemReport.ContentSha256Before = beforeSha;
                    itemReport.ContentSha256After = row.NewContentSha;
                }

                row.Changed = false;
                itemReport.Changed = false;
                itemReport.Status = "no-change";
                report.NoChange++;
                rows.Add(row);
                continue;
            }

            if (item.ExpectSubject != null
                && !string.Equals(before.Subject, item.ExpectSubject, StringComparison.Ordinal))
            {
                FailPreflight(row, report, ref preflightFailed, "expect_subject mismatch (live subject drifted)");
                rows.Add(row);
                continue;
            }

            if (item.ExpectContentSha256 != null
                && !string.Equals(beforeSha, item.ExpectContentSha256, StringComparison.OrdinalIgnoreCase))
            {
                FailPreflight(row, report, ref preflightFailed, "expect_content_sha256 mismatch (live body drifted)");
                rows.Add(row);
                continue;
            }

            // Snapshot before/after for the report.
            if (field == "subject")
            {
                itemReport.SubjectBefore = before.Subject;
                row.Changed = !string.Equals(before.Subject, row.NewSubject, StringComparison.Ordinal);
            }
            else
            {
                itemReport.ContentBytesBefore = System.Text.Encoding.UTF8.GetByteCount(before.Content!);
                itemReport.ContentBytesAfter = System.Text.Encoding.UTF8.GetByteCount(row.NewContent!);
                itemReport.ContentSha256Before = beforeSha;
                itemReport.ContentSha256After = row.NewContentSha;
                row.Changed = !string.Equals(beforeSha, row.NewContentSha, StringComparison.Ordinal);
            }

            itemReport.Changed = row.Changed;
            itemReport.Status = row.Changed ? "preflight-ok" : "no-change";
            rows.Add(row);
        }

        report.Items = rows.Select(r => r.ItemReport).ToArray();

        if (preflightFailed)
        {
            // Zero writes occurred, so do not label the audit report as an apply run.
            report.Mode = "preflight-failed";
            report.CompletedAt = DateTimeOffset.UtcNow;
            RenderBatchReport(report, format, "Preflight failed — no writes were issued. Resolve every mismatch and re-run.");
            await WriteReportFileAsync(report, reportPath);
            return 1;
        }

        if (!apply)
        {
            report.CompletedAt = DateTimeOffset.UtcNow;
            RenderBatchReport(report, format, "DRY RUN — no PUT issued. Re-run with --apply --confirm-field-scope to write.");
            bool dryRunReportOk = await WriteReportFileAsync(report, reportPath);
            return dryRunReportOk ? 0 : 1;
        }

        // ---- Apply pass: one field-scoped PUT per changed row, verified by read-back ----
        bool halted = false;
        int processed = 0;
        foreach (var row in rows)
        {
            if (row.ResumeSkipped)
            {
                continue;
            }

            if (!row.Changed)
            {
                // no-change rows are never written and never counted as skipped
                continue;
            }

            if (halted)
            {
                row.ItemReport.Status = "skipped";
                row.ItemReport.FailureReason = "not attempted after an earlier failure (--stop-on-error)";
                report.Skipped++;
                continue;
            }

            var (ok, reason, after) = await ApplyFieldUpdateAsync(
                client, row.Item.SequenceId, row.Item.EmailId, row.Field,
                row.NewSubject, row.NewContent, row.NewContentSha,
                row.Item.ExpectSubject, row.Item.ExpectContentSha256,
                row.Item.ExpectedPublished, row.Item.ExpectedPosition, format);

            if (ok)
            {
                report.Updated++;
                row.ItemReport.Status = "applied";
                if (row.Field == "subject")
                {
                    row.ItemReport.SubjectAfter = after!.Subject;
                }
                else
                {
                    row.ItemReport.ContentBytesAfter = after!.Content != null ? System.Text.Encoding.UTF8.GetByteCount(after.Content) : 0;
                    row.ItemReport.ContentSha256After = after.Content != null ? Sha256Hex(after.Content) : null;
                }
            }
            else
            {
                report.Failed++;
                row.ItemReport.Status = "failed";
                row.ItemReport.FailureReason = reason;
                if (stopOnError)
                {
                    halted = true;
                }
            }

            // Checkpoint periodically (and on a halt) so a hard interruption still leaves a record
            // that --resume can build on, without the O(N^2) I/O of rewriting the whole report every
            // row. Best-effort: a failed checkpoint does not abort an in-flight run (mutations already
            // happened); the final write below determines the exit code.
            processed++;
            if (reportPath != null && (halted || processed % 10 == 0))
            {
                await WriteReportFileAsync(report, reportPath);
            }
        }

        report.CompletedAt = DateTimeOffset.UtcNow;
        string summary = report.Failed == 0
            ? $"Applied and verified ✓ ({report.Updated} updated, {report.Skipped} skipped)"
            : $"Completed with {report.Failed} failure(s); {report.Updated} updated, {report.Skipped} skipped. See report.";
        RenderBatchReport(report, format, summary);
        bool reportOk = await WriteReportFileAsync(report, reportPath);
        if (!reportOk)
        {
            // A requested audit report could not be written; surface non-zero even if the edits
            // themselves succeeded, so the operator does not believe they have a record they lack.
            return 1;
        }

        return report.Failed == 0 ? 0 : 1;
    }

    /// <summary>
    /// Reads the specified sequences and emits a candidate manifest with each email's current
    /// value and concurrency guard pre-filled from live state, for review before an update-batch.
    /// This is a read-only operation against the Kit API (it only writes local files).
    /// </summary>
    public static async Task<int> HandleEmailGenerateManifest(string[] args, IKitApiClient client)
    {
        if (args.Length < 1 || args[0] is "--help" or "-h" or "help")
        {
            PrintGenerateManifestUsage();
            return args.Length < 1 ? 1 : 0;
        }

        var sequenceIds = new List<long>();
        string? field = null;
        string? outPath = null;
        string contentDir = ".";
        string? name = null;

        int i = 0;
        for (; i < args.Length; i++)
        {
            // Stop on any option (single- or double-dash), so the -o/-f aliases are not misread as IDs.
            if (args[i].StartsWith("-", StringComparison.Ordinal))
            {
                break;
            }

            if (!long.TryParse(args[i], out var sid) || sid <= 0)
            {
                Console.WriteLine($"Invalid sequence ID: {args[i]} (must be a positive number)");
                return 1;
            }

            sequenceIds.Add(sid);
        }

        for (; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--field":
                    if (i + 1 >= args.Length)
                    { Console.WriteLine("Missing value for --field."); return 1; }
                    field = args[++i].ToLowerInvariant();
                    break;
                case "--out":
                case "-o":
                    if (i + 1 >= args.Length)
                    { Console.WriteLine("Missing value for --out."); return 1; }
                    outPath = args[++i];
                    break;
                case "--content-dir":
                    if (i + 1 >= args.Length)
                    { Console.WriteLine("Missing value for --content-dir."); return 1; }
                    contentDir = args[++i];
                    break;
                case "--name":
                    if (i + 1 >= args.Length)
                    { Console.WriteLine("Missing value for --name."); return 1; }
                    name = args[++i];
                    break;
                default:
                    Console.WriteLine($"Unknown option: {args[i]}");
                    return 1;
            }
        }

        if (sequenceIds.Count == 0)
        {
            Console.WriteLine("At least one sequence ID is required.");
            return 1;
        }

        // De-duplicate so a repeated ID cannot emit duplicate (sequence_id, email_id) rows that
        // update-batch's own validator would then reject.
        sequenceIds = sequenceIds.Distinct().ToList();

        if (field != "subject" && field != "content")
        {
            Console.WriteLine("--field is required and must be 'subject' or 'content'.");
            return 1;
        }

        if (field == "content" && outPath == null)
        {
            Console.WriteLine("Content manifests require --out: the exported HTML bodies must live alongside the manifest.");
            return 1;
        }

        // content_file paths are stored relative to the manifest file's directory, because
        // update-batch resolves a relative content_file against the manifest's directory (not the
        // generator's CWD). Create the body directory once up front.
        string manifestBaseDir = outPath != null
            ? (Path.GetDirectoryName(Path.GetFullPath(outPath)) ?? ".")
            : ".";
        // Validate every requested sequence up front, before writing any files, so a missing
        // sequence cannot leave orphaned body files on disk.
        var sequences = new Dictionary<long, Sequence>();
        foreach (var sid in sequenceIds)
        {
            var sequence = await client.GetSequenceAsync(sid);
            if (sequence == null)
            {
                Console.WriteLine($"Sequence {sid} not found.");
                return 1;
            }

            sequences[sid] = sequence;
        }

        var items = new List<SequenceEmailBatchManifestItem>();
        var writtenFiles = new List<string>();
        bool contentDirCreated = false;
        int skippedNoBody = 0;
        int skippedUnreadable = 0;

        try
        {
            foreach (var sid in sequenceIds)
            {
                var sequence = sequences[sid];

                await foreach (var listed in client.GetAllSequenceEmailsAsync(sid, includeContent: false))
                {
                    // Read every authoritative field value via the SAME single-email endpoint that
                    // update-batch's preflight uses, so every baked expectation (subject, content SHA,
                    // published, position) matches what preflight will compute — the list endpoint's
                    // representation of any of these may differ.
                    var email = await client.GetSequenceEmailAsync(sid, listed.Id);
                    if (email == null)
                    {
                        // Listed but not individually readable: omit, but never silently.
                        skippedUnreadable++;
                        Console.Error.WriteLine($"Warning: email {listed.Id} in sequence {sid} could not be read individually and was omitted.");
                        continue;
                    }

                    var item = new SequenceEmailBatchManifestItem
                    {
                        SequenceId = sid,
                        ExpectedSequenceName = sequence.Name,
                        EmailId = email.Id,
                        Field = field,
                        ExpectedPublished = email.Published,
                        ExpectedPosition = email.Position
                    };

                    if (field == "subject")
                    {
                        item.ExpectSubject = email.Subject;
                        item.Replacement = email.Subject; // start from the current value; edit before applying
                    }
                    else
                    {
                        if (email.Content == null)
                        {
                            skippedNoBody++;
                            continue;
                        }

                        string fileName = $"seq-{sid}-email-{email.Id}.html";
                        string filePath = Path.Combine(contentDir, fileName);
                        item.ContentFile = Path.GetRelativePath(manifestBaseDir, Path.GetFullPath(filePath)).Replace('\\', '/');
                        item.ExpectContentSha256 = Sha256Hex(email.Content);
                        // Create the body directory lazily on the first actual write, so a run that
                        // finds no bodies leaves no orphaned directory behind.
                        if (!contentDirCreated)
                        {
                            Directory.CreateDirectory(contentDir);
                            contentDirCreated = true;
                        }

                        // Stream the body to disk as it is fetched (keeps memory flat regardless of
                        // body count/size); any mid-loop failure is cleaned up in the catch below so no
                        // orphaned files remain.
                        await File.WriteAllTextAsync(filePath, email.Content);
                        writtenFiles.Add(filePath);
                    }

                    items.Add(item);
                }
            }
        }
        catch (Exception ex)
        {
            CleanupFiles(writtenFiles);
            Console.WriteLine($"generate-manifest failed: {ex.Message}. Removed {writtenFiles.Count} partial file(s).");
            return 1;
        }

        if (items.Count == 0)
        {
            CleanupFiles(writtenFiles);
            Console.WriteLine(skippedNoBody > 0 || skippedUnreadable > 0
                ? $"No manifest emitted: no usable emails ({skippedNoBody} without a body, {skippedUnreadable} unreadable)."
                : "No manifest emitted: the requested sequence(s) have no emails.");
            return 1;
        }

        var manifest = new SequenceEmailBatchManifest
        {
            SchemaVersion = 1,
            Name = name ?? $"Generated manifest {DateTimeOffset.UtcNow:yyyy-MM-dd}",
            Source = $"generated from sequence(s) {string.Join(", ", sequenceIds)} on {DateTimeOffset.UtcNow:O}",
            Items = items.ToArray()
        };

        var json = System.Text.Json.JsonSerializer.Serialize(manifest, KitJsonIndentedContext.Default.SequenceEmailBatchManifest);

        if (outPath != null)
        {
            try
            {
                Directory.CreateDirectory(manifestBaseDir);
                await File.WriteAllTextAsync(outPath, json);
            }
            catch (Exception ex)
            {
                // Clean up the body files so a manifest-write failure leaves no orphans behind.
                CleanupFiles(writtenFiles);
                Console.WriteLine($"Could not write manifest to {outPath}: {ex.Message}. Removed {writtenFiles.Count} body file(s).");
                return 1;
            }

            Console.WriteLine($"Wrote {items.Count} item(s) to {outPath}.");
            if (field == "content")
            {
                Console.WriteLine($"HTML bodies written under {contentDir}. Edit them, then run a dry-run update-batch.");
            }
            else
            {
                Console.WriteLine("Edit each 'replacement' value, then run a dry-run update-batch.");
            }

            if (skippedNoBody > 0 || skippedUnreadable > 0)
            {
                Console.WriteLine($"Note: omitted {skippedNoBody} email(s) with no retrievable body and {skippedUnreadable} unreadable email(s).");
            }
        }
        else
        {
            Console.WriteLine(json);
        }

        return 0;
    }

    // Mirrors the apply/verify orchestration in HandleEmailUpdate, and adds a fresh pre-PUT read that
    // re-asserts the concurrency guard against the CURRENT live value. Because the batch checks all
    // guards during a single up-front preflight pass and only then writes, this per-row re-check
    // closes the drift window between preflight and this row's PUT (the single-email command GETs and
    // PUTs back-to-back, so it needs no separate re-check). The safety-critical protected-field
    // invariant lives in the shared VerifyUpdate leaf, enforced by both paths.
    private static async Task<(bool ok, string? reason, SequenceEmail? after)> ApplyFieldUpdateAsync(
        IKitApiClient client, long sequenceId, long emailId, string field,
        string? newSubject, string? newContent, string? newContentSha,
        string? expectSubject, string? expectContentSha256,
        bool? expectedPublished, int? expectedPosition, string format)
    {
        // Fresh pre-write read: re-assert identity, delivery state, and the guard against live state
        // at apply time.
        SequenceEmail before;
        try
        {
            var fresh = await client.GetSequenceEmailAsync(sequenceId, emailId);
            if (fresh == null)
            {
                return (false, "email not found at apply time (it may have been deleted)", null);
            }

            if (fresh.Id != emailId || fresh.SequenceId != sequenceId)
            {
                return (false, "identity mismatch at apply time (server returned a different email)", null);
            }

            before = fresh;
        }
        catch (Exception ex)
        {
            return (false, $"pre-write GET failed: {ex.Message}", null);
        }

        // Re-check delivery-state expectations at apply time, closing the preflight->PUT window: a
        // publish or reorder that slipped in since preflight must abort this row — and must be caught
        // even when the field value is already at target, so this runs before the idempotency
        // short-circuit below (which would otherwise report such a row 'applied').
        if (expectedPublished.HasValue && before.Published != expectedPublished.Value)
        {
            return (false, $"expected_published mismatch at apply time (live {before.Published.ToString().ToLowerInvariant()})", before);
        }

        if (expectedPosition.HasValue && before.Position != expectedPosition.Value)
        {
            return (false, $"expected_position mismatch at apply time (live {before.Position})", before);
        }

        // A deleted body must report accurately (before the content SHA guard, which would otherwise
        // fire first and misdiagnose it as ordinary drift).
        if (field == "content" && before.Content == null)
        {
            return (false, "no body at apply time; aborting to avoid a blind overwrite", before);
        }

        string? beforeSha = before.Content != null ? Sha256Hex(before.Content) : null;

        // Apply-time idempotency: if the live value already equals the intended replacement (a
        // concurrent run applied it between preflight and this PUT), the desired end state is already
        // achieved — treat it as a verified success (no PUT), not a guard failure that would halt the
        // batch under --stop-on-error. Delivery-state was already re-checked above.
        bool alreadyApplied = field == "subject"
            ? string.Equals(before.Subject, newSubject, StringComparison.Ordinal)
            : string.Equals(beforeSha, newContentSha, StringComparison.Ordinal);
        if (alreadyApplied)
        {
            return (true, null, before);
        }

        if (expectSubject != null && !string.Equals(before.Subject, expectSubject, StringComparison.Ordinal))
        {
            return (false, "expect_subject mismatch at apply time (live subject drifted since preflight)", before);
        }

        if (expectContentSha256 != null && !string.Equals(beforeSha, expectContentSha256, StringComparison.OrdinalIgnoreCase))
        {
            return (false, "expect_content_sha256 mismatch at apply time (live body drifted since preflight)", before);
        }

        var request = field == "subject"
            ? SequenceEmailUpdateRequest.ForSubject(newSubject!)
            : SequenceEmailUpdateRequest.ForContent(newContent!);

        SequenceEmail? after;
        try
        {
            using var progress = ProgressIndicatorFactory.Create(
                $"Updating {field} for email {before.Id} in sequence {before.SequenceId}",
                enabled: format != "json");
            after = await client.UpdateSequenceEmailAsync(before.SequenceId, before.Id, request);
            progress.Complete(after == null ? "Email not found" : "PUT complete");
        }
        catch (Exception ex)
        {
            // Catch broadly (not just HttpRequestException): an HttpClient timeout surfaces as
            // TaskCanceledException/OperationCanceledException. Any escape here would abort the
            // whole apply loop and skip the audit report, leaving partial mutations unrecorded.
            return (false, $"PUT failed: {ex.Message}", null);
        }

        if (after == null)
        {
            return (false, "email not found during PUT (it may have been deleted)", null);
        }

        // Authoritative verification: re-read and compare GET-vs-GET, exactly as the single-email path.
        SequenceEmail? confirm;
        try
        {
            confirm = await client.GetSequenceEmailAsync(before.SequenceId, before.Id);
        }
        catch (Exception ex)
        {
            return (false, $"verification GET failed after PUT: {ex.Message} (outcome UNKNOWN — re-read before retrying)", after);
        }

        if (confirm == null)
        {
            return (false, "verification GET returned no email after PUT (outcome UNKNOWN — re-read before retrying)", after);
        }

        if (confirm.Id != before.Id || confirm.SequenceId != before.SequenceId)
        {
            return (false, "verification GET returned a different email after PUT (outcome UNKNOWN)", confirm);
        }

        var verifyError = VerifyUpdate(before, confirm, field, newSubject, newContentSha);
        if (verifyError != null)
        {
            return (false, verifyError, confirm);
        }

        return (true, null, confirm);
    }

    private static void FailPreflight(BatchRow row, SequenceEmailBatchReport report, ref bool preflightFailed, string reason)
    {
        row.ItemReport.Status = "preflight-failed";
        row.ItemReport.FailureReason = reason;
        preflightFailed = true;
        report.Failed++;
    }

    private static string? ValidateBatchManifest(SequenceEmailBatchManifest manifest)
    {
        if (manifest.SchemaVersion != 1)
        {
            return $"unsupported schema_version {manifest.SchemaVersion} (expected 1)";
        }

        if (manifest.Items is not { Length: > 0 })
        {
            return "manifest has no items";
        }

        var seen = new HashSet<(long, long)>();
        for (int i = 0; i < manifest.Items.Length; i++)
        {
            var item = manifest.Items[i];
            if (item.SequenceId <= 0)
            {
                return $"item {i}: sequence_id must be a positive number";
            }

            if (item.EmailId <= 0)
            {
                return $"item {i}: email_id must be a positive number";
            }

            if (string.IsNullOrWhiteSpace(item.Field))
            {
                return $"item {i}: 'field' is required and must be 'subject' or 'content'";
            }

            string field = item.Field.ToLowerInvariant();
            if (field != "subject" && field != "content")
            {
                return $"item {i}: field must be 'subject' or 'content'";
            }

            if (!seen.Add((item.SequenceId, item.EmailId)))
            {
                return $"item {i}: duplicate (sequence_id, email_id) = ({item.SequenceId}, {item.EmailId})";
            }

            if (field == "subject")
            {
                if (string.IsNullOrWhiteSpace(item.Replacement))
                {
                    return $"item {i}: subject row requires a non-empty 'replacement'";
                }

                if (item.ContentFile != null || item.ExpectContentSha256 != null)
                {
                    return $"item {i}: subject row must not set content_file or expect_content_sha256";
                }

                // The concurrency guard is mandatory for batch: it is the only protection against
                // overwriting a subject that drifted since the manifest was authored.
                if (string.IsNullOrWhiteSpace(item.ExpectSubject))
                {
                    return $"item {i}: subject row requires 'expect_subject' (concurrency guard)";
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(item.ContentFile))
                {
                    return $"item {i}: content row requires 'content_file'";
                }

                if (item.Replacement != null || item.ExpectSubject != null)
                {
                    return $"item {i}: content row must not set replacement or expect_subject";
                }

                if (string.IsNullOrWhiteSpace(item.ExpectContentSha256))
                {
                    return $"item {i}: content row requires 'expect_content_sha256' (concurrency guard)";
                }
            }
        }

        return null;
    }

    private static async Task<(HashSet<(long, long)> set, string? error)> LoadResumeSetAsync(string? path, string currentManifestSha)
    {
        var set = new HashSet<(long, long)>();
        if (path == null)
        {
            return (set, null);
        }

        // A missing resume file is a hard error, not a silent fresh run: an operator who typed
        // --resume expects the prior progress to be honored, and silently re-attempting every
        // already-applied row would abort on guard mismatch.
        if (!File.Exists(path))
        {
            return (set, $"--resume report not found: {path}. Fix the path, or re-run without --resume for a fresh run.");
        }

        SequenceEmailBatchReport? prior;
        try
        {
            prior = System.Text.Json.JsonSerializer.Deserialize(
                await File.ReadAllTextAsync(path), KitJsonIndentedContext.Default.SequenceEmailBatchReport);
        }
        catch (Exception ex)
        {
            return (set, $"Could not parse --resume report {path}: {ex.Message}");
        }

        if (prior == null)
        {
            return (set, null);
        }

        // Provenance check: a report this tool writes always carries manifest_sha256. A report that
        // lacks it (hand-authored / corrupt) cannot be trusted to describe THIS manifest, and one
        // whose hash differs was produced from a different manifest — either would otherwise silently
        // skip rows whose intended edit has since changed.
        if (string.IsNullOrEmpty(prior.ManifestSha256))
        {
            return (set, $"The --resume report {path} has no manifest_sha256 provenance and cannot be "
                + "trusted against this manifest. Re-run without --resume for a fresh run.");
        }

        if (!string.Equals(prior.ManifestSha256, currentManifestSha, StringComparison.OrdinalIgnoreCase))
        {
            return (set, "The --resume report was produced from a different manifest "
                + $"(report sha {ShortSha(prior.ManifestSha256)}, current sha {ShortSha(currentManifestSha)}). "
                + "Refusing to resume; re-run without --resume, or resume against the original manifest.");
        }

        // Both a prior 'applied' and a prior 'resumed' row mean the edit is already live, so resuming
        // from a resumed report stays correct.
        if (prior.Items != null)
        {
            foreach (var it in prior.Items)
            {
                if (it.Status is "applied" or "resumed")
                {
                    set.Add((it.SequenceId, it.EmailId));
                }
            }
        }

        return (set, null);
    }

    // Returns true when there was nothing to write (no --report) or the write succeeded; false when a
    // requested report could not be written, so callers can surface a non-zero exit code.
    private static async Task<bool> WriteReportFileAsync(SequenceEmailBatchReport report, string? path)
    {
        if (path == null)
        {
            return true;
        }

        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(report, KitJsonIndentedContext.Default.SequenceEmailBatchReport);
            await File.WriteAllTextAsync(path, json);
            // stderr so it does not pollute --format json stdout.
            Console.Error.WriteLine($"Report written to {path}");
            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: could not write report to {path}: {ex.Message}");
            return false;
        }
    }

    private static void RenderBatchReport(SequenceEmailBatchReport report, string format, string summaryLine)
    {
        if (format == "json")
        {
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(report, KitJsonIndentedContext.Default.SequenceEmailBatchReport));
            return;
        }

        Console.WriteLine($"Sequence email batch update — {report.Mode}");
        if (!string.IsNullOrEmpty(report.ManifestName))
        {
            Console.WriteLine($"Manifest: {report.ManifestName}");
        }

        Console.WriteLine($"Manifest SHA-256: {report.ManifestSha256}");
        Console.WriteLine();

        foreach (var group in report.Items.GroupBy(x => x.SequenceId))
        {
            Console.WriteLine($"Sequence {group.Key}:");
            foreach (var it in group)
            {
                string change = it.Field == "subject"
                    ? $"{TerminalText.RenderSingleLine(it.SubjectBefore ?? string.Empty)} -> {TerminalText.RenderSingleLine(it.SubjectAfter ?? string.Empty)}"
                    : $"{it.ContentBytesBefore?.ToString() ?? "?"} bytes ({ShortSha(it.ContentSha256Before)}) -> {it.ContentBytesAfter?.ToString() ?? "?"} bytes ({ShortSha(it.ContentSha256After)})";
                string reason = it.FailureReason != null ? $" — {it.FailureReason}" : string.Empty;
                Console.WriteLine($"  email {it.EmailId} [{it.Field}] {it.Status}{reason}: {change}");
            }
        }

        Console.WriteLine();
        Console.WriteLine($"Counts — preflighted {report.Preflighted}, updated {report.Updated}, "
            + $"no-change {report.NoChange}, skipped {report.Skipped}, failed {report.Failed}");
        Console.WriteLine(summaryLine);
        Console.WriteLine(report.ScopeStatement);
    }

    private static string ShortSha(string? sha) =>
        string.IsNullOrEmpty(sha) ? "none" : (sha.Length <= 12 ? sha : sha[..12]);

    private static void CleanupFiles(IEnumerable<string> paths)
    {
        foreach (var p in paths)
        {
            try
            {
                File.Delete(p);
            }
            catch
            {
                // best effort
            }
        }
    }

    private static string Sha256HexBytes(byte[] value)
    {
        var hash = System.Security.Cryptography.SHA256.HashData(value);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static void PrintBatchUsage()
    {
        Console.WriteLine("Usage: kit sequence email update-batch <manifest.json> [options]");
        Console.WriteLine("Applies a reviewed manifest of single-field (subject OR content) sequence-email edits.");
        Console.WriteLine("Dry-run is the default; a full preflight must pass before any write.");
        Console.WriteLine("Options:");
        Console.WriteLine("  --apply                 Issue writes (default is a dry-run preview)");
        Console.WriteLine("  --confirm-field-scope   Required with --apply; acknowledges only the target field is sent");
        Console.WriteLine("  --stop-on-error         Stop after the first failure (default)");
        Console.WriteLine("  --continue-on-error     Attempt every changed row even after a failure");
        Console.WriteLine("  --resume <report.json>  Skip rows already applied in a prior report");
        Console.WriteLine("  --report <path>         Write a redacted JSON execution report");
        Console.WriteLine("  --format, -f <format>   Output format: text (default), json");
    }

    private static void PrintGenerateManifestUsage()
    {
        Console.WriteLine("Usage: kit sequence email generate-manifest <sequence-id> [<sequence-id> ...] --field subject|content [options]");
        Console.WriteLine("Reads the sequences and emits a candidate update-batch manifest with guards pre-filled from live state.");
        Console.WriteLine("Options:");
        Console.WriteLine("  --field subject|content  Which field to remediate (required)");
        Console.WriteLine("  --out, -o <path>         Write the manifest to a file (default: stdout)");
        Console.WriteLine("  --content-dir <dir>      Where to write HTML bodies for content manifests (default: .)");
        Console.WriteLine("  --name <text>            Manifest name");
    }

    // ---- Lifecycle: publish / unpublish (kit sequence email publish|unpublish) ----------------
    //
    // Delivery-sensitive: publishing a position-0 email can make Kit process queued subscribers
    // (i.e. trigger sends). Dry-run by default; a write requires --apply plus a typed --confirm,
    // and publishing a position-0 email requires an extra --confirm-position-zero. The write sends
    // ONLY {"published": ...} (via SequenceEmailPublishRequest) and is read-back verified to confirm
    // nothing but publish state changed.

    public static Task<int> HandleEmailPublish(string[] args, IKitApiClient client)
        => HandleEmailPublishState(args, client, publish: true);

    public static Task<int> HandleEmailUnpublish(string[] args, IKitApiClient client)
        => HandleEmailPublishState(args, client, publish: false);

    private static async Task<int> HandleEmailPublishState(string[] args, IKitApiClient client, bool publish)
    {
        string verb = publish ? "publish" : "unpublish";
        string confirmFlag = publish ? "--confirm-publish" : "--confirm-unpublish";

        if (args.Length < 2 || args[0] is "--help" or "-h" or "help")
        {
            Console.WriteLine($"Usage: kit sequence email {verb} <sequence-id> <email-id> [options]");
            Console.WriteLine($"Sets an existing sequence email's publish state to {(publish ? "published" : "unpublished")}. Sends only the");
            Console.WriteLine("published field — never subject, content, position, delay, send_days, template, or preview.");
            Console.WriteLine("Options:");
            Console.WriteLine("  --apply                    Issue the PUT (default is a dry-run preview)");
            Console.WriteLine($"  {confirmFlag,-26}Required with --apply");
            if (publish)
            {
                Console.WriteLine("  --confirm-position-zero    Required with --apply when the email is at position 0 (can trigger sends)");
            }

            Console.WriteLine("  --format, -f <format>      Output format: text (default), json");
            return args.Length < 2 ? 1 : 0;
        }

        if (!long.TryParse(args[0], out var sequenceId))
        {
            Console.WriteLine("Invalid sequence ID. Please provide a numeric ID.");
            return 1;
        }

        if (!long.TryParse(args[1], out var emailId))
        {
            Console.WriteLine("Invalid email ID. Please provide a numeric ID.");
            return 1;
        }

        bool apply = false;
        bool confirmVerb = false;
        bool confirmPositionZero = false;
        string format = "text";

        for (int i = 2; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--apply":
                    apply = true;
                    break;
                case "--confirm-publish":
                    if (publish)
                    { confirmVerb = true; }
                    else
                    { Console.WriteLine("Unknown option: --confirm-publish"); return 1; }
                    break;
                case "--confirm-unpublish":
                    if (!publish)
                    { confirmVerb = true; }
                    else
                    { Console.WriteLine("Unknown option: --confirm-unpublish"); return 1; }
                    break;
                case "--confirm-position-zero":
                    confirmPositionZero = true;
                    break;
                case "--format":
                case "-f":
                    if (i + 1 >= args.Length)
                    { Console.WriteLine("Missing value for --format."); return 1; }
                    format = args[++i];
                    break;
                default:
                    Console.WriteLine($"Unknown option: {args[i]}");
                    return 1;
            }
        }

        if (format != "text" && format != "json")
        {
            Console.WriteLine("Invalid --format. Use 'text' or 'json'.");
            return 1;
        }

        var before = await client.GetSequenceEmailAsync(sequenceId, emailId);
        if (before == null)
        {
            Console.WriteLine($"Email not found: {emailId} in sequence {sequenceId}");
            return 1;
        }

        if (before.Id != emailId || before.SequenceId != sequenceId)
        {
            Console.WriteLine($"Verification failed: server returned email {before.Id} in sequence {before.SequenceId}, "
                + $"expected {emailId} in sequence {sequenceId}. Aborting with no write.");
            return 1;
        }

        if (before.Published == publish)
        {
            Console.WriteLine($"No change needed — email {emailId} is already {(publish ? "published" : "unpublished")}. No PUT issued.");
            return 0;
        }

        bool positionZero = before.Position == 0;

        if (!apply)
        {
            Console.WriteLine($"Sequence email {verb} — sequence {sequenceId}, email {emailId} (position {before.Position})");
            Console.WriteLine($"  published: {before.Published.ToString().ToLowerInvariant()} -> {publish.ToString().ToLowerInvariant()}");
            if (publish && positionZero)
            {
                Console.WriteLine("  ⚠️  This email is at position 0. Publishing it can make Kit process queued subscribers "
                    + "(i.e. TRIGGER SENDS). Apply requires --confirm-position-zero.");
            }

            Console.WriteLine($"DRY RUN — no PUT issued. Re-run with --apply {confirmFlag}{(publish && positionZero ? " --confirm-position-zero" : string.Empty)} to write.");
            return 0;
        }

        if (!confirmVerb)
        {
            Console.WriteLine($"--apply requires {confirmFlag} to {verb} email {emailId}. Re-run with {confirmFlag}, or omit --apply for a dry-run.");
            return 1;
        }

        if (publish && positionZero && !confirmPositionZero)
        {
            Console.WriteLine($"Email {emailId} is at position 0; publishing it can trigger sends to queued subscribers. "
                + "Re-run with --confirm-position-zero to proceed.");
            return 1;
        }

        SequenceEmail? after;
        try
        {
            using var progress = ProgressIndicatorFactory.Create(
                $"Setting published={publish.ToString().ToLowerInvariant()} for email {emailId} in sequence {sequenceId}",
                enabled: format != "json");
            after = await client.SetSequenceEmailPublishedAsync(sequenceId, emailId, publish);
            progress.Complete(after == null ? "Email not found" : "PUT complete");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{char.ToUpperInvariant(verb[0])}{verb[1..]} failed: {ex.Message}");
            return 1;
        }

        if (after == null)
        {
            Console.WriteLine($"{verb} failed: email {emailId} in sequence {sequenceId} was not found. No changes applied.");
            return 1;
        }

        SequenceEmail? confirm;
        try
        {
            confirm = await client.GetSequenceEmailAsync(sequenceId, emailId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"The {verb} was sent, but the follow-up verification GET failed: {ex.Message}. "
                + "Treat the outcome as UNKNOWN — re-read the email before retrying.");
            return 1;
        }

        if (confirm == null || confirm.Id != emailId || confirm.SequenceId != sequenceId)
        {
            Console.WriteLine($"The {verb} was sent, but the follow-up GET did not return the expected email. Treat the outcome as UNKNOWN.");
            return 1;
        }

        var verifyError = VerifyPublishChange(before, confirm, publish);
        if (verifyError != null)
        {
            Console.WriteLine($"Verification failed: {verifyError}. The write was sent but the server state does not match "
                + "the intended publish-only change. No compensating write performed.");
            return 1;
        }

        Console.WriteLine($"Sequence email {verb} — sequence {sequenceId}, email {emailId}");
        Console.WriteLine($"  published: {before.Published.ToString().ToLowerInvariant()} -> {confirm.Published.ToString().ToLowerInvariant()}");
        Console.WriteLine("Applied and verified ✓");
        return 0;
    }

    // ---- Lifecycle: reorder (kit sequence email reorder) --------------------------------------
    //
    // Reorders a sequence by declaring the complete intended email order. Reordering can make active
    // subscribers skip or repeat emails, so it is dry-run by default and a write requires --apply plus
    // --confirm-reorder. The --order list must be a permutation of the sequence's current email IDs
    // (no adds/drops). Each move sends only {"position": N}; after all moves the full sequence is
    // re-read and the final order is required to match the target exactly.

    public static async Task<int> HandleEmailReorder(string[] args, IKitApiClient client)
    {
        if (args.Length < 1 || args[0] is "--help" or "-h" or "help")
        {
            Console.WriteLine("Usage: kit sequence email reorder <sequence-id> --order <id,id,...> [options]");
            Console.WriteLine("Reorders the sequence to the declared email order. --order must list every current email ID exactly once.");
            Console.WriteLine("Options:");
            Console.WriteLine("  --order <id,id,...>   Complete intended order of email IDs (required)");
            Console.WriteLine("  --apply               Issue the writes (default is a dry-run preview)");
            Console.WriteLine("  --confirm-reorder     Required with --apply");
            Console.WriteLine("  --format, -f <format> Output format: text (default), json");
            return args.Length < 1 ? 1 : 0;
        }

        if (!long.TryParse(args[0], out var sequenceId))
        {
            Console.WriteLine("Invalid sequence ID. Please provide a numeric ID.");
            return 1;
        }

        long[]? order = null;
        bool apply = false;
        bool confirmReorder = false;
        string format = "text";

        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--order":
                    if (i + 1 >= args.Length)
                    { Console.WriteLine("Missing value for --order."); return 1; }
                    var parts = args[++i].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    var parsed = new List<long>();
                    foreach (var p in parts)
                    {
                        if (!long.TryParse(p, out var id))
                        {
                            Console.WriteLine($"Invalid email ID in --order: {p}");
                            return 1;
                        }

                        parsed.Add(id);
                    }

                    order = parsed.ToArray();
                    break;
                case "--apply":
                    apply = true;
                    break;
                case "--confirm-reorder":
                    confirmReorder = true;
                    break;
                case "--format":
                case "-f":
                    if (i + 1 >= args.Length)
                    { Console.WriteLine("Missing value for --format."); return 1; }
                    format = args[++i];
                    break;
                default:
                    Console.WriteLine($"Unknown option: {args[i]}");
                    return 1;
            }
        }

        if (order == null || order.Length == 0)
        {
            Console.WriteLine("--order is required and must list the sequence's email IDs in the intended order.");
            return 1;
        }

        if (format != "text" && format != "json")
        {
            Console.WriteLine("Invalid --format. Use 'text' or 'json'.");
            return 1;
        }

        if (order.Length != order.Distinct().Count())
        {
            Console.WriteLine("--order contains a duplicate email ID.");
            return 1;
        }

        // Read the current ordered emails.
        var current = new List<SequenceEmail>();
        await foreach (var e in client.GetAllSequenceEmailsAsync(sequenceId))
        {
            current.Add(e);
        }

        if (current.Count == 0)
        {
            Console.WriteLine($"Sequence {sequenceId} has no emails (or was not found).");
            return 1;
        }

        current.Sort((a, b) => a.Position.CompareTo(b.Position));

        // --order must be a permutation of the current email IDs — no adds or drops.
        var currentIds = current.Select(e => e.Id).ToHashSet();
        var orderIds = order.ToHashSet();
        if (!currentIds.SetEquals(orderIds))
        {
            var missing = currentIds.Except(orderIds).ToArray();
            var extra = orderIds.Except(currentIds).ToArray();
            Console.WriteLine("--order must be a permutation of the sequence's current email IDs (no adds or drops).");
            if (missing.Length > 0)
            {
                Console.WriteLine($"  missing from --order: {string.Join(", ", missing)}");
            }

            if (extra.Length > 0)
            {
                Console.WriteLine($"  not in the sequence: {string.Join(", ", extra)}");
            }

            return 1;
        }

        // Target position for each email id is its index in --order.
        var targetPosition = new Dictionary<long, int>();
        for (int i = 0; i < order.Length; i++)
        {
            targetPosition[order[i]] = i;
        }

        var moves = current
            .Where(e => e.Position != targetPosition[e.Id])
            .Select(e => (e.Id, From: e.Position, To: targetPosition[e.Id]))
            .OrderBy(m => m.To)
            .ToArray();

        Console.WriteLine($"Sequence email reorder — sequence {sequenceId}");
        foreach (var e in order)
        {
            var cur = current.First(c => c.Id == e);
            string change = cur.Position == targetPosition[e] ? "(unchanged)" : $"{cur.Position} -> {targetPosition[e]}";
            Console.WriteLine($"  pos {targetPosition[e]}: email {e} {change}");
        }

        if (moves.Length == 0)
        {
            Console.WriteLine("No change needed — the sequence is already in the requested order. No writes issued.");
            return 0;
        }

        if (!apply)
        {
            Console.WriteLine($"DRY RUN — {moves.Length} email(s) would move. Re-run with --apply --confirm-reorder to write.");
            return 0;
        }

        if (!confirmReorder)
        {
            Console.WriteLine("--apply requires --confirm-reorder to reorder a live sequence (this can make active "
                + "subscribers skip or repeat emails). Re-run with --confirm-reorder, or omit --apply for a dry-run.");
            return 1;
        }

        // Concurrency guard: re-read and confirm the live order still matches what we planned from.
        var recheck = new List<SequenceEmail>();
        await foreach (var e in client.GetAllSequenceEmailsAsync(sequenceId))
        {
            recheck.Add(e);
        }

        recheck.Sort((a, b) => a.Position.CompareTo(b.Position));
        if (!recheck.Select(e => (e.Id, e.Position)).SequenceEqual(current.Select(e => (e.Id, e.Position))))
        {
            Console.WriteLine("Precondition failed: the sequence order changed between preview and apply. Re-run the dry-run and review before applying.");
            return 1;
        }

        // Apply position moves in ascending target order.
        int applied = 0;
        foreach (var m in moves)
        {
            try
            {
                using var progress = ProgressIndicatorFactory.Create(
                    $"Moving email {m.Id} to position {m.To}", enabled: format != "json");
                var res = await client.SetSequenceEmailPositionAsync(sequenceId, m.Id, m.To);
                progress.Complete(res == null ? "not found" : "moved");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Reorder failed while moving email {m.Id} to position {m.To}: {ex.Message}. "
                    + $"{applied} of {moves.Length} move(s) were applied; the sequence order may be partially changed — "
                    + "re-read it and re-run a dry-run before retrying.");
                return 1;
            }

            applied++;
        }

        // Authoritative verification: re-read the whole sequence and require the final order to match.
        var final = new List<SequenceEmail>();
        await foreach (var e in client.GetAllSequenceEmailsAsync(sequenceId))
        {
            final.Add(e);
        }

        final.Sort((a, b) => a.Position.CompareTo(b.Position));
        var finalOrder = final.Select(e => e.Id).ToArray();
        if (!finalOrder.SequenceEqual(order))
        {
            Console.WriteLine("Verification failed: after applying, the live order does not match the requested order.");
            Console.WriteLine($"  requested: {string.Join(", ", order)}");
            Console.WriteLine($"  live:      {string.Join(", ", finalOrder)}");
            Console.WriteLine("No compensating writes performed — re-read the sequence and reconcile in the Kit UI if needed.");
            return 1;
        }

        // Confirm the reorder did not add/drop emails or alter publish state per email.
        var beforeById = current.ToDictionary(e => e.Id);
        foreach (var e in final)
        {
            if (beforeById.TryGetValue(e.Id, out var b) && b.Published != e.Published)
            {
                Console.WriteLine($"Verification warning: email {e.Id} publish state changed during reorder "
                    + $"({b.Published.ToString().ToLowerInvariant()} -> {e.Published.ToString().ToLowerInvariant()}). Review in the Kit UI.");
                return 1;
            }
        }

        Console.WriteLine($"Reordered and verified ✓ ({moves.Length} email(s) moved).");
        return 0;
    }

    /// <summary>
    /// Verifies a publish-only change: published became <paramref name="expectedPublished"/> and every
    /// other field is byte-identical between the preflight and follow-up GET.
    /// </summary>
    private static string? VerifyPublishChange(SequenceEmail before, SequenceEmail after, bool expectedPublished)
    {
        if (after.Published != expectedPublished)
        {
            return "published was not updated to the requested value";
        }

        if (before.Position != after.Position)
        {
            return "position changed unexpectedly";
        }

        if (!string.Equals(before.Subject, after.Subject, StringComparison.Ordinal))
        {
            return "subject changed unexpectedly";
        }

        if (!string.Equals(before.Content, after.Content, StringComparison.Ordinal))
        {
            return "content changed unexpectedly";
        }

        if (before.DelayValue != after.DelayValue)
        {
            return "delay_value changed unexpectedly";
        }

        if (!string.Equals(before.DelayUnit, after.DelayUnit, StringComparison.Ordinal))
        {
            return "delay_unit changed unexpectedly";
        }

        if (before.EmailTemplateId != after.EmailTemplateId)
        {
            return "email_template_id changed unexpectedly";
        }

        if (!string.Equals(before.EmailAddress, after.EmailAddress, StringComparison.Ordinal))
        {
            return "email_address changed unexpectedly";
        }

        if (!string.Equals(before.PreviewText, after.PreviewText, StringComparison.Ordinal))
        {
            return "preview_text changed unexpectedly";
        }

        if (!SendDaysEqual(before.SendDays, after.SendDays))
        {
            return "send_days changed unexpectedly";
        }

        return null;
    }

    public static async Task<int> HandleSubscribers(string[] args, IKitApiClient client)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: kit sequence subscribers <id> [options]");
            Console.WriteLine("Options:");
            Console.WriteLine("  --status, -s <status>  Filter by status (active, inactive, bounced, complained, cancelled, all)");
            Console.WriteLine("  --format, -f <format>  Output format (table, json, csv)");
            Console.WriteLine("  --output, -o <file>    Export to file");
            Console.WriteLine("  --all                  Fetch all subscribers");
            return 1;
        }

        if (!long.TryParse(args[0], out var sequenceId))
        {
            Console.WriteLine("Invalid sequence ID. Please provide a numeric ID.");
            return 1;
        }

        string? status = null;
        string format = "table";
        string? outputPath = null;
        bool fetchAll = false;

        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--status":
                case "--state":
                case "-s":
                    if (i + 1 < args.Length)
                    {
                        status = args[++i];
                    }

                    break;
                case "--format":
                case "-f":
                    if (i + 1 < args.Length)
                    {
                        format = args[++i];
                    }

                    break;
                case "--output":
                case "-o":
                    if (i + 1 < args.Length)
                    {
                        outputPath = args[++i];
                    }

                    break;
                case "--all":
                    fetchAll = true;
                    break;
            }
        }

        using var progress = new ProgressIndicator($"Fetching subscribers for sequence {sequenceId}");

        List<SequenceSubscriber> subscribers = new();

        if (fetchAll)
        {
            await foreach (var subscriber in client.GetAllSequenceSubscribersAsync(sequenceId, status))
            {
                subscribers.Add(subscriber);
            }
        }
        else
        {
            var response = await client.GetSequenceSubscribersAsync(sequenceId, status, 100);
            subscribers.AddRange(response.Data);
        }

        progress.Complete($"Found {subscribers.Count:N0} subscribers in sequence");

        if (!string.IsNullOrEmpty(outputPath))
        {
            await ExportSequenceSubscribers(subscribers, outputPath);
            Console.WriteLine($"✓ Exported {subscribers.Count:N0} subscribers to {outputPath}");
        }
        else
        {
            PrintSequenceSubscribers(subscribers, format);
        }

        return 0;
    }

    public static async Task<int> HandleStats(string[] args, IKitApiClient client)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: kit sequence stats <id>");
            return 1;
        }

        if (!long.TryParse(args[0], out var id))
        {
            Console.WriteLine("Invalid sequence ID. Please provide a numeric ID.");
            return 1;
        }

        using var progress = new ProgressIndicator($"Calculating stats for sequence {id}");

        var sequence = await client.GetSequenceAsync(id);

        if (sequence == null)
        {
            progress.Complete($"Sequence not found: {id}");
            return 1;
        }

        var emails = new List<SequenceEmail>();
        await foreach (var email in client.GetAllSequenceEmailsAsync(id, includeStats: true))
        {
            emails.Add(email);
        }

        var sequenceName = TerminalText.RenderSingleLine(sequence.Name);
        progress.Complete($"Retrieved stats for sequence: {sequenceName}");

        Console.WriteLine("\nSequence Statistics");
        Console.WriteLine(new string('═', 60));
        Console.WriteLine($"Name: {sequenceName}");
        Console.WriteLine("Total Subscribers: N/A (use 'kit sequence subscribers <id> --all' to count)");
        Console.WriteLine($"Total Emails: {sequence.EmailCount:N0}");
        Console.WriteLine($"Status: {(sequence.Active ? "Active" : "Inactive")}");
        Console.WriteLine($"On Hold: {(sequence.Hold ? "Yes" : "No")}");
        Console.WriteLine($"Repeating: {(sequence.Repeat ? "Yes" : "No")}");

        if (emails.Count > 0)
        {
            Console.WriteLine("\nEmail Performance:");
            Console.WriteLine(new string('─', 60));

            var performance = SequenceEmailMetrics.Aggregate(emails);

            Console.WriteLine($"Total Emails Sent: {performance.Recipients:N0}");
            Console.WriteLine($"Total Opens: {performance.Opens:N0}");
            Console.WriteLine($"Total Clicks: {performance.Clicks:N0}");
            Console.WriteLine($"Average Open Rate: {performance.OpenRate:F2}%");
            Console.WriteLine($"Average Click Rate: {performance.ClickRate:F2}%");

            Console.WriteLine("\nTop Performing Emails:");
            Console.WriteLine(new string('─', 60));

            var topEmails = emails
                .OrderByDescending(e => e.Stats?.OpenRate ?? 0)
                .Take(3);

            foreach (var email in topEmails)
            {
                Console.WriteLine($"  • \"{TruncateString(TerminalText.RenderSingleLine(email.Subject), 40)}\"");
                Console.WriteLine($"    Position: {email.Position}, Delay: {TerminalText.RenderSingleLine(email.DelayFormatted)}");
                Console.WriteLine($"    Opens: {email.Stats?.OpenRate ?? 0:F2}%, Clicks: {email.Stats?.ClickRate ?? 0:F2}%");
            }
        }

        return 0;
    }

    public static async Task<int> HandleAnalyze(string[] args, IKitApiClient client)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: kit sequence analyze <id>");
            return 1;
        }

        if (!long.TryParse(args[0], out var id))
        {
            Console.WriteLine("Invalid sequence ID. Please provide a numeric ID.");
            return 1;
        }

        using var progress = new ProgressIndicator($"Analyzing sequence {id}");

        var stats = await client.GetSequenceStatsAsync(id);

        if (stats == null)
        {
            progress.Complete($"Sequence not found: {id}");
            return 1;
        }

        progress.Complete("Analysis complete");

        Console.WriteLine("\nSequence Analysis");
        Console.WriteLine(new string('═', 60));
        Console.WriteLine($"Total Subscribers: {stats.TotalSubscribers:N0}");
        var activeRate = stats.TotalSubscribers > 0
            ? (double)stats.ActiveSubscribers / stats.TotalSubscribers
            : 0;
        Console.WriteLine($"Active: {stats.ActiveSubscribers:N0} ({activeRate:P1})");
        Console.WriteLine($"Cancelled: {stats.CancelledSubscribers:N0}");
        Console.WriteLine();
        Console.WriteLine($"Emails Sent: {stats.EmailsSent:N0}");
        Console.WriteLine($"Average Open Rate: {stats.AverageOpenRate:F2}%");
        Console.WriteLine($"Average Click Rate: {stats.AverageClickRate:F2}%");

        Console.WriteLine("\nInsights:");
        Console.WriteLine(new string('─', 60));

        if (stats.AverageOpenRate < 20)
        {
            Console.WriteLine("⚠️  Below average open rate - review subject lines and preview text");
        }

        if (stats.AverageClickRate < 2)
        {
            Console.WriteLine("⚠️  Low click rate - consider improving CTAs and content relevance");
        }

        return 0;
    }

    private static void PrintSequences(IEnumerable<Sequence> sequences, string format)
    {
        var sequenceList = sequences.ToList();

        if (format.Equals("json", StringComparison.OrdinalIgnoreCase))
        {
            var json = System.Text.Json.JsonSerializer.Serialize(
                sequenceList.ToArray(),
                KitJsonIndentedContext.Default.SequenceArray);
            Console.WriteLine(json);
            return;
        }

        if (!sequenceList.Any())
        {
            Console.WriteLine("No sequences found.");
            return;
        }

        // Table format
        // Kit V4's list endpoint includes sequence metadata and subscriber/email counts.
        // Use 'kit sequence stats <id>' or 'kit sequence analyze <id>' for email performance details.
        const int idWidth = 10;
        const int nameWidth = 40;
        const int statusWidth = 10;
        const int holdWidth = 7;
        const int subscribersWidth = 12;
        const int emailsWidth = 8;
        const int createdWidth = 12;

        Console.WriteLine(new string('─', idWidth + nameWidth + statusWidth + holdWidth + subscribersWidth + emailsWidth + createdWidth + 17));
        Console.WriteLine($"│ {"ID",-idWidth} │ {"Name",-nameWidth} │ {"Status",-statusWidth} │ {"On Hold",-holdWidth} │ {"Subscribers",subscribersWidth} │ {"Emails",emailsWidth} │ {"Created",createdWidth} │");
        Console.WriteLine(new string('─', idWidth + nameWidth + statusWidth + holdWidth + subscribersWidth + emailsWidth + createdWidth + 17));

        foreach (var sequence in sequenceList.OrderBy(s => s.Name))
        {
            var name = TruncateString(TerminalText.RenderSingleLine(sequence.Name), nameWidth);
            var status = sequence.Active ? "Active" : "Inactive";
            var hold = sequence.Hold ? "Yes" : "No";
            var created = sequence.CreatedAt.ToString("yyyy-MM-dd");

            Console.WriteLine($"│ {sequence.Id,-idWidth} │ {name,-nameWidth} │ {status,-statusWidth} │ {hold,-holdWidth} │ {sequence.SubscriberCount,subscribersWidth:N0} │ {sequence.EmailCount,emailsWidth:N0} │ {created,createdWidth} │");
        }

        Console.WriteLine(new string('─', idWidth + nameWidth + statusWidth + holdWidth + subscribersWidth + emailsWidth + createdWidth + 17));
        Console.WriteLine($"Total: {sequenceList.Count:N0} sequence(s)");
        Console.WriteLine("\nTip: Use 'kit sequence stats <id>' for email performance details.");
    }

    private static void PrintSequenceEmails(IEnumerable<SequenceEmail> emails)
    {
        var emailList = emails.OrderBy(e => e.Position).ToList();

        if (!emailList.Any())
        {
            Console.WriteLine("No emails found in sequence.");
            return;
        }

        Console.WriteLine("\nSequence Emails:");
        Console.WriteLine(new string('─', 80));

        foreach (var email in emailList)
        {
            Console.WriteLine($"\n{email.Position}. {TerminalText.RenderSingleLine(email.Subject)}");
            Console.WriteLine($"   Delay: {TerminalText.RenderSingleLine(email.DelayFormatted)}");
            Console.WriteLine($"   Sender: {TerminalText.RenderSingleLine(email.EmailAddress)}");
            Console.WriteLine($"   Published: {(email.Published ? "Yes" : "No")}");

            if (email.EmailTemplateId.HasValue)
            {
                Console.WriteLine($"   Template ID: {email.EmailTemplateId.Value}");
            }

            if (email.SendDays is { Length: > 0 })
            {
                Console.WriteLine($"   Send Days: {TerminalText.RenderSingleLine(email.SendDaysFormatted)}");
            }

            if (!string.IsNullOrEmpty(email.Content))
            {
                Console.WriteLine("   Content:");
                Console.WriteLine("   ┌─");
                var content = TerminalText.RenderMultiline(email.Content);
                foreach (var line in content.Split('\n'))
                {
                    Console.WriteLine($"   │ {line}");
                }
                Console.WriteLine("   └─");
            }

            if (email.Stats != null)
            {
                Console.WriteLine($"   Recipients: {email.Stats.Recipients:N0}");
                Console.WriteLine($"   Opens: {email.Stats.Opens:N0} ({email.Stats.OpenRate:F2}%)");
                Console.WriteLine($"   Clicks: {email.Stats.Clicks:N0} ({email.Stats.ClickRate:F2}%)");

                if (email.Stats.EmailUnsubscribes > 0)
                {
                    Console.WriteLine($"   Unsubscribes: {email.Stats.EmailUnsubscribes} ({email.Stats.UnsubscribeRate:F2}%)");
                }
            }
        }
    }

    private static void PrintSequenceSubscribers(IEnumerable<SequenceSubscriber> subscribers, string format)
    {
        var subscriberList = subscribers.ToList();

        if (format.Equals("json", StringComparison.OrdinalIgnoreCase))
        {
            var json = System.Text.Json.JsonSerializer.Serialize(
                subscriberList.ToArray(),
                KitJsonContext.Default.SequenceSubscriberArray);
            Console.WriteLine(json);
            return;
        }

        if (!subscriberList.Any())
        {
            Console.WriteLine("No subscribers found in sequence.");
            return;
        }

        // Table format (simplified)
        Console.WriteLine($"\n{"Email",-40} {"State",-12} {"Added",-20}");
        Console.WriteLine(new string('─', 75));

        foreach (var sub in subscriberList.Take(50))
        {
            var email = TruncateString(TerminalText.RenderSingleLine(sub.EmailAddress), 40);
            var state = TerminalText.RenderSingleLine(sub.State);
            var addedAt = sub.AddedAt.ToString("yyyy-MM-dd HH:mm");

            Console.WriteLine($"{email,-40} {state,-12} {addedAt,-20}");
        }

        if (subscriberList.Count > 50)
        {
            Console.WriteLine($"\n... and {subscriberList.Count - 50:N0} more");
        }
    }

    private static async Task ExportSequenceSubscribers(List<SequenceSubscriber> subscribers, string outputPath)
    {
        var format = Path.GetExtension(outputPath).ToLowerInvariant() switch
        {
            ".json" => "json",
            ".csv" => "csv",
            _ => "csv"
        };

        if (!outputPath.Contains('.'))
        {
            outputPath += ".csv";
        }

        using var writer = new StreamWriter(outputPath);

        if (format == "json")
        {
            var json = System.Text.Json.JsonSerializer.Serialize(
                subscribers.ToArray(),
                KitJsonContext.Default.SequenceSubscriberArray);
            await writer.WriteAsync(json);
        }
        else
        {
            await writer.WriteLineAsync("id,email_address,first_name,state,created_at,added_at");

            foreach (var sub in subscribers)
            {
                var email = EscapeCsvField(sub.EmailAddress);
                var name = EscapeCsvField(sub.FirstName ?? "");
                await writer.WriteLineAsync(
                    $"{sub.Id},{email},{name},{sub.State}," +
                    $"{sub.CreatedAt:yyyy-MM-dd'T'HH:mm:ss'Z'},{sub.AddedAt:yyyy-MM-dd'T'HH:mm:ss'Z'}");
            }
        }
    }

    private static string TruncateString(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (value.Length <= maxLength)
        {
            return value;
        }

        return value[..(maxLength - 3)] + "...";
    }

    private static string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field))
        {
            return "";
        }

        field = field.Replace("\"", "\"\"");

        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field}\"";
        }

        return field;
    }
}
