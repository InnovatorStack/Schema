using System;
using System.Collections.Generic;
using System.Text.Json;

namespace InnovatorSchema;

internal sealed class JsonInputNode : InputNode
{
    private readonly JsonElement element;
    private IReadOnlyList<InputMember>? members;
    private Dictionary<string, InputNode>? index;
    private IReadOnlyList<InputNode>? elements;

    public JsonInputNode(JsonElement element)
    {
        this.element = element;
    }

    public override InputFormat Format => InputFormat.Json;

    public override InputKind Kind => element.ValueKind switch
    {
        JsonValueKind.Null => InputKind.Null,
        JsonValueKind.String => InputKind.String,
        JsonValueKind.Number => InputKind.Number,
        JsonValueKind.True or JsonValueKind.False => InputKind.Boolean,
        JsonValueKind.Object => InputKind.Object,
        JsonValueKind.Array => InputKind.Array,
        _ => InputKind.Missing
    };

    public override string Text
    {
        get
        {
            if (Kind == InputKind.Number)
            {
                return element.GetRawText();
            }

            if (element.GetString() is string value)
            {
                return value;
            }

            throw new InvalidOperationException("A string node must contain a string.");
        }
    }

    public override bool Boolean => element.GetBoolean();

    public override IReadOnlyList<InputMember> Members
    {
        get
        {
            EnsureMembers();
            return members ?? throw new InvalidOperationException("Members were not initialized.");
        }
    }

    public override IReadOnlyList<InputNode> Elements
    {
        get
        {
            if (elements is null)
            {
                var values = new List<InputNode>();

                foreach (JsonElement child in element.EnumerateArray())
                {
                    values.Add(new JsonInputNode(child));
                }

                elements = values.AsReadOnly();
            }

            return elements;
        }
    }

    public override InputNode Property(string name)
    {
        EnsureMembers();

        if (index is not null && index.TryGetValue(name, out InputNode? value))
        {
            return value;
        }

        return MissingInputNode.Instance;
    }

    private void EnsureMembers()
    {
        if (members is not null)
        {
            return;
        }

        var values = new List<InputMember>();
        index = new Dictionary<string, InputNode>(StringComparer.Ordinal);

        foreach (JsonProperty property in element.EnumerateObject())
        {
            var node = new JsonInputNode(property.Value);
            values.Add(new InputMember(property.Name, node));
            index.Add(property.Name, node);
        }

        members = values.AsReadOnly();
    }
}
