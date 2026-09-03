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
                    if (i + 1 >= args.Length) { Console.WriteLine("Missing value for --subject."); return 1; }
                    if (subject != null) { Console.WriteLine("Duplicate --subject flag."); return 1; }
                    subject = args[++i];
                    break;
                case "--content-file":
                    if (i + 1 >= args.Length) { Console.WriteLine("Missing value for --content-file."); return 1; }
                    if (contentFile != null) { Console.WriteLine("Duplicate --content-file flag."); return 1; }
                    contentFile = args[++i];
                    break;
                case "--expect-subject":
                    if (i + 1 >= args.Length) { Console.WriteLine("Missing value for --expect-subject."); return 1; }
                    expectSubject = args[++i];
                    break;
                case "--expect-content-sha256":
                    if (i + 1 >= args.Length) { Console.WriteLine("Missing value for --expect-content-sha256."); return 1; }
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
                    if (i + 1 >= args.Length) { Console.WriteLine("Missing value for --format."); return 1; }
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
        catch (HttpRequestException ex)
        {
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
        catch (HttpRequestException ex)
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

        if (before.Position != after.Position) return "position changed unexpectedly";
        if (before.Published != after.Published) return "published changed unexpectedly";
        if (before.DelayValue != after.DelayValue) return "delay_value changed unexpectedly";
        if (!string.Equals(before.DelayUnit, after.DelayUnit, StringComparison.Ordinal)) return "delay_unit changed unexpectedly";
        if (before.EmailTemplateId != after.EmailTemplateId) return "email_template_id changed unexpectedly";
        if (!string.Equals(before.EmailAddress, after.EmailAddress, StringComparison.Ordinal)) return "email_address changed unexpectedly";
        if (!string.Equals(before.PreviewText, after.PreviewText, StringComparison.Ordinal)) return "preview_text changed unexpectedly";
        if (!SendDaysEqual(before.SendDays, after.SendDays)) return "send_days changed unexpectedly";

        return null;
    }

    private static bool SendDaysEqual(string[]? a, string[]? b)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;
        if (a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++)
        {
            if (!string.Equals(a[i], b[i], StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static string Sha256Hex(string value)
    {
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

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
