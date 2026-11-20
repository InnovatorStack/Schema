using System;
using System.Collections.Generic;

namespace InnovatorSchema;

/// <summary>Missing and null states permitted by a reader or successful output.</summary>
[Flags]
public enum PresencePolicy
{
    /// <summary>A present, non-null value is required.</summary>
    Required = 0,
    /// <summary>An omitted value is permitted.</summary>
    AllowMissing = 1,
    /// <summary>An explicit null is permitted.</summary>
    AllowNull = 2
}

/// <summary>An immutable source reader and pipeline producing present values of type T.</summary>
/// <remarks>Missing and null outputs are represented by ValidationValue&lt;T&gt;.</remarks>
public class Schema<T>
{
    private readonly IReadOnlyDictionary<IssueCode, string>? messages;

    /// <summary>Share a definition without adding a stage or retaining request data.</summary>
    protected Schema(Schema<T> definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        Reader = definition.Reader;
        InputPresence = definition.InputPresence;
        messages = definition.messages;
    }

    internal Schema(SchemaReader<T> reader, PresencePolicy presence = PresencePolicy.Required,
        IReadOnlyDictionary<IssueCode, string>? messages = null)
    {
        Reader = reader;
        InputPresence = presence;
        this.messages = messages;
    }

    /// <summary>Missing and null states accepted before the pipeline runs.</summary>
    public PresencePolicy InputPresence { get; }

    /// <summary>Missing and null states that can remain after successful validation.</summary>
    public PresencePolicy OutputPresence => Reader.OutputPresence(InputPresence);

    internal SchemaReader<T> Reader { get; }
    internal DefinitionGraph Graph => Reader.Definition;

    /// <summary>Permit omitted input; present-value rules and transforms skip that state.</summary>
    public Schema<T> Optional()
    {
        return new Schema<T>(Reader, InputPresence | PresencePolicy.AllowMissing, messages);
    }

    /// <summary>Permit null input without permitting transforms to introduce null.</summary>
    public Schema<T> Nullable()
    {
        return new Schema<T>(Reader, InputPresence | PresencePolicy.AllowNull, messages);
    }

    /// <summary>Reject missing or null output at this point in the pipeline.</summary>
    public Schema<T> RequireValue()
    {
        return WithReader(new RequiredReader<T>(Reader));
    }

    /// <summary>Check present values; accepted missing and null states pass through.</summary>
    public Schema<T> Check(Rule<T> rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        SchemaReader<T> source = Reader;
        RuleSet<T> checks = rule.Checks;

        if (source is CheckedReader<T> previous)
        {
            source = previous.Source;
            checks = previous.Checks.Combine(checks);
        }

        return WithReader(new CheckedReader<T>(source, checks));
    }

    /// <summary>Check present values using a predicate safe for concurrent calls.</summary>
    public Schema<T> Check(Func<T, bool> predicate, string ruleId, string message)
    {
        return Check(new Rule<T>(new CustomRule<T>(predicate, ruleId, message)));
    }

    /// <summary>Map valid present values; a null return produces a NullNotAllowed issue.</summary>
    public Schema<TNext> Transform<TNext>(Func<T, TNext> transform)
    {
        ArgumentNullException.ThrowIfNull(transform);
        return WithReader(new TransformReader<T, TNext>(Reader, transform));
    }

    /// <summary>Map valid present values to a reference or an explicitly permitted null.</summary>
    public Schema<TNext> TransformNullable<TNext>(Func<T, TNext?> transform) where TNext : class
    {
        ArgumentNullException.ThrowIfNull(transform);
        return WithReader(new NullableReferenceReader<T, TNext>(Reader, transform));
    }

    /// <summary>Map present values to a nullable value type.</summary>
    public Schema<TNext> TransformNullable<TNext>(Func<T, TNext?> transform) where TNext : struct
    {
        ArgumentNullException.ThrowIfNull(transform);
        return WithReader(new NullableTransformReader<T, TNext>(Reader, transform));
    }

    /// <summary>Override this schema's diagnostic text, leaving child schemas alone.</summary>
    public Schema<T> WithMessage(IssueCode code, string message)
    {
        if (!Enum.IsDefined(code))
        {
            throw new ArgumentOutOfRangeException(nameof(code));
        }

        DefinitionGuard.Text(message, nameof(message), DefinitionLimits.MessageLength);
        var updated = new Dictionary<IssueCode, string>();

        if (messages is not null)
        {
            foreach (var pair in messages)
            {
                updated.Add(pair.Key, pair.Value);
            }
        }

        updated[code] = message;
        return new Schema<T>(Reader, InputPresence, updated);
    }

    internal ValidationValue<T> Read(InputNode input, ValidationContext context)
    {
        using var scope = context.EnterMessages(messages);
        return Reader.Read(input, context, InputPresence);
    }

    private Schema<TNext> WithReader<TNext>(SchemaReader<TNext> reader)
    {
        return new Schema<TNext>(reader, InputPresence, messages);
    }
}
