using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace InnovatorSchema;

/// <summary>A logical validation path rooted at $.</summary>
public sealed class IssuePath : IReadOnlyList<PathSegment>
{
    private readonly PathSegment[] segments;

    internal IssuePath(IEnumerable<PathSegment> source)
    {
        segments = new List<PathSegment>(source).ToArray();
    }

    /// <summary>The number of path components.</summary>
    public int Count => segments.Length;

    /// <summary>The component at the specified position.</summary>
    public PathSegment this[int index] => segments[index];

    /// <summary>Render an unambiguous path rooted at $.</summary>
    public override string ToString()
    {
        var result = new StringBuilder("$");

        foreach (PathSegment segment in segments)
        {
            Append(result, segment);
        }

        return result.ToString();
    }

    /// <inheritdoc />
    public IEnumerator<PathSegment> GetEnumerator()
    {
        foreach (PathSegment segment in segments)
        {
            yield return segment;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private static void Append(StringBuilder result, PathSegment segment)
    {
        switch (segment.Kind)
        {
            case PathSegmentKind.Index:
                result.Append('[').Append(segment.Index.ToString(CultureInfo.InvariantCulture));
                result.Append(']');
                break;
            case PathSegmentKind.Attribute:
                result.Append("[@").Append(JsonSerializer.Serialize(segment.Name)).Append(']');
                break;
            case PathSegmentKind.Relationship:
                result.Append(".Relationships[");
                result.Append(JsonSerializer.Serialize(segment.Name)).Append(']');
                break;
            default:
                AppendProperty(result, segment.Name);
                break;
        }
    }

    private static void AppendProperty(StringBuilder result, string name)
    {
        bool identifier = name.Length > 0;

        for (int index = 0; index < name.Length; index++)
        {
            char character = name[index];
            bool letter = character is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or '_';
            bool digit = index > 0 && character is >= '0' and <= '9';
            identifier &= letter || digit;
        }

        if (identifier)
        {
            result.Append('.').Append(name);
        }
        else
        {
            result.Append('[').Append(JsonSerializer.Serialize(name)).Append(']');
        }
    }
}
