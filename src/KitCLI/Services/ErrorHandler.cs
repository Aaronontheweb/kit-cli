using System.Net;
using System.Text.Json;

namespace KitCLI.Services;

/// <summary>
/// Centralized error handling service for consistent error messages and logging
/// </summary>
public static class ErrorHandler
{
    private static bool _verboseMode = Environment.GetEnvironmentVariable("KIT_CLI_VERBOSE") == "1";

    public static void SetVerboseMode(bool verbose) => _verboseMode = verbose;

    /// <summary>
    /// Handle API errors with user-friendly messages
    /// </summary>
    public static void HandleApiError(HttpStatusCode statusCode, string? responseBody = null, string? context = null)
    {
        var errorMessage = statusCode switch
        {
            HttpStatusCode.Unauthorized => "Authentication failed. Please check your API key with 'kit config test'",
            HttpStatusCode.Forbidden => "Access denied. Your API key may not have the required permissions",
            HttpStatusCode.NotFound => context ?? "Resource not found. Please check the ID and try again",
            HttpStatusCode.TooManyRequests => "Rate limit exceeded. Please wait a moment and try again",
            HttpStatusCode.InternalServerError => "Kit API server error. Please try again later",
            HttpStatusCode.ServiceUnavailable => "Kit API is temporarily unavailable. Please try again later",
            HttpStatusCode.GatewayTimeout => "Request timed out. The Kit API may be experiencing high load",
            _ => $"Unexpected error (HTTP {(int)statusCode})"
        };

        Console.Error.WriteLine($"❌ {errorMessage}");

        if (_verboseMode && !string.IsNullOrEmpty(responseBody))
        {
            Console.Error.WriteLine("Response details:");
            TryPrettyPrintJson(responseBody);
        }
    }

    /// <summary>
    /// Handle common exceptions with helpful messages
    /// </summary>
    public static void HandleException(Exception ex, string? context = null)
    {
        switch (ex)
        {
            case HttpRequestException httpEx:
                HandleNetworkError(httpEx);
                break;
            
            case TaskCanceledException:
                Console.Error.WriteLine("❌ Operation cancelled or timed out");
                break;
            
            case JsonException jsonEx:
                Console.Error.WriteLine($"❌ Invalid response format from Kit API");
                if (_verboseMode)
                {
                    Console.Error.WriteLine($"JSON Error: {jsonEx.Message}");
                }
                break;
            
            case UnauthorizedAccessException:
                Console.Error.WriteLine("❌ Access denied. Please check your permissions");
                break;
            
            case ArgumentException argEx:
                Console.Error.WriteLine($"❌ Invalid argument: {argEx.Message}");
                break;
            
            default:
                var message = context ?? "An unexpected error occurred";
                Console.Error.WriteLine($"❌ {message}");
                if (_verboseMode)
                {
                    Console.Error.WriteLine($"Error details: {ex.Message}");
                    Console.Error.WriteLine($"Type: {ex.GetType().Name}");
                    if (ex.InnerException != null)
                    {
                        Console.Error.WriteLine($"Inner: {ex.InnerException.Message}");
                    }
                }
                break;
        }
    }

    /// <summary>
    /// Handle network-specific errors
    /// </summary>
    private static void HandleNetworkError(HttpRequestException ex)
    {
        if (ex.Message.Contains("connection", StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine("❌ Unable to connect to Kit API. Please check your internet connection");
        }
        else if (ex.Message.Contains("SSL", StringComparison.OrdinalIgnoreCase) || 
                 ex.Message.Contains("TLS", StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine("❌ SSL/TLS error. This may be a temporary issue with the Kit API");
        }
        else if (ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine("❌ Request timed out. The Kit API may be slow to respond");
        }
        else
        {
            Console.Error.WriteLine($"❌ Network error: {ex.Message}");
        }

        if (_verboseMode && ex.InnerException != null)
        {
            Console.Error.WriteLine($"Inner exception: {ex.InnerException.Message}");
        }
    }

    /// <summary>
    /// Pretty print JSON for verbose output
    /// </summary>
    private static void TryPrettyPrintJson(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            // For AOT compatibility, we'll just format the JSON manually
            Console.Error.WriteLine(json);
        }
        catch
        {
            // If JSON parsing fails, just print the raw text
            Console.Error.WriteLine(json);
        }
    }

    /// <summary>
    /// Provide helpful suggestions for common errors
    /// </summary>
    public static void ProvideSuggestions(HttpStatusCode statusCode, string? operation = null)
    {
        Console.WriteLine("\n💡 Suggestions:");
        
        switch (statusCode)
        {
            case HttpStatusCode.Unauthorized:
                Console.WriteLine("  • Run 'kit config set --api-key YOUR_KEY' to update your API key");
                Console.WriteLine("  • Get your API key from: https://app.kit.com/account/edit");
                break;
            
            case HttpStatusCode.NotFound:
                if (operation?.Contains("subscriber", StringComparison.OrdinalIgnoreCase) == true)
                {
                    Console.WriteLine("  • Use 'kit subscriber search' to find subscribers by email");
                    Console.WriteLine("  • Use 'kit subscriber list' to see all subscribers");
                }
                else if (operation?.Contains("broadcast", StringComparison.OrdinalIgnoreCase) == true)
                {
                    Console.WriteLine("  • Use 'kit broadcast list' to see available broadcasts");
                }
                break;
            
            case HttpStatusCode.TooManyRequests:
                Console.WriteLine("  • Wait a few seconds before retrying");
                Console.WriteLine("  • Use '--limit' to reduce the number of results");
                Console.WriteLine("  • Consider using export commands for bulk operations");
                break;
        }
    }
}