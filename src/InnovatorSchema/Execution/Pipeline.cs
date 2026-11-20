using System;

namespace InnovatorSchema;

internal abstract class SchemaReader<T>
{
    protected SchemaReader(DefinitionGraph definition, bool readsCollection = false, int stages = 0)
    {
        if (stages > DefinitionLimits.PipelineStages)
        {
            throw new ArgumentException("A schema may contain at most 64 pipeline stages.");
        }

        Definition = definition;
        ReadsCollection = readsCollection;
        Stages = stages;
    }

    public DefinitionGraph Definition { get; }
    public bool ReadsCollection { get; }
    public int Stages { get; }

    public virtual PresencePolicy OutputPresence(PresencePolicy input)
    {
        return input;
    }

    public abstract ValidationValue<T> Read(
        InputNode input, ValidationContext context, PresencePolicy presence);
}

internal abstract class ValueReader<T> : SchemaReader<T>
{
    protected ValueReader(DefinitionGraph definition, bool readsCollection = false)
        : base(definition, readsCollection)
    {
    }

    public sealed override ValidationValue<T> Read(
        InputNode input, ValidationContext context, PresencePolicy presence)
    {
        if (!context.Step() || !input.Validate(context))
        {
            return ValidationValue<T>.Invalid();
        }

        // Only the source reader interprets input presence. Later state checks must still run.
        if (input.Kind == InputKind.Missing)
        {
            if ((presence & PresencePolicy.AllowMissing) != 0)
            {
                return ValidationValue<T>.Missing();
            }

            context.Add(IssueCode.Required, "A value is required.");
            return ValidationValue<T>.Invalid();
        }

        if (input.Kind == InputKind.Null)
        {
            if ((presence & PresencePolicy.AllowNull) != 0)
            {
                return ValidationValue<T>.Null();
            }

            context.Add(IssueCode.NullNotAllowed, "Null is not allowed.");
            return ValidationValue<T>.Invalid();
        }

        return ReadValue(input, context);
    }

    protected abstract ValidationValue<T> ReadValue(InputNode input, ValidationContext context);
}

internal sealed class CheckedReader<T> : SchemaReader<T>
{
    public CheckedReader(SchemaReader<T> source, RuleSet<T> checks)
        : base(source.Definition.WithStage(checks.Count), source.ReadsCollection, source.Stages + 1)
    {
        Source = source;
        Checks = checks;
    }

    public SchemaReader<T> Source { get; }
    public RuleSet<T> Checks { get; }

    public override PresencePolicy OutputPresence(PresencePolicy input)
    {
        return Source.OutputPresence(input);
    }

    public override ValidationValue<T> Read(
        InputNode input, ValidationContext context, PresencePolicy presence)
    {
        ValidationValue<T> value = Source.Read(input, context, presence);

        if (value.HasValue && !Checks.Check(value.Value, context))
        {
            return ValidationValue<T>.Invalid();
        }

        return value;
    }
}

internal sealed class RequiredReader<T> : SchemaReader<T>
{
    private readonly SchemaReader<T> source;

    public RequiredReader(SchemaReader<T> source)
        : base(source.Definition.WithStage(), source.ReadsCollection, source.Stages + 1)
    {
        this.source = source;
    }

    public override PresencePolicy OutputPresence(PresencePolicy input)
    {
        return PresencePolicy.Required;
    }

    public override ValidationValue<T> Read(
        InputNode input, ValidationContext context, PresencePolicy presence)
    {
        ValidationValue<T> value = source.Read(input, context, presence);

        if (!context.Step())
        {
            return ValidationValue<T>.Invalid();
        }

        if (value.IsMissing)
        {
            context.Add(IssueCode.Required, "A value is required.");
            return ValidationValue<T>.Invalid();
        }

        if (value.IsNull)
        {
            context.Add(IssueCode.NullNotAllowed, "Null is not allowed.");
            return ValidationValue<T>.Invalid();
        }

        return value;
    }
}

internal abstract class TransformReaderBase<TInput, TOutput> : SchemaReader<TOutput>
{
    private readonly SchemaReader<TInput> source;
    private readonly bool nullable;

    protected TransformReaderBase(SchemaReader<TInput> source, bool nullable)
        : base(source.Definition.WithStage(), source.ReadsCollection, source.Stages + 1)
    {
        this.source = source;
        this.nullable = nullable;
    }

    public override PresencePolicy OutputPresence(PresencePolicy input)
    {
        PresencePolicy result = source.OutputPresence(input);
        return nullable ? result | PresencePolicy.AllowNull : result;
    }

    public sealed override ValidationValue<TOutput> Read(
        InputNode input, ValidationContext context, PresencePolicy presence)
    {
        int before = context.IssueCount;
        ValidationValue<TInput> value = source.Read(input, context, presence);

        if (!context.Step() || context.IssueCount != before)
        {
            return ValidationValue<TOutput>.Invalid();
        }

        if (!value.HasValue)
        {
            return value.WithoutValue<TOutput>();
        }

        ValidationValue<TOutput> output = Invoke(value.Value);

        if (output.IsNull && !nullable)
        {
            context.Add(IssueCode.NullNotAllowed, "The transform returned null.");
            return ValidationValue<TOutput>.Invalid();
        }

        return output;
    }

    protected abstract ValidationValue<TOutput> Invoke(TInput value);
}

internal sealed class TransformReader<TInput, TOutput> : TransformReaderBase<TInput, TOutput>
{
    private readonly Func<TInput, TOutput> transform;

    public TransformReader(SchemaReader<TInput> source, Func<TInput, TOutput> transform)
        : base(source, false)
    {
        this.transform = transform;
    }

    protected override ValidationValue<TOutput> Invoke(TInput value)
    {
        return ValidationValue<TOutput>.Present(transform(value));
    }
}

internal sealed class NullableTransformReader<TInput, TOutput>
    : TransformReaderBase<TInput, TOutput>
    where TOutput : struct
{
    private readonly Func<TInput, TOutput?> transform;

    public NullableTransformReader(SchemaReader<TInput> source, Func<TInput, TOutput?> transform)
        : base(source, true)
    {
        this.transform = transform;
    }

    protected override ValidationValue<TOutput> Invoke(TInput value)
    {
        TOutput? output = transform(value);

        if (!output.HasValue)
        {
            return ValidationValue<TOutput>.Null();
        }

        return ValidationValue<TOutput>.Present(output.Value);
    }
}

internal sealed class NullableReferenceReader<TInput, TOutput>
    : TransformReaderBase<TInput, TOutput>
    where TOutput : class
{
    private readonly Func<TInput, TOutput?> transform;

    public NullableReferenceReader(SchemaReader<TInput> source, Func<TInput, TOutput?> transform)
        : base(source, true)
    {
        this.transform = transform;
    }

    protected override ValidationValue<TOutput> Invoke(TInput value)
    {
        TOutput? output = transform(value);

        if (output is null)
        {
            return ValidationValue<TOutput>.Null();
        }

        return ValidationValue<TOutput>.Present(output);
    }
}
