using System.Globalization;
using System.Text.Json;
using KitCLI.Helpers;
using KitCLI.Models;
using KitCLI.Services;

namespace KitCLI.Commands;

/// <summary>
/// Handles cohort analysis commands for subscriber data.
/// </summary>
public static class CohortCommands
{
    /// <summary>
    /// Handle the 'kit cohort by-signup' command.
    /// Analyzes subscriber retention by signup date cohorts.
    /// </summary>
    public static async Task<int> HandleBySignup(string[] args, IKitApiClient client)
    {
        // Parse arguments
        string period = "monthly";
        string metric = "retention";
        int lookbackDays = 365;
        string format = "table";
        string? exportPath = null;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--period":
                case "-p":
                    if (i + 1 < args.Length)
                    {
                        period = args[++i].ToLowerInvariant();
                    }

                    break;
                case "--metric":
                case "-m":
                    if (i + 1 < args.Length)
                    {
                        metric = args[++i].ToLowerInvariant();
                    }

                    break;
                case "--days":
                case "-d":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out var days))
                    {
                        lookbackDays = days;
                    }

                    break;
                case "--format":
                case "-f":
                    if (i + 1 < args.Length)
                    {
                        format = args[++i].ToLowerInvariant();
                    }

                    break;
                case "--export":
                    if (i + 1 < args.Length)
                    {
                        exportPath = args[++i];
                    }

                    break;
            }
        }

        // Validate period
        if (period != "weekly" && period != "monthly" && period != "quarterly")
        {
            Console.WriteLine($"Invalid period '{period}'. Use: weekly, monthly, quarterly");
            return 1;
        }

        // Validate metric
        if (metric != "retention" && metric != "engagement")
        {
            Console.WriteLine($"Invalid metric '{metric}'. Use: retention, engagement");
            return 1;
        }

        var cutoffDate = DateTime.UtcNow.AddDays(-lookbackDays);

        using var progress = new ProgressIndicator($"Analyzing {period} cohorts by signup date");

        // Stream all subscribers and collect within date range
        var subscribers = new List<Subscriber>();
        await foreach (var subscriber in client.GetAllSubscribersAsync())
        {
            if (subscriber.CreatedAt >= cutoffDate)
            {
                subscribers.Add(subscriber);
            }
        }

        if (subscribers.Count == 0)
        {
            progress.Complete("No subscribers found in date range");
            Console.WriteLine($"\nNo subscribers signed up in the last {lookbackDays} days.");
            return 0;
        }

        progress.Complete($"Analyzing {subscribers.Count:N0} subscribers");

        // Group subscribers by period
        var cohorts = GroupIntoCohorts(subscribers, period);

        // Calculate retention metrics for each cohort
        var now = DateTime.UtcNow;
        var ageIntervals = GetAgeIntervals(period);

        foreach (var cohort in cohorts)
        {
            cohort.AgeDays = (int)(now - cohort.EndDate).TotalDays;
            cohort.MetricsByAge = CalculateAgeMetrics(cohort, ageIntervals, now);
        }

        // Sort cohorts by date (newest first)
        var sortedCohorts = cohorts.OrderByDescending(c => c.StartDate).ToArray();

        // Calculate aggregate metrics
        var avgRetention = sortedCohorts.Length > 0
            ? sortedCohorts.Average(c => c.RetentionRate)
            : 0;

        var halfLifeDays = EstimateHalfLife(sortedCohorts);

        // Generate insight
        var insight = GenerateInsight(sortedCohorts, avgRetention, halfLifeDays, period);

        var result = new CohortAnalysisResult
        {
            AnalysisType = "by-signup",
            Period = period,
            Metric = metric,
            LookbackDays = lookbackDays,
            TotalSubscribersAnalyzed = subscribers.Count,
            Cohorts = sortedCohorts,
            AverageRetentionRate = avgRetention,
            HalfLifeDays = halfLifeDays,
            Insight = insight
        };

        // Output results
        if (exportPath != null)
        {
            return await ExportCohortAnalysis(result, exportPath, ageIntervals);
        }

        PrintCohortAnalysis(result, format, ageIntervals);
        return 0;
    }

    /// <summary>
    /// Handle the 'kit cohort by-tag' command.
    /// Compares subscriber cohorts grouped by tags.
    /// </summary>
    public static async Task<int> HandleByTag(string[] args, IKitApiClient client)
    {
        // Parse arguments
        string? tagsArg = null;
        string? pattern = null;
        string format = "table";
        string? exportPath = null;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--tags":
                case "-t":
                    if (i + 1 < args.Length)
                    {
                        tagsArg = args[++i];
                    }
                    break;
                case "--pattern":
                case "-p":
                    if (i + 1 < args.Length)
                    {
                        pattern = args[++i];
                    }
                    break;
                case "--format":
                case "-f":
                    if (i + 1 < args.Length)
                    {
                        format = args[++i].ToLowerInvariant();
                    }
                    break;
                case "--export":
                    if (i + 1 < args.Length)
                    {
                        exportPath = args[++i];
                    }
                    break;
            }
        }

        // Validate arguments
        if (string.IsNullOrEmpty(tagsArg) && string.IsNullOrEmpty(pattern))
        {
            Console.WriteLine("Usage: kit cohort by-tag --tags <tag1,tag2,...> [options]");
            Console.WriteLine("       kit cohort by-tag --pattern <pattern> [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --tags, -t <tags>       Comma-separated list of tag names or IDs");
            Console.WriteLine("  --pattern, -p <pattern> Glob pattern to match tag names (e.g., 'training-*')");
            Console.WriteLine("  --format, -f <format>   Output format: table (default), json, csv");
            Console.WriteLine("  --export <path>         Export to file");
            return 1;
        }

        using var progress = new ProgressIndicator("Loading tags");

        // Get all tags
        var allTags = await client.GetTagsAsync();
        if (allTags.Length == 0)
        {
            progress.Complete("No tags found");
            Console.WriteLine("No tags found in your account.");
            return 1;
        }

        progress.Complete($"Found {allTags.Length} tags");

        // Filter tags based on input
        var selectedTags = new List<Tag>();

        if (!string.IsNullOrEmpty(tagsArg))
        {
            var tagNames = tagsArg.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var tagName in tagNames)
            {
                // Try to find by ID first
                if (long.TryParse(tagName, out var tagId))
                {
                    var tag = allTags.FirstOrDefault(t => t.Id == tagId);
                    if (tag != null)
                    {
                        selectedTags.Add(tag);
                        continue;
                    }
                }

                // Find by name (case-insensitive)
                var matchingTag = allTags.FirstOrDefault(t =>
                    t.Name.Equals(tagName, StringComparison.OrdinalIgnoreCase));

                if (matchingTag != null)
                {
                    selectedTags.Add(matchingTag);
                }
                else
                {
                    Console.WriteLine($"Warning: Tag '{tagName}' not found, skipping.");
                }
            }
        }

        if (!string.IsNullOrEmpty(pattern))
        {
            // Simple glob pattern matching (supports * wildcard)
            var regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(pattern)
                .Replace("\\*", ".*") + "$";

            var regex = new System.Text.RegularExpressions.Regex(regexPattern,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            foreach (var tag in allTags)
            {
                if (regex.IsMatch(tag.Name) && !selectedTags.Any(t => t.Id == tag.Id))
                {
                    selectedTags.Add(tag);
                }
            }
        }

        if (selectedTags.Count == 0)
        {
            Console.WriteLine("No matching tags found.");
            return 1;
        }

        if (selectedTags.Count == 1)
        {
            Console.WriteLine("At least 2 tags are required for comparison.");
            return 1;
        }

        Console.WriteLine($"\nComparing {selectedTags.Count} tags: {string.Join(", ", selectedTags.Select(t => t.Name))}");

        // Fetch subscribers for each tag
        var tagCohorts = new List<TagCohort>();

        foreach (var tag in selectedTags)
        {
            using var tagProgress = new ProgressIndicator($"Fetching subscribers for '{tag.Name}'");

            var subscribers = new List<Subscriber>();
            string? cursor = null;
            bool hasMore = true;

            while (hasMore)
            {
                var response = await client.GetTagSubscribersAsync(tag.Id, 100, cursor);

                foreach (var sub in response.Data)
                {
                    subscribers.Add(sub);
                }

                if (response.Pagination != null && response.Pagination.HasNextPage)
                {
                    cursor = response.Pagination.EndCursor;
                }
                else
                {
                    hasMore = false;
                }
            }

            tagProgress.Complete($"Found {subscribers.Count:N0} subscribers");

            // Calculate metrics
            var active = subscribers.Count(s => s.State.Equals("active", StringComparison.OrdinalIgnoreCase));
            var cancelled = subscribers.Count(s => s.State.Equals("cancelled", StringComparison.OrdinalIgnoreCase));

            tagCohorts.Add(new TagCohort
            {
                TagId = tag.Id,
                TagName = tag.Name,
                TotalSubscribers = subscribers.Count,
                ActiveSubscribers = active,
                CancelledSubscribers = cancelled,
                RetentionRate = subscribers.Count > 0 ? (double)active / subscribers.Count * 100 : 0
            });
        }

        // Sort by subscriber count descending
        var sortedCohorts = tagCohorts.OrderByDescending(c => c.TotalSubscribers).ToArray();

        // Generate insights
        var insight = GenerateTagInsight(sortedCohorts);

        var result = new TagCohortAnalysisResult
        {
            AnalysisType = "by-tag",
            TagCount = sortedCohorts.Length,
            TotalSubscribersAnalyzed = sortedCohorts.Sum(c => c.TotalSubscribers),
            Cohorts = sortedCohorts,
            Insight = insight
        };

        // Output results
        if (exportPath != null)
        {
            return await ExportTagCohortAnalysis(result, exportPath);
        }

        PrintTagCohortAnalysis(result, format);
        return 0;
    }

    private static string GenerateTagInsight(TagCohort[] cohorts)
    {
        if (cohorts.Length < 2)
        {
            return "Not enough cohorts for comparison.";
        }

        var insights = new List<string>();

        // Find highest and lowest retention
        var highestRetention = cohorts.MaxBy(c => c.RetentionRate);
        var lowestRetention = cohorts.MinBy(c => c.RetentionRate);

        if (highestRetention != null && lowestRetention != null && highestRetention.TagId != lowestRetention.TagId)
        {
            var diff = highestRetention.RetentionRate - lowestRetention.RetentionRate;
            if (diff > 5)
            {
                insights.Add($"'{highestRetention.TagName}' has {diff:F1}% higher retention than '{lowestRetention.TagName}'.");
            }
        }

        // Find largest cohort
        var largest = cohorts.MaxBy(c => c.TotalSubscribers);
        if (largest != null)
        {
            insights.Add($"Largest cohort: '{largest.TagName}' with {largest.TotalSubscribers:N0} subscribers.");
        }

        // Check for small sample sizes
        var smallCohorts = cohorts.Where(c => c.TotalSubscribers < 30).ToList();
        if (smallCohorts.Count > 0)
        {
            insights.Add($"Note: {smallCohorts.Count} tag(s) have fewer than 30 subscribers (low statistical significance).");
        }

        return insights.Count > 0 ? string.Join(" ", insights) : "Tag cohorts are similar in retention rates.";
    }

    private static void PrintTagCohortAnalysis(TagCohortAnalysisResult result, string format)
    {
        switch (format.ToLowerInvariant())
        {
            case "json":
                PrintTagCohortJson(result);
                break;
            case "csv":
                PrintTagCohortCsv(result);
                break;
            default:
                PrintTagCohortTable(result);
                break;
        }
    }

    private static void PrintTagCohortTable(TagCohortAnalysisResult result)
    {
        Console.WriteLine();
        Console.WriteLine("Tag Cohort Comparison");
        Console.WriteLine(new string('─', 90));

        Console.WriteLine($"{"Tag",-30} {"Subscribers",12} {"Active",10} {"Cancelled",10} {"Retention",12}");
        Console.WriteLine(new string('─', 90));

        foreach (var cohort in result.Cohorts)
        {
            var tagName = cohort.TagName.Length > 28 ? cohort.TagName[..28] + ".." : cohort.TagName;
            Console.WriteLine($"{tagName,-30} {cohort.TotalSubscribers,12:N0} {cohort.ActiveSubscribers,10:N0} {cohort.CancelledSubscribers,10:N0} {cohort.RetentionRate,11:F1}%");
        }

        Console.WriteLine(new string('─', 90));
        Console.WriteLine($"{"Total",-30} {result.TotalSubscribersAnalyzed,12:N0}");
        Console.WriteLine();
        Console.WriteLine($"Insight: {result.Insight}");
    }

    private static void PrintTagCohortJson(TagCohortAnalysisResult result)
    {
        var json = JsonSerializer.Serialize(result, KitJsonIndentedContext.Default.TagCohortAnalysisResult);
        Console.WriteLine(json);
    }

    private static void PrintTagCohortCsv(TagCohortAnalysisResult result)
    {
        Console.WriteLine("tag_id,tag_name,total_subscribers,active_subscribers,cancelled_subscribers,retention_rate");

        foreach (var cohort in result.Cohorts)
        {
            var tagName = EscapeCsvField(cohort.TagName);
            Console.WriteLine($"{cohort.TagId},{tagName},{cohort.TotalSubscribers},{cohort.ActiveSubscribers},{cohort.CancelledSubscribers},{cohort.RetentionRate:F2}");
        }
    }

    private static async Task<int> ExportTagCohortAnalysis(TagCohortAnalysisResult result, string outputPath)
    {
        var fileFormat = Path.GetExtension(outputPath).ToLowerInvariant() switch
        {
            ".json" => "json",
            ".csv" => "csv",
            _ => "csv"
        };

        if (!outputPath.Contains('.'))
        {
            outputPath += ".csv";
        }

        await using var writer = new StreamWriter(outputPath);

        if (fileFormat == "json")
        {
            var json = JsonSerializer.Serialize(result, KitJsonIndentedContext.Default.TagCohortAnalysisResult);
            await writer.WriteAsync(json);
        }
        else
        {
            await writer.WriteLineAsync("tag_id,tag_name,total_subscribers,active_subscribers,cancelled_subscribers,retention_rate");

            foreach (var cohort in result.Cohorts)
            {
                var tagName = EscapeCsvField(cohort.TagName);
                await writer.WriteLineAsync($"{cohort.TagId},{tagName},{cohort.TotalSubscribers},{cohort.ActiveSubscribers},{cohort.CancelledSubscribers},{cohort.RetentionRate:F2}");
            }
        }

        Console.WriteLine($"Exported tag cohort analysis to {outputPath}");
        return 0;
    }

    private static string EscapeCsvField(string field)
    {
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n'))
        {
            return "\"" + field.Replace("\"", "\"\"") + "\"";
        }
        return field;
    }

    private static List<SignupCohort> GroupIntoCohorts(List<Subscriber> subscribers, string period)
    {
        var grouped = period switch
        {
            "weekly" => subscribers.GroupBy(s => GetWeekKey(s.CreatedAt)),
            "quarterly" => subscribers.GroupBy(s => GetQuarterKey(s.CreatedAt)),
            _ => subscribers.GroupBy(s => GetMonthKey(s.CreatedAt)) // monthly default
        };

        return grouped.Select(g =>
        {
            var (start, end, label) = ParsePeriodKey(g.Key, period);
            var total = g.Count();
            var active = g.Count(s => s.State.Equals("active", StringComparison.OrdinalIgnoreCase));
            var cancelled = g.Count(s => s.State.Equals("cancelled", StringComparison.OrdinalIgnoreCase));

            return new SignupCohort
            {
                Period = label,
                StartDate = start,
                EndDate = end,
                TotalSubscribers = total,
                ActiveSubscribers = active,
                CancelledSubscribers = cancelled,
                RetentionRate = total > 0 ? (double)active / total * 100 : 0
            };
        }).ToList();
    }

    private static string GetMonthKey(DateTime date) => $"{date.Year:0000}-{date.Month:00}";

    private static string GetQuarterKey(DateTime date)
    {
        var quarter = (date.Month - 1) / 3 + 1;
        return $"{date.Year:0000}-Q{quarter}";
    }

    private static string GetWeekKey(DateTime date)
    {
        var cal = CultureInfo.InvariantCulture.Calendar;
        var week = cal.GetWeekOfYear(date, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        return $"{date.Year:0000}-W{week:00}";
    }

    private static (DateTime start, DateTime end, string label) ParsePeriodKey(string key, string period)
    {
        if (period == "weekly")
        {
            // Format: YYYY-Wnn
            var year = int.Parse(key[..4]);
            var week = int.Parse(key[6..]);
            var jan1 = new DateTime(year, 1, 1);
            var daysOffset = DayOfWeek.Monday - jan1.DayOfWeek;
            var firstMonday = jan1.AddDays(daysOffset);
            var start = firstMonday.AddDays((week - 1) * 7);
            var end = start.AddDays(6);
            return (start, end, $"Week {week} {year}");
        }
        else if (period == "quarterly")
        {
            // Format: YYYY-Qn
            var year = int.Parse(key[..4]);
            var quarter = int.Parse(key[6..]);
            var startMonth = (quarter - 1) * 3 + 1;
            var start = new DateTime(year, startMonth, 1);
            var end = start.AddMonths(3).AddDays(-1);
            return (start, end, $"Q{quarter} {year}");
        }
        else
        {
            // Format: YYYY-MM (monthly)
            var year = int.Parse(key[..4]);
            var month = int.Parse(key[5..]);
            var start = new DateTime(year, month, 1);
            var end = start.AddMonths(1).AddDays(-1);
            var monthName = CultureInfo.InvariantCulture.DateTimeFormat.GetAbbreviatedMonthName(month);
            return (start, end, $"{monthName} {year}");
        }
    }

    private static int[] GetAgeIntervals(string period)
    {
        return period switch
        {
            "weekly" => [7, 14, 28, 56], // 1, 2, 4, 8 weeks
            "quarterly" => [30, 90, 180, 365], // 1, 3, 6, 12 months
            _ => [30, 90, 180, 365] // monthly: 1, 3, 6, 12 months
        };
    }

    private static string[] GetAgeLabels(string period)
    {
        return period switch
        {
            "weekly" => ["Week 1", "Week 2", "Week 4", "Week 8"],
            "quarterly" => ["Month 1", "Month 3", "Month 6", "Month 12"],
            _ => ["Month 1", "Month 3", "Month 6", "Month 12"]
        };
    }

    private static CohortAgeMetric[] CalculateAgeMetrics(SignupCohort cohort, int[] intervals, DateTime now)
    {
        var labels = GetAgeLabels("monthly"); // Use appropriate labels
        var metrics = new List<CohortAgeMetric>();

        for (int i = 0; i < intervals.Length; i++)
        {
            var daysOld = intervals[i];
            var notYetReached = cohort.AgeDays < daysOld;

            // For retention, we look at current state - subscribers who are still active
            // If cohort hasn't reached this age yet, mark as not reached
            metrics.Add(new CohortAgeMetric
            {
                AgeLabel = labels[i],
                DaysOld = daysOld,
                RetentionRate = notYetReached ? 0 : cohort.RetentionRate,
                ActiveCount = notYetReached ? 0 : cohort.ActiveSubscribers,
                NotYetReached = notYetReached
            });
        }

        return metrics.ToArray();
    }

    private static int? EstimateHalfLife(SignupCohort[] cohorts)
    {
        // Find cohorts old enough to have meaningful data
        var matureCohorts = cohorts.Where(c => c.AgeDays > 30 && c.TotalSubscribers > 10).ToList();
        if (matureCohorts.Count == 0)
        {
            return null;
        }

        // Find the first cohort where retention dropped below 50%
        var halfLifeCohort = matureCohorts
            .OrderBy(c => c.AgeDays)
            .FirstOrDefault(c => c.RetentionRate < 50);

        return halfLifeCohort?.AgeDays;
    }

    private static string GenerateInsight(SignupCohort[] cohorts, double avgRetention, int? halfLifeDays, string period)
    {
        if (cohorts.Length == 0)
        {
            return "No cohort data available.";
        }

        var insights = new List<string>();

        // Average retention insight
        insights.Add($"Average retention across cohorts: {avgRetention:F1}%");

        // Half-life insight
        if (halfLifeDays.HasValue)
        {
            var monthsToHalfLife = halfLifeDays.Value / 30.0;
            if (monthsToHalfLife < 1)
            {
                insights.Add($"Retention drops below 50% within {halfLifeDays.Value} days.");
            }
            else
            {
                insights.Add($"Retention drops below 50% around month {monthsToHalfLife:F1}.");
            }

            var reengageAt = halfLifeDays.Value * 0.6;
            insights.Add($"Consider re-engagement campaigns around day {reengageAt:F0}.");
        }
        else if (avgRetention > 70)
        {
            insights.Add("Retention is strong - half-life not yet reached for mature cohorts.");
        }

        // Trend insight (need at least 6 cohorts for a meaningful comparison)
        if (cohorts.Length >= 6)
        {
            var recent = cohorts.Take(3).Average(c => c.RetentionRate);
            var older = cohorts.Skip(3).Take(3).Average(c => c.RetentionRate);
            if (recent > older + 5)
            {
                insights.Add("Recent cohorts show improving retention.");
            }
            else if (recent < older - 5)
            {
                insights.Add("Recent cohorts show declining retention - investigate cause.");
            }
        }

        return string.Join(" ", insights);
    }

    private static void PrintCohortAnalysis(CohortAnalysisResult result, string format, int[] ageIntervals)
    {
        switch (format.ToLowerInvariant())
        {
            case "json":
                PrintCohortJson(result);
                break;
            case "csv":
                PrintCohortCsv(result, ageIntervals);
                break;
            default:
                PrintCohortTable(result, ageIntervals);
                break;
        }
    }

    private static void PrintCohortTable(CohortAnalysisResult result, int[] ageIntervals)
    {
        var labels = GetAgeLabels(result.Period);

        Console.WriteLine();
        Console.WriteLine($"Signup Cohort Analysis ({result.Period}, {result.Metric})");
        Console.WriteLine($"Lookback: {result.LookbackDays} days | Total subscribers: {result.TotalSubscribersAnalyzed:N0}");
        Console.WriteLine(new string('─', 80));

        // Header
        var header = $"{"Cohort",-12} {"Size",8}";
        foreach (var label in labels)
        {
            header += $" {label,10}";
        }

        Console.WriteLine(header);
        Console.WriteLine(new string('─', 80));

        // Rows
        foreach (var cohort in result.Cohorts)
        {
            var row = $"{cohort.Period,-12} {cohort.TotalSubscribers,8:N0}";

            foreach (var metric in cohort.MetricsByAge)
            {
                if (metric.NotYetReached)
                {
                    row += $" {"-",10}";
                }
                else
                {
                    row += $" {metric.RetentionRate,9:F1}%";
                }
            }

            Console.WriteLine(row);
        }

        Console.WriteLine(new string('─', 80));

        // Summary
        Console.WriteLine($"Average retention: {result.AverageRetentionRate:F1}%");
        if (result.HalfLifeDays.HasValue)
        {
            Console.WriteLine($"Estimated half-life: {result.HalfLifeDays} days");
        }

        Console.WriteLine();
        Console.WriteLine($"Insight: {result.Insight}");
    }

    private static void PrintCohortJson(CohortAnalysisResult result)
    {
        var json = JsonSerializer.Serialize(result, KitJsonIndentedContext.Default.CohortAnalysisResult);
        Console.WriteLine(json);
    }

    private static void PrintCohortCsv(CohortAnalysisResult result, int[] ageIntervals)
    {
        var labels = GetAgeLabels(result.Period);

        // Header
        var header = "cohort,start_date,end_date,total,active,cancelled,retention_rate,age_days";
        foreach (var label in labels)
        {
            header += $",{label.Replace(" ", "_").ToLowerInvariant()}_retention";
        }

        Console.WriteLine(header);

        // Rows
        foreach (var cohort in result.Cohorts)
        {
            var row = $"{cohort.Period},{cohort.StartDate:yyyy-MM-dd},{cohort.EndDate:yyyy-MM-dd}," +
                      $"{cohort.TotalSubscribers},{cohort.ActiveSubscribers},{cohort.CancelledSubscribers}," +
                      $"{cohort.RetentionRate:F2},{cohort.AgeDays}";

            foreach (var metric in cohort.MetricsByAge)
            {
                row += metric.NotYetReached ? "," : $",{metric.RetentionRate:F2}";
            }

            Console.WriteLine(row);
        }
    }

    private static async Task<int> ExportCohortAnalysis(CohortAnalysisResult result, string outputPath, int[] ageIntervals)
    {
        var fileFormat = Path.GetExtension(outputPath).ToLowerInvariant() switch
        {
            ".json" => "json",
            ".csv" => "csv",
            _ => "csv"
        };

        if (!outputPath.Contains('.'))
        {
            outputPath += ".csv";
        }

        await using var writer = new StreamWriter(outputPath);

        if (fileFormat == "json")
        {
            var json = JsonSerializer.Serialize(result, KitJsonIndentedContext.Default.CohortAnalysisResult);
            await writer.WriteAsync(json);
        }
        else
        {
            var labels = GetAgeLabels(result.Period);

            // Header
            var header = "cohort,start_date,end_date,total,active,cancelled,retention_rate,age_days";
            foreach (var label in labels)
            {
                header += $",{label.Replace(" ", "_").ToLowerInvariant()}_retention";
            }

            await writer.WriteLineAsync(header);

            // Rows
            foreach (var cohort in result.Cohorts)
            {
                var row = $"{cohort.Period},{cohort.StartDate:yyyy-MM-dd},{cohort.EndDate:yyyy-MM-dd}," +
                          $"{cohort.TotalSubscribers},{cohort.ActiveSubscribers},{cohort.CancelledSubscribers}," +
                          $"{cohort.RetentionRate:F2},{cohort.AgeDays}";

                foreach (var metric in cohort.MetricsByAge)
                {
                    row += metric.NotYetReached ? "," : $",{metric.RetentionRate:F2}";
                }

                await writer.WriteLineAsync(row);
            }
        }

        Console.WriteLine($"Exported cohort analysis to {outputPath}");
        return 0;
    }
}
