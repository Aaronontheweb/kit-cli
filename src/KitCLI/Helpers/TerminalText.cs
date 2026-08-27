using System.Globalization;
using System.Text;

namespace KitCLI.Helpers;

internal static class TerminalText
{
    /// <summary>
    /// Renders untrusted text safely on a single terminal line.
    /// </summary>
    public static string RenderSingleLine(string value) => Render(value, preserveLineFeeds: false);

    /// <summary>
    /// Renders untrusted text safely in a delimited multi-line terminal region.
    /// </summary>
    public static string RenderMultiline(string value) => Render(value, preserveLineFeeds: true);

    private static string Render(string value, bool preserveLineFeeds)
    {
        var sanitized = new StringBuilder(value.Length);

        for (var index = 0; index < value.Length;)
        {
            var character = value[index];

            if (character == '\u001B')
            {
                index = SkipEscapeSequence(value, index + 1);
                continue;
            }

            if (character is '\u009B')
            {
                index = SkipCsiSequence(value, index + 1);
                continue;
            }

            if (character is '\u009D' or '\u0090' or '\u0098' or '\u009E' or '\u009F')
            {
                index = SkipStringControlSequence(value, index + 1);
                continue;
            }

            if (character == '\n' && preserveLineFeeds)
            {
                sanitized.Append(character);
            }
            else if (IsSafePrintable(character))
            {
                sanitized.Append(character);
            }

            index++;
        }

        return sanitized.ToString();
    }

    private static bool IsSafePrintable(char character) =>
        !char.IsControl(character) &&
        char.GetUnicodeCategory(character) is not UnicodeCategory.Format
            and not UnicodeCategory.LineSeparator
            and not UnicodeCategory.ParagraphSeparator;

    private static int SkipEscapeSequence(string value, int index)
    {
        if (index >= value.Length)
        {
            return index;
        }

        return value[index] switch
        {
            '[' => SkipCsiSequence(value, index + 1),
            ']' or 'P' or 'X' or '^' or '_' => SkipStringControlSequence(value, index + 1),
            _ => index + 1
        };
    }

    private static int SkipCsiSequence(string value, int index)
    {
        while (index < value.Length)
        {
            var character = value[index++];
            if (character is >= '\u0040' and <= '\u007E')
            {
                break;
            }
        }

        return index;
    }

    private static int SkipStringControlSequence(string value, int index)
    {
        while (index < value.Length)
        {
            var character = value[index++];
            if (character is '\a' or '\u009C')
            {
                break;
            }

            if (character == '\u001B' && index < value.Length && value[index] == '\\')
            {
                return index + 1;
            }
        }

        return index;
    }
}
