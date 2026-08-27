using System.Text;

namespace KitCLI.Helpers;

internal static class TerminalText
{
    /// <summary>
    /// Removes terminal control sequences from untrusted, multi-line text while preserving line feeds for delimited output.
    /// </summary>
    public static string RemoveControlSequences(string value)
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

            if (character == '\u009B')
            {
                index = SkipCsiSequence(value, index + 1);
                continue;
            }

            if (character == '\u009D')
            {
                index = SkipOscSequence(value, index + 1);
                continue;
            }

            if (character == '\n')
            {
                sanitized.Append(character);
            }
            else if (!char.IsControl(character))
            {
                sanitized.Append(character);
            }

            index++;
        }

        return sanitized.ToString();
    }

    private static int SkipEscapeSequence(string value, int index)
    {
        if (index >= value.Length)
        {
            return index;
        }

        return value[index] switch
        {
            '[' => SkipCsiSequence(value, index + 1),
            ']' => SkipOscSequence(value, index + 1),
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

    private static int SkipOscSequence(string value, int index)
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
