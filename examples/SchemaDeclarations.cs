using InnovatorSchema;
using V = InnovatorSchema.Validator;

namespace Company.Validation
{
    public static class InlineSchemas
    {
        public static ItemSchema Part()
        {
            return V.Item("Part")
                .Property("item_number", V.String().NotBlank().MaxLength(64))
                .Property("weight", V.DecimalText().Min(0m).Optional());
        }

        public static ObjectSchema Order()
        {
            return V.Object()
                .Property("item_number", V.String().NotBlank().MaxLength(64))
                .Property("quantity", V.Decimal().Min(0.001m))
                .Property("expedite", V.Boolean().Optional());
        }
    }

    public sealed class PartSchema : Schema<ValidatedObject>
    {
        public static PartSchema Instance { get; } = new PartSchema();

        public PartSchema()
            : base(InlineSchemas.Part())
        {
        }
    }

    public sealed class OrderSchema : Schema<ValidatedObject>
    {
        public static OrderSchema Instance { get; } = new OrderSchema();

        public OrderSchema()
            : base(InlineSchemas.Order())
        {
        }
    }
    public sealed class CodeSchema : Schema<string>
    {
        public static CodeSchema Instance { get; } = new CodeSchema();

        public CodeSchema() : base(V.String()
            .Transform(value => value.Trim()).NotBlank().MaxLength(64).Matches("^P-"))
        {
        }
    }
}
