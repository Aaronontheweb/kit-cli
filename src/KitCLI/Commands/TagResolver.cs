using KitCLI.Models;
using KitCLI.Services;

namespace KitCLI.Commands;

/// <summary>
/// Resolves a user-supplied tag identifier using the CLI's documented name-first policy.
/// </summary>
internal static class TagResolver
{
    public static async Task<Tag?> ResolveAsync(IKitApiClient client, string identifier)
    {
        var tags = await client.GetTagsAsync();
        return Resolve(tags, identifier);
    }

    /// <summary>
    /// Matches names case-insensitively before numeric IDs because tags may have numeric names.
    /// </summary>
    public static Tag? Resolve(Tag[] tags, string identifier)
    {
        var byName = tags.FirstOrDefault(tag => tag.Name.Equals(identifier, StringComparison.OrdinalIgnoreCase));
        if (byName != null)
        {
            return byName;
        }

        return long.TryParse(identifier, out var id)
            ? tags.FirstOrDefault(tag => tag.Id == id)
            : null;
    }
}
