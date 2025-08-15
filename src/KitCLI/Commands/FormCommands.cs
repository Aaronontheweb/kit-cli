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
                        format = args[++i].ToLowerInvariant();
                    break;
                case "--include-archived":
                    includeArchived = true;
                    break;
                case "--limit":
                case "-l":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out var l))
                        limit = Math.Min(l, 500);
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
                        format = args[++i].ToLowerInvariant();
                    break;
                case "--limit":
                case "-l":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out var l))
                        limit = Math.Min(l, 1000);
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

    private static string EscapeCsvField(string? field)
    {
        if (string.IsNullOrEmpty(field))
            return "";

        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return "\"" + field.Replace("\"", "\"\"") + "\"";
        }

        return field;
    }
}
