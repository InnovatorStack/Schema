using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;

namespace InnovatorSchema;

/// <summary>Read-only parsed fields, accessible by name or string index.</summary>
[SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix",
    Justification = "Parsed fields support member access and dictionary lookup.")]
public sealed class ParsedObject : DynamicObject, IReadOnlyDictionary<string, object?>
{
    private readonly Dictionary<string, object?> values;
    private readonly HashSet<string>? missing;

    internal ParsedObject(Dictionary<string, object?> ownedValues, HashSet<string>? missing = null)
    {
        values = ownedValues;
        this.missing = missing;
    }

    /// <summary>Read a declared field. An omitted optional field returns null.</summary>
    public object? this[string key] => TryRead(key, out object? value) ? value :
        throw new KeyNotFoundException("The schema does not declare field: " + key);

    int IReadOnlyCollection<KeyValuePair<string, object?>>.Count => values.Count;
    IEnumerable<string> IReadOnlyDictionary<string, object?>.Keys => values.Keys;
    IEnumerable<object?> IReadOnlyDictionary<string, object?>.Values => values.Values;

    /// <summary>Whether the parsed output contains the field, including explicit null.</summary>
    public bool ContainsKey(string key)
    {
        return values.ContainsKey(key);
    }

    /// <summary>Read a present field without merging omission with explicit null.</summary>
    public bool TryGetValue(string key, out object? value)
    {
        return values.TryGetValue(key, out value);
    }

    /// <inheritdoc />
    public override bool TryGetMember(GetMemberBinder binder, out object? result)
    {
        return TryRead(binder.Name, out result);
    }

    /// <inheritdoc />
    public override IEnumerable<string> GetDynamicMemberNames()
    {
        foreach (string key in values.Keys)
        {
            yield return key;
        }

        if (missing is not null)
        {
            foreach (string key in missing)
            {
                yield return key;
            }
        }
    }

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
    {
        return values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private bool TryRead(string key, out object? value)
    {
        return values.TryGetValue(key, out value) || (missing?.Contains(key) ?? false);
    }
}
