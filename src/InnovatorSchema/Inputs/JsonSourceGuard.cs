using System;

namespace InnovatorSchema;

internal static class JsonSourceGuard
{
    public static void Check(string text, ValidationOptions options)
    {
        bool inString = false;
        int depth = 0;

        for (int index = 0; index < text.Length; index++)
        {
            if ((index & 1023) == 0)
            {
                options.CancellationToken.ThrowIfCancellationRequested();
            }

            char character = text[index];

            if (char.IsSurrogate(character))
            {
                if (!char.IsHighSurrogate(character) || index + 1 >= text.Length ||
                    !char.IsLowSurrogate(text[index + 1]))
                {
                    InvalidUnicode();
                }

                index++;
            }
            else if (inString && character == '\\')
            {
                index++;

                if (index < text.Length && text[index] == 'u')
                {
                    CheckEscape(text, ref index);
                }
            }
            else if (character == '"')
            {
                inString = !inString;
            }
            else if (!inString && character is '{' or '[')
            {
                depth++;

                if (depth > options.MaximumDepth)
                {
                    throw new InputFailureException(IssueCode.LimitExceeded,
                        "Input nesting limit exceeded.", new IssuePath(Array.Empty<PathSegment>()));
                }
            }
            else if (!inString && character is '}' or ']')
            {
                depth--;
            }
        }
    }

    private static void CheckEscape(string text, ref int index)
    {
        if (!ReadHex(text, index + 1, out char character))
        {
            return;
        }

        index += 4;

        if (char.IsHighSurrogate(character))
        {
            if (index + 6 >= text.Length || text[index + 1] != '\\' || text[index + 2] != 'u' ||
                !ReadHex(text, index + 3, out char low) || !char.IsLowSurrogate(low))
            {
                InvalidUnicode();
            }

            index += 6;
        }
        else if (char.IsLowSurrogate(character))
        {
            InvalidUnicode();
        }
    }

    private static bool ReadHex(string text, int start, out char character)
    {
        int value = 0;
        character = default;

        if (start > text.Length - 4)
        {
            return false;
        }

        for (int index = start; index < start + 4; index++)
        {
            int digit = text[index] switch
            {
                >= '0' and <= '9' => text[index] - '0',
                >= 'a' and <= 'f' => text[index] - 'a' + 10,
                >= 'A' and <= 'F' => text[index] - 'A' + 10,
                _ => -1
            };

            if (digit < 0)
            {
                return false;
            }

            value = value * 16 + digit;
        }

        character = (char)value;
        return true;
    }

    private static void InvalidUnicode()
    {
        throw new InputFailureException(IssueCode.MalformedJson,
            "JSON contains an unpaired Unicode surrogate.",
            new IssuePath(Array.Empty<PathSegment>()));
    }
}
