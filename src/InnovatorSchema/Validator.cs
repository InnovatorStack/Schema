using System;
using System.Diagnostics.CodeAnalysis;

namespace InnovatorSchema;

/// <summary>Define schemas and validate JSON, XML, AML or supplied Items.</summary>
public static class Validator
{
    /// <summary>Read a string without trimming or changing its contents.</summary>
    [SuppressMessage("Naming", "CA1720:Identifier contains type name",
        Justification = "Names the value type described by the schema.")]
    public static Schema<string> String()
    {
        return new Schema<string>(new StringValueReader());
    }

    /// <summary>Read a native JSON number exactly as a decimal.</summary>
    [SuppressMessage("Naming", "CA1720:Identifier contains type name",
        Justification = "Names the value type described by the schema.")]
    public static Schema<decimal> Decimal()
    {
        return new Schema<decimal>(
            new NumberReader<decimal>(InputKind.Number, NumericText.TryDecimal));
    }

    /// <summary>Read invariant numeric text exactly as a decimal, without trimming.</summary>
    public static Schema<decimal> DecimalText()
    {
        return new Schema<decimal>(
            new NumberReader<decimal>(InputKind.String, NumericText.TryDecimal));
    }

    /// <summary>Read a native JSON number exactly as a signed 64-bit integer.</summary>
    [SuppressMessage("Naming", "CA1720:Identifier contains type name",
        Justification = "Names the value type described by the schema.")]
    public static Schema<long> Integer()
    {
        return new Schema<long>(new NumberReader<long>(InputKind.Number, NumericText.TryInteger));
    }

    /// <summary>Read invariant numeric text exactly as a signed 64-bit integer.</summary>
    public static Schema<long> IntegerText()
    {
        return new Schema<long>(new NumberReader<long>(InputKind.String, NumericText.TryInteger));
    }

    /// <summary>Read a native JSON boolean.</summary>
    public static Schema<bool> Boolean()
    {
        return new Schema<bool>(new BooleanReader(null));
    }

    /// <summary>Read the exact boolean tokens selected by format, without trimming.</summary>
    public static Schema<bool> BooleanText(BooleanTextFormat format = BooleanTextFormat.ZeroOne)
    {
        if (format != BooleanTextFormat.ZeroOne && format != BooleanTextFormat.TrueFalse &&
            format != BooleanTextFormat.Either)
        {
            throw new ArgumentOutOfRangeException(nameof(format));
        }

        return new Schema<bool>(new BooleanReader(format));
    }

    /// <summary>Bind a property to a schema and retain its output type.</summary>
    public static Field<T> Field<T>(string name, Schema<T> schema)
    {
        return new Field<T>(name, schema);
    }

    /// <summary>Read elements in source order, including accepted null positions.</summary>
    public static Schema<ValidatedArray<T>> Array<T>(Schema<T> element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return new Schema<ValidatedArray<T>>(new ArrayReader<T>(element));
    }

    /// <summary>Bind a schema to an XML attribute.</summary>
    public static Field<T> Attribute<T>(string name, Schema<T> schema)
    {
        return new Field<T>(name, schema, new FieldSelector(FieldSource.Attribute, name));
    }

    /// <summary>Bind a collection schema to supplied relationships of an exact type.</summary>
    public static Field<T> Relationships<T>(string itemType, Schema<T> collection)
    {
        ArgumentNullException.ThrowIfNull(collection);

        if (!collection.Reader.ReadsCollection)
        {
            throw new ArgumentException("Relationships requires a collection reader.",
                nameof(collection));
        }

        return new Field<T>(itemType, collection,
            new FieldSelector(FieldSource.Relationship, itemType));
    }

    /// <summary>Declare an object shape; undeclared properties are allowed by default.</summary>
    [SuppressMessage("Naming", "CA1720:Identifier contains type name",
        Justification = "Names the value type described by the schema.")]
    public static ObjectSchema Object(params Field[] fields)
    {
        return new ObjectSchema(fields, UnknownPropertyHandling.Allow);
    }

    /// <summary>Declare an exact Item type; undeclared properties are allowed by default.</summary>
    public static ItemSchema Item(string itemType, params Field[] fields)
    {
        return new ItemSchema(itemType, new ObjectSchema(fields, UnknownPropertyHandling.Allow));
    }

    /// <summary>Check an inclusive lower bound on an ordered value type.</summary>
    public static Rule<T> Min<T>(T value, string? message = null) where T : struct, IComparable<T>
    {
        return new Rule<T>(new BoundRule<T>(IssueCode.Minimum, value, message));
    }

    /// <summary>Check an inclusive upper bound on an ordered value type.</summary>
    public static Rule<T> Max<T>(T value, string? message = null) where T : struct, IComparable<T>
    {
        return new Rule<T>(new BoundRule<T>(IssueCode.Maximum, value, message));
    }

    /// <summary>Require a non-whitespace character without changing the string.</summary>
    public static Rule<string> NotBlank(string? message = null)
    {
        return new Rule<string>(new NotBlankRule(message));
    }

    /// <summary>Check a minimum length in UTF-16 code units.</summary>
    public static Rule<string> MinLength(int length, string? message = null)
    {
        return new Rule<string>(new LengthRule(IssueCode.MinimumLength, length, message));
    }

    /// <summary>Check a maximum length in UTF-16 code units.</summary>
    public static Rule<string> MaxLength(int length, string? message = null)
    {
        return new Rule<string>(new LengthRule(IssueCode.MaximumLength, length, message));
    }

    /// <summary>Match a culture-invariant regex with a 25 ms deadline.</summary>
    public static Rule<string> Matches(string pattern, string? message = null)
    {
        return new Rule<string>(new PatternRule(pattern, message));
    }

    /// <summary>Create a reusable predicate; caller exceptions propagate.</summary>
    public static Rule<T> Check<T>(Func<T, bool> predicate, string ruleId, string message)
    {
        return new Rule<T>(new CustomRule<T>(predicate, ruleId, message));
    }

    /// <summary>Check an inclusive lower bound on the current output.</summary>
    public static Schema<T> Min<T>(this Schema<T> schema, T value, string? message = null)
        where T : struct, IComparable<T>
    {
        ArgumentNullException.ThrowIfNull(schema);
        return schema.Check(Min(value, message));
    }

    /// <summary>Check an inclusive upper bound on the current output.</summary>
    public static Schema<T> Max<T>(this Schema<T> schema, T value, string? message = null)
        where T : struct, IComparable<T>
    {
        ArgumentNullException.ThrowIfNull(schema);
        return schema.Check(Max(value, message));
    }

    /// <summary>Check the current string for a non-whitespace character.</summary>
    public static Schema<string> NotBlank(this Schema<string> schema, string? message = null)
    {
        ArgumentNullException.ThrowIfNull(schema);
        return schema.Check(NotBlank(message));
    }

    /// <summary>Check the current string's minimum length in UTF-16 code units.</summary>
    public static Schema<string> MinLength(this Schema<string> schema, int length,
        string? message = null)
    {
        ArgumentNullException.ThrowIfNull(schema);
        return schema.Check(MinLength(length, message));
    }

    /// <summary>Check the current string's maximum length in UTF-16 code units.</summary>
    public static Schema<string> MaxLength(this Schema<string> schema, int length,
        string? message = null)
    {
        ArgumentNullException.ThrowIfNull(schema);
        return schema.Check(MaxLength(length, message));
    }

    /// <summary>Match the current string with a culture-invariant, bounded regex.</summary>
    public static Schema<string> Matches(this Schema<string> schema, string pattern,
        string? message = null)
    {
        ArgumentNullException.ThrowIfNull(schema);
        return schema.Check(Matches(pattern, message));
    }

    /// <summary>Require a minimum number of entries, including accepted null positions.</summary>
    public static Schema<ValidatedArray<T>> MinCount<T>(this Schema<ValidatedArray<T>> schema,
        int count, string? message = null)
    {
        ArgumentNullException.ThrowIfNull(schema);
        return schema.Check(new Rule<ValidatedArray<T>>(
            new CountRule<T>(IssueCode.MinimumCount, count, message)));
    }

    /// <summary>Require a maximum number of entries, including accepted null positions.</summary>
    public static Schema<ValidatedArray<T>> MaxCount<T>(this Schema<ValidatedArray<T>> schema,
        int count, string? message = null)
    {
        ArgumentNullException.ThrowIfNull(schema);
        return schema.Check(new Rule<ValidatedArray<T>>(
            new CountRule<T>(IssueCode.MaximumCount, count, message)));
    }

    /// <summary>Validate text in the selected representation.</summary>
    public static ValidationResult<T> Validate<T>(Schema<T> schema, string? input,
        InputFormat format = InputFormat.Json, ValidationOptions? options = null)
    {
        return format switch
        {
            InputFormat.Json => ValidateJson(schema, input, options),
            InputFormat.Xml => ValidateXml(schema, input, options),
            InputFormat.Aml => ValidateAml(schema, input, options),
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };
    }

    /// <summary>Validate text and return runtime output or a structured error.</summary>
    public static SafeParseResult SafeParse<T>(Schema<T> schema, string? input,
        InputFormat format = InputFormat.Json, ValidationOptions? options = null)
    {
        return OutputMaterializer.Parse(Validate(schema, input, format, options),
            options?.CancellationToken ?? default);
    }

    /// <summary>Return parsed text output or throw ValidationException for invalid input.</summary>
    public static dynamic? Parse<T>(Schema<T> schema, string? input,
        InputFormat format = InputFormat.Json, ValidationOptions? options = null)
    {
        return OutputMaterializer.Require(SafeParse(schema, input, format, options));
    }

    /// <summary>Validate JSON and return issues and typed output.</summary>
    public static ValidationResult<T> ValidateJson<T>(Schema<T> schema, string? json,
        ValidationOptions? options = null)
    {
        return ValidationEngine.Text(schema, json, InputFormat.Json, options);
    }

    /// <summary>Validate XML using explicit property, attribute and collection schemas.</summary>
    public static ValidationResult<T> ValidateXml<T>(Schema<T> schema, string? xml,
        ValidationOptions? options = null)
    {
        return ValidationEngine.Text(schema, xml, InputFormat.Xml, options);
    }

    /// <summary>Validate one Item from AML text without requiring an IOM context.</summary>
    public static ValidationResult<T> ValidateAml<T>(Schema<T> schema, string? aml,
        ValidationOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(schema);
        return ValidationEngine.Text(schema, aml, InputFormat.Xml, options,
            schema.Reader.ReadsCollection ? ArasInput.AmlCollection : ArasInput.SingleAml);
    }

    /// <summary>Validate supplied Item XML without querying or changing the source.</summary>
    public static ValidationResult<T> Validate<T>(Schema<T> schema, object input,
        ValidationOptions? options = null)
    {
        return ArasInput.Read(schema, input, options);
    }

    /// <summary>Parse supplied Item XML and return data or validation issues.</summary>
    public static SafeParseResult SafeParse<T>(Schema<T> schema, object input,
        ValidationOptions? options = null)
    {
        return OutputMaterializer.Parse(ArasInput.Read(schema, input, options),
            options?.CancellationToken ?? default);
    }

    /// <summary>Return parsed Item data or throw ValidationException for invalid input.</summary>
    public static dynamic? Parse<T>(Schema<T> schema, object input,
        ValidationOptions? options = null)
    {
        return OutputMaterializer.Require(SafeParse(schema, input, options));
    }
}
