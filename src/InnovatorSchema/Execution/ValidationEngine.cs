using System;
using System.Text.Json;
using System.Xml;

namespace InnovatorSchema;

internal static class ValidationEngine
{
    public static ValidationResult<T> Text<T>(
        Schema<T> schema, string? text, InputFormat format, ValidationOptions? options = null,
        Func<InputNode, ValidationContext, InputNode>? prepareRoot = null)
    {
        ArgumentNullException.ThrowIfNull(schema);
        options ??= new ValidationOptions();

        InputDocument Open()
        {
            if (text is null)
            {
                return InputDocument.Missing();
            }

            if (format == InputFormat.Json)
            {
                return InputDocument.Json(text, options);
            }

            return InputDocument.Xml(text, options);
        }

        return Execute(schema, Open, options, prepareRoot);
    }

    public static ValidationResult<T> Element<T>(
        Schema<T> schema, XmlElement element, ValidationOptions? options = null,
        Func<InputNode, ValidationContext, InputNode>? prepareRoot = null)
    {
        ArgumentNullException.ThrowIfNull(schema);
        options ??= new ValidationOptions();

        return Execute(schema, () => InputDocument.FromXml(element, options), options, prepareRoot);
    }

    public static ValidationResult<T> Failure<T>(
        IssueCode code, string message, ValidationOptions? options = null)
    {
        var context = new ValidationContext(options ?? new ValidationOptions());
        context.Add(code, message);

        return Finish(context, ValidationValue<T>.Invalid());
    }

    public static ValidationResult<T> Execute<T>(
        Schema<T> schema, Func<InputDocument> open, ValidationOptions options,
        Func<InputNode, ValidationContext, InputNode>? prepareRoot = null)
    {
        var context = new ValidationContext(options);
        ValidationValue<T> output = ValidationValue<T>.Invalid();
        using InputDocument? document = OpenDocument(open, context);

        if (document is null)
        {
            return Finish(context, output);
        }

        InputNode root = document.Root;

        if (prepareRoot is not null && root.Kind != InputKind.Missing)
        {
            root = prepareRoot(root, context);
        }

        if (context.IssueCount == 0)
        {
            output = schema.Read(root, context);
        }

        return Finish(context, output);
    }

    private static InputDocument? OpenDocument(
        Func<InputDocument> open, ValidationContext context)
    {
        try
        {
            return open();
        }
        catch (JsonException exception)
        {
            context.AddInput(new ValidationIssue(
                IssueCode.MalformedJson, "JSON syntax is malformed.", RootPath(),
                lineNumber: Position(exception.LineNumber),
                columnNumber: Position(exception.BytePositionInLine)));
        }
        catch (XmlException exception)
        {
            context.AddInput(new ValidationIssue(
                IssueCode.MalformedXml, "XML syntax is malformed or prohibited.", RootPath(),
                lineNumber: exception.LineNumber, columnNumber: exception.LinePosition));
        }
        catch (InputFailureException exception)
        {
            context.AddInput(
                new ValidationIssue(exception.Code, exception.Message, exception.Path));
        }

        return null;
    }

    private static ValidationResult<T> Finish<T>(
        ValidationContext context, ValidationValue<T> output)
    {
        context.Options.CancellationToken.ThrowIfCancellationRequested();

        if (context.IssueCount != 0)
        {
            output = ValidationValue<T>.Invalid();
        }

        return new ValidationResult<T>(context.Freeze(), output);
    }

    private static IssuePath RootPath()
    {
        return new IssuePath(Array.Empty<PathSegment>());
    }

    private static int? Position(long? value)
    {
        if (!value.HasValue || value < 0 || value >= int.MaxValue)
        {
            return null;
        }

        return checked((int)value.Value + 1);
    }
}
