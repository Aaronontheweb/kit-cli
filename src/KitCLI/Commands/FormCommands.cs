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

        // Build result
        var result = new FormTrendResult
        {
            Days = days,
            GroupBy = groupBy,
            StartDate = startDate,
            EndDate = endDate,
            TotalSignups = totalSignups,
            FormCount = formTrends.Count,
            Forms = formTrends.ToArray(),
            Insight = GenerateInsight(formTrends)
        };

        // Output
        if (!string.IsNullOrEmpty(exportPath))
        {
            await ExportTrends(result, exportPath);
            Console.WriteLine($"✓ Trends exported to {exportPath}");
        }

        if (format == "json")
        {
            var json = JsonSerializer.Serialize(result, KitJsonIndentedContext.Default.FormTrendResult);
            Console.WriteLine(json);
        }
        else
        {
            PrintTrends(result);
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
        var current = startDate;

        while (current < endDate)
        {
            DateTime periodEnd;
            string label;

            switch (groupBy)
            {
                case "daily":
                    periodEnd = current.AddDays(1);
                    label = current.ToString("yyyy-MM-dd");
                    break;
                case "weekly":
                    var daysUntilMonday = ((int)current.DayOfWeek - 1 + 7) % 7;
                    var weekStart = current.AddDays(-daysUntilMonday);
                    if (weekStart < startDate) weekStart = startDate;
                    periodEnd = weekStart.AddDays(7);
                    if (periodEnd > endDate) periodEnd = endDate;
                    label = $"Week of {weekStart:yyyy-MM-dd}";
                    current = weekStart;
                    break;
                case "monthly":
                default:
                    periodEnd = new DateTime(current.Year, current.Month, 1).AddMonths(1);
                    label = current.ToString("yyyy-MM");
                    break;
            }

            if (periodEnd > endDate) periodEnd = endDate;

            var signups = subscribers.Count(s => s.CreatedAt >= current && s.CreatedAt < periodEnd);

            periods.Add(new FormTrendPeriod
            {
                Period = label,
                StartDate = current,
                EndDate = periodEnd,
                Signups = signups
            });

            current = periodEnd;
        }

        return periods;
    }

    private static FormTrendData CalculateFormTrend(
        Form form,
        List<Subscriber> subscribers,
        List<FormTrendPeriod> periods,
        int days)
    {
        var nonEmptyPeriods = periods.Where(p => p.Signups > 0).ToList();
        var totalSignups = subscribers.Count;
        var dailyAverage = days > 0 ? (double)totalSignups / days : 0;
        var avgPerPeriod = nonEmptyPeriods.Count > 0 ? nonEmptyPeriods.Average(p => p.Signups) : 0;

        // Find peak period
        var peakPeriod = periods.OrderByDescending(p => p.Signups).FirstOrDefault();

        // Calculate trend (compare first half to second half)
        string trend = "stable";
        double trendChange = 0;

        if (nonEmptyPeriods.Count >= 2)
        {
            var midpoint = nonEmptyPeriods.Count / 2;
            var firstHalf = nonEmptyPeriods.Take(midpoint).ToList();
            var secondHalf = nonEmptyPeriods.Skip(midpoint).ToList();

            var firstHalfAvg = firstHalf.Average(p => p.Signups);
            var secondHalfAvg = secondHalf.Average(p => p.Signups);

            if (firstHalfAvg > 0)
            {
                trendChange = ((secondHalfAvg - firstHalfAvg) / firstHalfAvg) * 100;
            }

            trend = trendChange switch
            {
                > 10 => "improving",
                < -10 => "declining",
                _ => "stable"
            };
        }

        return new FormTrendData
        {
            FormId = form.Id,
            FormName = form.Name ?? $"Form {form.Id}",
            FormType = form.Type ?? "unknown",
            TotalSignups = totalSignups,
            AveragePerPeriod = avgPerPeriod,
            DailyAverage = dailyAverage,
            Trend = trend,
            TrendChange = trendChange,
            PeakPeriod = peakPeriod?.Period,
            PeakSignups = peakPeriod?.Signups ?? 0,
            Periods = periods.ToArray()
        };
    }

    private static string GenerateInsight(List<FormTrendData> forms)
    {
        if (forms.Count == 0)
            return "No form data available for analysis.";

        var topForm = forms[0];
        var improving = forms.Count(f => f.Trend == "improving");
        var declining = forms.Count(f => f.Trend == "declining");

        var insight = $"\"{topForm.FormName}\" is your top form with {topForm.TotalSignups:N0} signups";

        if (topForm.Trend == "improving")
            insight += $" and growing ({topForm.TrendChange:+0.0}%)";
        else if (topForm.Trend == "declining")
            insight += $" but declining ({topForm.TrendChange:+0.0}%)";

        insight += ".";

        if (improving > 0)
            insight += $" {improving} form(s) showing growth.";
        if (declining > 0)
            insight += $" {declining} form(s) need attention (declining signups).";

        return insight;
    }

    private static void PrintTrends(FormTrendResult result)
    {
        Console.WriteLine();
        Console.WriteLine($"═══════════════════════════════════════════════════════════════════════════════");
        Console.WriteLine($"  FORM SIGNUP TRENDS");
        Console.WriteLine($"═══════════════════════════════════════════════════════════════════════════════");
        Console.WriteLine();

        Console.WriteLine($"  Period:        {result.StartDate:yyyy-MM-dd} to {result.EndDate:yyyy-MM-dd} ({result.Days} days)");
        Console.WriteLine($"  Grouped by:    {result.GroupBy}");
        Console.WriteLine($"  Total signups: {result.TotalSignups:N0} across {result.FormCount} forms");
        Console.WriteLine();

        // Form summary table
        Console.WriteLine($"  ─────────────────────────────────────────────────────────────────────────────");
        Console.WriteLine($"  FORM PERFORMANCE");
        Console.WriteLine($"  ─────────────────────────────────────────────────────────────────────────────");
        Console.WriteLine();

        Console.WriteLine($"  {"Form",-30} {"Total",-10} {"Daily Avg",-12} {"Trend",-15} {"Peak",-15}");
        Console.WriteLine($"  {new string('─', 80)}");

        foreach (var form in result.Forms)
        {
            var name = form.FormName.Length > 28 ? form.FormName[..25] + "..." : form.FormName;
            var trendIcon = form.Trend switch
            {
                "improving" => "↑",
                "declining" => "↓",
                _ => "→"
            };
            var trendText = $"{trendIcon} {form.TrendChange:+0.0;-0.0;0.0}%";
            var peakText = form.PeakPeriod ?? "N/A";
            if (peakText.Length > 13) peakText = peakText[..13];

            Console.WriteLine($"  {name,-30} {form.TotalSignups,-10:N0} {form.DailyAverage,-12:F1} {trendText,-15} {peakText,-15}");
        }

        Console.WriteLine();

        // Insight
        if (!string.IsNullOrEmpty(result.Insight))
        {
            Console.WriteLine($"  ─────────────────────────────────────────────────────────────────────────────");
            Console.WriteLine($"  INSIGHTS");
            Console.WriteLine($"  ─────────────────────────────────────────────────────────────────────────────");
            Console.WriteLine();
            Console.WriteLine($"  {result.Insight}");
            Console.WriteLine();
        }

        Console.WriteLine($"═══════════════════════════════════════════════════════════════════════════════");
        Console.WriteLine();
    }

    private static async Task ExportTrends(FormTrendResult result, string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();

        await using var writer = new StreamWriter(path);

        if (extension == ".json")
        {
            var json = JsonSerializer.Serialize(result, KitJsonIndentedContext.Default.FormTrendResult);
            await writer.WriteAsync(json);
        }
        else // CSV
        {
            await writer.WriteLineAsync("form_id,form_name,form_type,total_signups,daily_average,trend,trend_change,peak_period,peak_signups");

            foreach (var form in result.Forms)
            {
                var name = EscapeCsvField(form.FormName);
                await writer.WriteLineAsync($"{form.FormId},{name},{form.FormType},{form.TotalSignups},{form.DailyAverage:F2},{form.Trend},{form.TrendChange:F2},{form.PeakPeriod},{form.PeakSignups}");
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
}
