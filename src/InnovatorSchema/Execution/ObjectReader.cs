using System;
using System.Collections.Generic;

namespace InnovatorSchema;

internal static class ObjectReader
{
    public static ValidationValue<ValidatedObject> Read(
        ObjectValueReader schema, InputNode input, ValidationContext context)
    {
        if (!input.CanReadObject)
        {
            context.Add(IssueCode.TypeMismatch, "An object is required.");
            return ValidationValue<ValidatedObject>.Invalid();
        }

        int before = context.IssueCount;
        IReadOnlySet<string>? duplicates = CheckMembers(schema, input, context);
        var values = new FieldValue[schema.Fields.Count];

        for (int index = 0; index < values.Length; index++)
        {
            Field field = schema.Fields[index];
            if (context.IsStopped)
            {
                break;
            }

            if (field.Selector.Source == FieldSource.Property &&
                duplicates?.Contains(field.Name) == true)
            {
                continue;
            }

            if (field.Selector.Source == FieldSource.Relationship &&
                duplicates?.Contains("Relationships") == true)
            {
                continue;
            }

            values[index] = field.Read(input, context);
        }

        if (context.IssueCount != before || context.IsStopped)
        {
            return ValidationValue<ValidatedObject>.Invalid();
        }

        return ValidationValue<ValidatedObject>.Present(new ValidatedObject(schema, values));
    }

    private static IReadOnlySet<string>? CheckMembers(
        ObjectValueReader schema, InputNode input, ValidationContext context)
    {
        IReadOnlySet<string>? duplicates = input.DuplicateProperties;

        if (duplicates is not null)
        {
            foreach (string name in duplicates)
            {
                using var scope = context.Enter(new PathSegment(PathSegmentKind.Property, name));
                context.Add(IssueCode.DuplicateProperty, "Property occurs more than once.");
            }
        }

        if (schema.UnknownPropertyHandling == UnknownPropertyHandling.Allow || context.IsStopped)
        {
            return duplicates;
        }

        SortedSet<string>? unknown = null;

        foreach (InputMember member in input.Members)
        {
            if (!schema.DeclaredProperties.Contains(member.Name))
            {
                unknown ??= new SortedSet<string>(StringComparer.Ordinal);
                unknown.Add(member.Name);
            }
        }

        if (unknown is not null)
        {
            foreach (string name in unknown)
            {
                using var scope = context.Enter(new PathSegment(PathSegmentKind.Property, name));
                context.Add(IssueCode.UnknownProperty, "Property is not declared by this schema.");
            }
        }

        return duplicates;
    }
}
