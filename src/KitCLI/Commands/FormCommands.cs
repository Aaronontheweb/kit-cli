using System.Text.Json;
using KitCLI.Helpers;
using KitCLI.Models;
using KitCLI.Services;

namespace KitCLI.Commands;

/// <summary>
/// Commands for managing Kit forms (landing pages/opt-in forms)
/// </summary>
public static class FormCommands
{
    public static async Task<int> HandleList(string[] args, IKitApiClient client)
    {
        var format = "table";
        var includeArchived = false;
        var limit = 100;

        // Parse arguments
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLowerInvariant())
            {
                case "--format":
                case "-f":
                    if (i + 1 < args.Length)
                    {
                        format = args[++i].ToLowerInvariant();
                    }
                    break;
                case "--include-archived":
                    includeArchived = true;
                    break;
                case "--limit":
                case "-l":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out var l))
                    {
                        limit = Math.Min(l, 500);
                    }
                    break;
            }
        }

        try
        {
            Console.WriteLine("Fetching forms...");
            var forms = new List<Form>();

            await foreach (var batch in client.GetAllFormsAsync(limit))
            {
                forms.Add(batch);
            }

            // Filter out archived unless requested
            if (!includeArchived)
            {
                forms = forms.Where(f => !f.Archived).ToList();
            }

            PrintForms(forms, format);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"❌ Failed to fetch forms: {ex.Message}");
            return 1;
        }
    }

    public static async Task<int> HandleGet(string[] args, IKitApiClient client)
    {
        if (args.Length < 1)
        {
            Console.Error.WriteLine("❌ Form ID is required");
            Console.WriteLine("Usage: kit form get <id>");
            return 1;
        }

        if (!long.TryParse(args[0], out var formId))
        {
            Console.Error.WriteLine("❌ Invalid form ID");
            return 1;
        }

        try
        {
            var form = await client.GetFormAsync(formId);
            if (form == null)
            {
                Console.Error.WriteLine($"❌ Form {formId} not found");
                return 1;
            }

            PrintFormDetails(form);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"❌ Failed to fetch form {formId}: {ex.Message}");
            return 1;
        }
    }

    public static async Task<int> HandleSubscribers(string[] args, IKitApiClient client)
    {
        if (args.Length < 1)
        {
            Console.Error.WriteLine("❌ Form ID is required");
            Console.WriteLine("Usage: kit form subscribers <id> [options]");
            return 1;
        }

        if (!long.TryParse(args[0], out var formId))
        {
            Console.Error.WriteLine("❌ Invalid form ID");
            return 1;
        }

        var format = "table";
        var limit = 100;

        // Parse arguments
        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i].ToLowerInvariant())
            {
                case "--format":
                case "-f":
                    if (i + 1 < args.Length)
                    {
                        format = args[++i].ToLowerInvariant();
                    }
                    break;
                case "--limit":
                case "-l":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out var l))
                    {
                        limit = Math.Min(l, 1000);
                    }
                    break;
            }
        }

        try
        {
            Console.WriteLine($"Fetching subscribers for form {formId}...");
            var subscribers = new List<Subscriber>();

            await foreach (var batch in client.GetAllFormSubscribersAsync(formId, limit))
            {
                subscribers.Add(batch);
            }

            OutputFormatter.PrintSubscribers(subscribers, format);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"❌ Failed to fetch subscribers for form {formId}: {ex.Message}");
            return 1;
        }
    }

    public static async Task<int> HandleCompare(string[] args, IKitApiClient client)
    {
        var formIds = new List<long>();
        string format = "table";
        string? exportPath = null;

        // Parse form IDs and options
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--format":
                case "-f":
                    if (i + 1 < args.Length)
                    {
                        format = args[++i].ToLowerInvariant();
                    }
                    break;
                case "--export":
                case "-o":
                    if (i + 1 < args.Length)
                    {
                        exportPath = args[++i];
                    }
                    break;
                default:
                    // Try to parse as form ID
                    if (long.TryParse(args[i], out var formId))
                    {
                        formIds.Add(formId);
                    }
                    break;
            }
        }

        if (formIds.Count < 2)
        {
            Console.Error.WriteLine("❌ At least 2 form IDs are required for comparison");
            Console.WriteLine("Usage: kit form compare <id1> <id2> [id3...] [options]");
            Console.WriteLine("Options:");
            Console.WriteLine("  --format, -f <format>   Output format: table (default), json, csv");
            Console.WriteLine("  --export, -o <path>     Export to file");
            return 1;
        }

        using var progress = new ProgressIndicator($"Comparing {formIds.Count} forms...");

        // Fetch forms and their subscribers in parallel
        var comparisonItems = new List<FormComparisonItem>();

        foreach (var id in formIds)
        {
            var form = await client.GetFormAsync(id);
            if (form == null)
            {
                Console.Error.WriteLine($"⚠ Form {id} not found, skipping");
                continue;
            }

            var subscribers = new List<Subscriber>();
            await foreach (var sub in client.GetAllFormSubscribersAsync(id, 1000))
            {
                subscribers.Add(sub);
            }

            var now = DateTime.UtcNow;
            var thirtyDaysAgo = now.AddDays(-30);
            var ninetyDaysAgo = now.AddDays(-90);

            var activeCount = subscribers.Count(s => s.State.Equals("active", StringComparison.OrdinalIgnoreCase));
            var cancelledCount = subscribers.Count(s => s.State.Equals("cancelled", StringComparison.OrdinalIgnoreCase));
            var bouncedCount = subscribers.Count(s => s.State.Equals("bounced", StringComparison.OrdinalIgnoreCase));
            var complainedCount = subscribers.Count(s => s.State.Equals("complained", StringComparison.OrdinalIgnoreCase));
            var signups30d = subscribers.Count(s => s.CreatedAt >= thirtyDaysAgo);
            var signups90d = subscribers.Count(s => s.CreatedAt >= ninetyDaysAgo);

            var ageDays = (int)(now - form.CreatedAt).TotalDays;
            var dailyAverage = ageDays > 0 ? (double)subscribers.Count / ageDays : 0;

            comparisonItems.Add(new FormComparisonItem
            {
                FormId = form.Id,
                FormName = form.Name ?? $"Form {form.Id}",
                FormType = form.Type ?? "unknown",
                TotalSubscribers = subscribers.Count,
                ActiveSubscribers = activeCount,
                CancelledSubscribers = cancelledCount,
                BouncedSubscribers = bouncedCount,
                ComplainedSubscribers = complainedCount,
                RetentionRate = subscribers.Count > 0 ? (double)activeCount / subscribers.Count * 100 : 0,
                Signups30d = signups30d,
                Signups90d = signups90d,
                DailyAverage = dailyAverage,
                CreatedAt = form.CreatedAt,
                AgeDays = ageDays,
                Archived = form.Archived
            });
        }

        if (comparisonItems.Count < 2)
        {
            progress.Complete("Not enough forms found for comparison");
            Console.Error.WriteLine("❌ Need at least 2 valid forms for comparison");
            return 1;
        }

        progress.Complete($"Comparing {comparisonItems.Count} forms");

        // Determine winner (highest subscriber count with best retention)
        var winner = comparisonItems
            .OrderByDescending(f => f.TotalSubscribers)
            .ThenByDescending(f => f.RetentionRate)
            .First();

        var winnerReason = $"highest subscriber count ({winner.TotalSubscribers:N0}) and {winner.RetentionRate:F1}% retention";

        var result = new FormComparisonResult
        {
            Forms = comparisonItems.ToArray(),
            WinnerFormId = winner.FormId,
            WinnerFormName = winner.FormName,
            WinnerReason = winnerReason
        };

        // Output
        if (!string.IsNullOrEmpty(exportPath))
        {
            await ExportComparison(result, exportPath);
            Console.WriteLine($"✓ Comparison exported to {exportPath}");
        }

        switch (format)
        {
            case "json":
                var json = JsonSerializer.Serialize(result, KitJsonIndentedContext.Default.FormComparisonResult);
                Console.WriteLine(json);
                break;
            case "csv":
                PrintComparisonCsv(result);
                break;
            default:
                PrintComparisonTable(result);
                break;
        }

        return 0;
    }

    private static void PrintComparisonTable(FormComparisonResult result)
    {
        var forms = result.Forms;
        var colWidth = Math.Max(15, forms.Max(f => f.FormName.Length) + 2);

        Console.WriteLine();
        Console.WriteLine("═══════════════════════════════════════════════════════════════════════════════");
        Console.WriteLine("  FORM COMPARISON");
        Console.WriteLine("═══════════════════════════════════════════════════════════════════════════════");
        Console.WriteLine();

        // Header with form names
        Console.Write($"  {"Metric",-20}");
        foreach (var form in forms)
        {
            var name = form.FormName.Length > colWidth - 2 ? form.FormName[..(colWidth - 5)] + "..." : form.FormName;
            Console.Write($"  {name.PadLeft(colWidth)}");
        }
        Console.WriteLine();
        Console.WriteLine($"  {new string('─', 20 + (colWidth + 2) * forms.Length)}");

        // Subscriber counts
        PrintComparisonRow("Total Subscribers", forms.Select(f => f.TotalSubscribers.ToString("N0")), colWidth);
        PrintComparisonRow("Active", forms.Select(f => f.ActiveSubscribers.ToString("N0")), colWidth);
        PrintComparisonRow("Cancelled", forms.Select(f => f.CancelledSubscribers.ToString("N0")), colWidth);
        PrintComparisonRow("Bounced", forms.Select(f => f.BouncedSubscribers.ToString("N0")), colWidth);
        PrintComparisonRow("Retention Rate", forms.Select(f => $"{f.RetentionRate:F1}%"), colWidth);

        Console.WriteLine($"  {new string('─', 20 + (colWidth + 2) * forms.Length)}");

        // Activity metrics
        PrintComparisonRow("Signups (30d)", forms.Select(f => f.Signups30d.ToString("N0")), colWidth);
        PrintComparisonRow("Signups (90d)", forms.Select(f => f.Signups90d.ToString("N0")), colWidth);
        PrintComparisonRow("Daily Average", forms.Select(f => f.DailyAverage.ToString("F1")), colWidth);

        Console.WriteLine($"  {new string('─', 20 + (colWidth + 2) * forms.Length)}");

        // Form info
        PrintComparisonRow("Form Type", forms.Select(f => f.FormType), colWidth);
        PrintComparisonRow("Created", forms.Select(f => f.CreatedAt.ToString("yyyy-MM-dd")), colWidth);
        PrintComparisonRow("Age (days)", forms.Select(f => f.AgeDays.ToString("N0")), colWidth);

        Console.WriteLine();
        Console.WriteLine($"═══════════════════════════════════════════════════════════════════════════════");
        Console.WriteLine();
        Console.WriteLine($"  Winner: \"{result.WinnerFormName}\" - {result.WinnerReason}");
        Console.WriteLine();
    }

    private static void PrintComparisonRow(string label, IEnumerable<string> values, int colWidth)
    {
        Console.Write($"  {label,-20}");
        foreach (var value in values)
        {
            Console.Write($"  {value.PadLeft(colWidth)}");
        }
        Console.WriteLine();
    }

    private static void PrintComparisonCsv(FormComparisonResult result)
    {
        Console.WriteLine("form_id,form_name,form_type,total_subscribers,active_subscribers,cancelled_subscribers,bounced_subscribers,retention_rate,signups_30d,signups_90d,daily_average,created_at,age_days");

        foreach (var form in result.Forms)
        {
            var name = EscapeCsvField(form.FormName);
            Console.WriteLine($"{form.FormId},{name},{form.FormType},{form.TotalSubscribers},{form.ActiveSubscribers},{form.CancelledSubscribers},{form.BouncedSubscribers},{form.RetentionRate:F2},{form.Signups30d},{form.Signups90d},{form.DailyAverage:F2},{form.CreatedAt:yyyy-MM-dd},{form.AgeDays}");
        }
    }

    private static async Task ExportComparison(FormComparisonResult result, string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();

        await using var writer = new StreamWriter(path);

        if (extension == ".json")
        {
            var json = JsonSerializer.Serialize(result, KitJsonIndentedContext.Default.FormComparisonResult);
            await writer.WriteAsync(json);
        }
        else // CSV
        {
            await writer.WriteLineAsync("form_id,form_name,form_type,total_subscribers,active_subscribers,cancelled_subscribers,bounced_subscribers,retention_rate,signups_30d,signups_90d,daily_average,created_at,age_days");

            foreach (var form in result.Forms)
            {
                var name = EscapeCsvField(form.FormName);
                await writer.WriteLineAsync($"{form.FormId},{name},{form.FormType},{form.TotalSubscribers},{form.ActiveSubscribers},{form.CancelledSubscribers},{form.BouncedSubscribers},{form.RetentionRate:F2},{form.Signups30d},{form.Signups90d},{form.DailyAverage:F2},{form.CreatedAt:yyyy-MM-dd},{form.AgeDays}");
            }
        }
    }

    private static void PrintForms(IEnumerable<Form> forms, string format)
    {
        var formList = forms.ToList();

        if (!formList.Any())
        {
            Console.WriteLine("No forms found.");
            return;
        }

        switch (format.ToLowerInvariant())
        {
            case "json":
                PrintFormsJson(formList);
                break;
            case "csv":
                PrintFormsCsv(formList);
                break;
            default:
                PrintFormsTable(formList);
                break;
        }
    }

    private static void PrintFormsTable(List<Form> forms)
    {
        // Calculate column widths
        const int idWidth = 10;
        var maxNameLength = forms.Any() ? forms.Max(f => f.Name?.Length ?? 0) : 0;
        var nameWidth = Math.Max(20, maxNameLength);
        const int typeWidth = 15;
        const int subsWidth = 12;
        const int createdWidth = 10;
        var totalWidth = idWidth + nameWidth + typeWidth + subsWidth + createdWidth + 10;

        // Header
        Console.WriteLine(new string('─', totalWidth));
        Console.WriteLine($"│ {"ID".PadRight(idWidth)} │ {"Name".PadRight(nameWidth)} │ {"Type".PadRight(typeWidth)} │ {"Subscribers".PadRight(subsWidth)} │ {"Created".PadRight(createdWidth)} │");
        Console.WriteLine(new string('─', totalWidth));

        // Data rows
        foreach (var form in forms)
        {
            var name = form.Name?.Length > nameWidth
                ? form.Name.Substring(0, nameWidth - 3) + "..."
                : form.Name ?? "";
            var type = form.Type ?? "unknown";
            var created = form.CreatedAt.ToString("yyyy-MM-dd");

            var subscriptionsText = form.TotalSubscriptions.ToString("N0");
            Console.WriteLine($"│ {form.Id.ToString().PadRight(idWidth)} │ {name.PadRight(nameWidth)} │ {type.PadRight(typeWidth)} │ {subscriptionsText.PadRight(subsWidth)} │ {created.PadRight(createdWidth)} │");
        }

        // Footer
        Console.WriteLine(new string('─', totalWidth));
        Console.WriteLine($"Total: {forms.Count:N0} form(s), {forms.Sum(f => f.TotalSubscriptions):N0} total subscribers");
    }

    private static void PrintFormsJson(IEnumerable<Form> forms)
    {
        var json = JsonSerializer.Serialize(forms.ToArray(), KitJsonIndentedContext.Default.FormArray);
        Console.WriteLine(json);
    }

    private static void PrintFormsCsv(IEnumerable<Form> forms)
    {
        Console.WriteLine("id,name,type,format,total_subscriptions,archived,created_at,embed_url");

        foreach (var form in forms)
        {
            var name = EscapeCsvField(form.Name);
            var type = EscapeCsvField(form.Type);
            var format = EscapeCsvField(form.Format);
            var embedUrl = EscapeCsvField(form.EmbedUrl ?? "");

            Console.WriteLine($"{form.Id},{name},{type},{format},{form.TotalSubscriptions},{form.Archived},{form.CreatedAt:yyyy-MM-dd'T'HH:mm:ss'Z'},{embedUrl}");
        }
    }

    private static void PrintFormDetails(Form form)
    {
        Console.WriteLine();
        Console.WriteLine($"Form Details (ID: {form.Id})");
        Console.WriteLine(new string('═', 50));
        Console.WriteLine($"Name:         {form.Name}");
        Console.WriteLine($"Type:         {form.Type}");
        Console.WriteLine($"Format:       {form.Format}");
        Console.WriteLine($"Subscribers:  {form.TotalSubscriptions:N0}");
        Console.WriteLine($"Archived:     {(form.Archived ? "Yes" : "No")}");
        Console.WriteLine($"Created:      {form.CreatedAt:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"Updated:      {form.UpdatedAt:yyyy-MM-dd HH:mm:ss}");

        if (!string.IsNullOrEmpty(form.Description))
        {
            Console.WriteLine($"Description:  {form.Description}");
        }

        if (!string.IsNullOrEmpty(form.EmbedUrl))
        {
            Console.WriteLine($"Embed URL:    {form.EmbedUrl}");
        }

        if (!string.IsNullOrEmpty(form.RedirectUrl))
        {
            Console.WriteLine($"Redirect URL: {form.RedirectUrl}");
        }

        if (form.IncentiveEmail?.Enabled == true)
        {
            Console.WriteLine();
            Console.WriteLine("Incentive Email:");
            Console.WriteLine($"  Subject: {form.IncentiveEmail.Subject}");
            if (!string.IsNullOrEmpty(form.IncentiveEmail.Body))
            {
                var preview = form.IncentiveEmail.Body.Length > 100
                    ? form.IncentiveEmail.Body.Substring(0, 97) + "..."
                    : form.IncentiveEmail.Body;
                Console.WriteLine($"  Preview: {preview}");
            }
        }
    }

    private static string EscapeCsvField(string? field)
    {
        if (string.IsNullOrEmpty(field))
        {
            return "";
        }

        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return "\"" + field.Replace("\"", "\"\"") + "\"";
        }

        return field;
    }

    public static async Task<int> HandleTrends(string[] args, IKitApiClient client)
    {
        int days = 365;
        string groupBy = "monthly";
        string format = "table";
        string? exportPath = null;
        var formIds = new List<long>();

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--days":
                case "-d":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out var d))
                    {
                        days = d;
                    }
                    break;
                case "--period":
                case "--group-by":
                case "-g":
                    if (i + 1 < args.Length)
                    {
                        groupBy = args[++i].ToLowerInvariant();
                        if (groupBy != "daily" && groupBy != "weekly" && groupBy != "monthly")
                        {
                            Console.WriteLine("Invalid period value. Use: daily, weekly, or monthly");
                            return 1;
                        }
                    }
                    break;
                case "--form-ids":
                    if (i + 1 < args.Length)
                    {
                        var ids = args[++i].Split(',', StringSplitOptions.RemoveEmptyEntries);
                        foreach (var id in ids)
                        {
                            if (long.TryParse(id.Trim(), out var formId))
                            {
                                formIds.Add(formId);
                            }
                        }
                    }
                    break;
                case "--format":
                case "-f":
                    if (i + 1 < args.Length)
                    {
                        format = args[++i];
                    }
                    break;
                case "--export":
                case "-o":
                    if (i + 1 < args.Length)
                    {
                        exportPath = args[++i];
                    }
                    break;
            }
        }

        using var progress = new ProgressIndicator($"Analyzing form signup trends over {days} days");

        // Calculate date range
        var endDate = DateTime.UtcNow;
        var startDate = endDate.AddDays(-days);

        // Fetch all forms
        var forms = new List<Form>();
        await foreach (var form in client.GetAllFormsAsync(500))
        {
            // Filter by form IDs if specified
            if (formIds.Count == 0 || formIds.Contains(form.Id))
            {
                if (!form.Archived) // Only include active forms
                {
                    forms.Add(form);
                }
            }
        }

        if (forms.Count == 0)
        {
            progress.Complete("No forms found");
            return 0;
        }

        progress.Complete($"Found {forms.Count} forms, fetching subscriber data...");

        // Fetch subscribers for each form and build trend data
        var formTrends = new List<FormTrendData>();
        var totalSignups = 0;

        foreach (var form in forms)
        {
            using var formProgress = new ProgressIndicator($"Analyzing form: {form.Name}");

            var subscribers = new List<Subscriber>();
            await foreach (var sub in client.GetAllFormSubscribersAsync(form.Id, 1000))
            {
                // Filter to subscribers in our date range
                if (sub.CreatedAt >= startDate && sub.CreatedAt <= endDate)
                {
                    subscribers.Add(sub);
                }
            }

            if (subscribers.Count == 0)
            {
                formProgress.Complete($"No signups in date range");
                continue;
            }

            // Group subscribers by period
            var periods = GroupSubscribersByPeriod(subscribers, groupBy, startDate, endDate);

            // Calculate trend
            var trendData = CalculateFormTrend(form, subscribers, periods, days);
            formTrends.Add(trendData);
            totalSignups += trendData.TotalSignups;

            formProgress.Complete($"{subscribers.Count} signups analyzed");
        }

        if (formTrends.Count == 0)
        {
            Console.WriteLine("No signup data found in the specified date range.");
            return 0;
        }

        // Sort by total signups descending
        formTrends = formTrends.OrderByDescending(f => f.TotalSignups).ToList();

        // Determine overall trend
        var overallTrend = "stable";
        if (formTrends.Count > 0)
        {
            var improvingCount = formTrends.Count(f => f.TrendDirection == "improving");
            var decliningCount = formTrends.Count(f => f.TrendDirection == "declining");
            if (improvingCount > decliningCount * 2)
            {
                overallTrend = "improving";
            }
            else if (decliningCount > improvingCount * 2)
            {
                overallTrend = "declining";
            }
        }

        // Build result
        var result = new FormTrendResult
        {
            Days = days,
            GroupBy = groupBy,
            StartDate = startDate,
            EndDate = endDate,
            TotalSignups = totalSignups,
            Forms = formTrends.ToArray(),
            OverallTrend = overallTrend,
            BestPerformingForm = formTrends.FirstOrDefault()?.FormName,
            BestPerformingFormId = formTrends.FirstOrDefault()?.FormId
        };

        // Handle export
        if (!string.IsNullOrEmpty(exportPath))
        {
            await ExportFormTrends(result, exportPath);
            Console.WriteLine($"✓ Exported trends to {exportPath}");
        }

        // Output
        if (format.Equals("json", StringComparison.OrdinalIgnoreCase))
        {
            var json = System.Text.Json.JsonSerializer.Serialize(
                result,
                KitJsonIndentedContext.Default.FormTrendResult);
            Console.WriteLine(json);
        }
        else if (format.Equals("csv", StringComparison.OrdinalIgnoreCase))
        {
            PrintTrendsCsv(result);
        }
        else
        {
            PrintTrendsTable(result);
        }

        return 0;
    }

    private static List<FormTrendPeriod> GroupSubscribersByPeriod(
        List<Subscriber> subscribers,
        string groupBy,
        DateTime startDate,
        DateTime endDate)
    {
        var periods = new List<FormTrendPeriod>();
        var currentDate = startDate;

        while (currentDate < endDate)
        {
            DateTime periodEnd;
            string periodLabel;

            switch (groupBy)
            {
                case "daily":
                    periodEnd = currentDate.AddDays(1);
                    periodLabel = currentDate.ToString("yyyy-MM-dd");
                    break;
                case "weekly":
                    periodEnd = currentDate.AddDays(7);
                    periodLabel = $"Week of {currentDate:MMM d}";
                    break;
                default: // monthly
                    periodEnd = currentDate.AddMonths(1);
                    periodLabel = currentDate.ToString("MMM yyyy");
                    break;
            }

            if (periodEnd > endDate)
            {
                periodEnd = endDate;
            }

            var periodSubs = subscribers
                .Where(s => s.CreatedAt >= currentDate && s.CreatedAt < periodEnd)
                .ToList();

            var activeSubs = periodSubs.Count(s => s.State.Equals("active", StringComparison.OrdinalIgnoreCase));
            var retentionRate = periodSubs.Count > 0 ? (double)activeSubs / periodSubs.Count * 100 : 0;

            periods.Add(new FormTrendPeriod
            {
                Period = periodLabel,
                StartDate = currentDate,
                EndDate = periodEnd,
                Signups = periodSubs.Count,
                ActiveSubscribers = activeSubs,
                RetentionRate = retentionRate
            });

            currentDate = periodEnd;
        }

        return periods;
    }

    private static FormTrendData CalculateFormTrend(
        Form form,
        List<Subscriber> subscribers,
        List<FormTrendPeriod> periods,
        int days)
    {
        var activeSubs = subscribers.Count(s => s.State.Equals("active", StringComparison.OrdinalIgnoreCase));
        var retentionRate = subscribers.Count > 0 ? (double)activeSubs / subscribers.Count * 100 : 0;
        var avgDaily = (double)subscribers.Count / days;

        // Calculate trend direction by comparing first half to second half
        var trendDirection = "stable";
        var trendChange = 0.0;

        var nonEmptyPeriods = periods.Where(p => p.Signups > 0).ToList();
        if (nonEmptyPeriods.Count >= 2)
        {
            var midpoint = nonEmptyPeriods.Count / 2;
            var firstHalf = nonEmptyPeriods.Take(midpoint).Sum(p => p.Signups);
            var secondHalf = nonEmptyPeriods.Skip(midpoint).Sum(p => p.Signups);

            if (firstHalf > 0)
            {
                trendChange = ((double)secondHalf - firstHalf) / firstHalf * 100;

                if (trendChange > 10)
                {
                    trendDirection = "improving";
                }
                else if (trendChange < -10)
                {
                    trendDirection = "declining";
                }
            }
        }

        return new FormTrendData
        {
            FormId = form.Id,
            FormName = form.Name ?? $"Form {form.Id}",
            FormType = form.Type ?? "unknown",
            TotalSignups = subscribers.Count,
            ActiveSubscribers = activeSubs,
            RetentionRate = retentionRate,
            AverageDailySignups = avgDaily,
            TrendDirection = trendDirection,
            TrendChangePercent = trendChange,
            Periods = periods.ToArray()
        };
    }

    private static void PrintTrendsTable(FormTrendResult result)
    {
        Console.WriteLine();
        Console.WriteLine("Form Signup Trends");
        Console.WriteLine(new string('═', 90));
        Console.WriteLine($"Period: {result.StartDate:yyyy-MM-dd} to {result.EndDate:yyyy-MM-dd} ({result.Days} days)");
        Console.WriteLine($"Total Signups: {result.TotalSignups:N0}");
        Console.WriteLine($"Overall Trend: {result.OverallTrend}");
        Console.WriteLine();

        // Header
        Console.WriteLine($"{"Form",-30} {"Type",-12} {"Signups",10} {"Active",10} {"Retention",10} {"Trend",12}");
        Console.WriteLine(new string('─', 90));

        foreach (var form in result.Forms)
        {
            var formName = form.FormName.Length > 27
                ? form.FormName[..24] + "..."
                : form.FormName;

            var trendIndicator = form.TrendDirection switch
            {
                "improving" => $"↑ {form.TrendChangePercent:+0.0}%",
                "declining" => $"↓ {form.TrendChangePercent:0.0}%",
                _ => "→ stable"
            };

            Console.WriteLine($"{formName,-30} {form.FormType,-12} {form.TotalSignups,10:N0} {form.ActiveSubscribers,10:N0} {form.RetentionRate,9:F1}% {trendIndicator,12}");
        }

        Console.WriteLine(new string('─', 90));

        if (result.BestPerformingForm != null)
        {
            Console.WriteLine();
            Console.WriteLine($"Best Performing: {result.BestPerformingForm}");
        }
        Console.WriteLine();
    }

    private static void PrintTrendsCsv(FormTrendResult result)
    {
        Console.WriteLine("form_id,form_name,form_type,total_signups,active_subscribers,retention_rate,avg_daily_signups,trend_direction,trend_change_percent");

        foreach (var form in result.Forms)
        {
            var name = EscapeCsvField(form.FormName);
            Console.WriteLine($"{form.FormId},{name},{form.FormType},{form.TotalSignups},{form.ActiveSubscribers},{form.RetentionRate:F2},{form.AverageDailySignups:F2},{form.TrendDirection},{form.TrendChangePercent:F2}");
        }
    }

    private static async Task ExportFormTrends(FormTrendResult result, string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();

        await using var writer = new StreamWriter(path);

        if (extension == ".json")
        {
            var json = System.Text.Json.JsonSerializer.Serialize(
                result,
                KitJsonIndentedContext.Default.FormTrendResult);
            await writer.WriteAsync(json);
        }
        else
        {
            // CSV
            await writer.WriteLineAsync("form_id,form_name,form_type,total_signups,active_subscribers,retention_rate,avg_daily_signups,trend_direction,trend_change_percent");

            foreach (var form in result.Forms)
            {
                var name = EscapeCsvField(form.FormName);
                await writer.WriteLineAsync($"{form.FormId},{name},{form.FormType},{form.TotalSignups},{form.ActiveSubscribers},{form.RetentionRate:F2},{form.AverageDailySignups:F2},{form.TrendDirection},{form.TrendChangePercent:F2}");
            }
        }
    }
}
