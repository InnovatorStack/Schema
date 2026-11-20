using System;
using System.Collections.Generic;

namespace InnovatorSchema;

internal readonly struct DefinitionGraph
{
    public static DefinitionGraph Leaf { get; } = new(1, 1, 0);

    private DefinitionGraph(int depth, int nodes, int rules)
    {
        if (depth > 64 || nodes > 16_384 || rules > DefinitionLimits.RulesPerObject)
        {
            throw new ArgumentException("Schema nesting, node or rule limit exceeded.");
        }

        Depth = depth;
        Nodes = nodes;
        Rules = rules;
    }

    public DefinitionGraph(IEnumerable<DefinitionGraph> children, int ownRules = 0)
    {
        int depth = 1;
        int nodes = 1;
        int rules = ownRules;
        int count = 0;

        foreach (DefinitionGraph child in children)
        {
            count++;

            if (count > DefinitionLimits.Fields)
            {
                throw new ArgumentException(
                    "A definition has too many children.", nameof(children));
            }

            depth = Math.Max(depth, child.Depth + 1);
            nodes += child.Nodes;
            rules += child.Rules;
        }

        this = new DefinitionGraph(depth, nodes, rules);
    }

    public int Depth { get; }
    public int Nodes { get; }
    public int Rules { get; }

    public DefinitionGraph WithStage(int rules = 0)
    {
        // Pipeline stages add work but do not add another level of input nesting.
        return new DefinitionGraph(Depth, Nodes + 1 + rules, Rules + rules);
    }
}

internal static class DefinitionLimits
{
    public const int Fields = 256;
    public const int RulesPerValue = 64;
    public const int RulesPerObject = 4096;
    public const int PipelineStages = 64;
    public const int NameLength = 256;
    public const int PatternLength = 4096;
    public const int RuleIdLength = 128;
    public const int MessageLength = 1024;
}

internal static class DefinitionGuard
{
    public static void NonNegative(int value, string parameterName)
    {
#if NET8_0_OR_GREATER
        ArgumentOutOfRangeException.ThrowIfNegative(value, parameterName);
#else
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
#endif
    }

    public static string Name(string value, string parameter)
    {
        return Text(value, parameter, DefinitionLimits.NameLength);
    }

    public static string Text(string value, string parameter, int maximumLength)
    {
        ArgumentNullException.ThrowIfNull(value, parameter);

        if (string.IsNullOrWhiteSpace(value) || value.Length > maximumLength)
        {
            throw new ArgumentException(
                $"Value must contain text and be at most {maximumLength} characters.",
                parameter);
        }

        return value;
    }

    public static string? Message(string? value)
    {
        if (value is null)
        {
            return null;
        }

        return Text(value, "message", DefinitionLimits.MessageLength);
    }
}
