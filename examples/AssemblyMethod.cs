var code = V.String()
    .Transform(value => value.Trim())
    .NotBlank("Enter an item number.")
    .MaxLength(64);

var document = V.Item("Document")
    .Property("item_number", code)
    .Property("major_rev", V.String().Matches("^[A-Z]{1,2}$", "Use an uppercase revision."));

var drawing = V.Item("Part Document")
    .Property("related_id", document);

var component = V.Item("Part")
    .Property("item_number", code)
    .Property("weight", V.DecimalText()
        .Min(0m, "Weight cannot be negative.")
        .Optional().Nullable())
    .Property("is_released", V.BooleanText().Optional())
    .Property(V.Relationships("Part Document", V.Array(drawing).MinCount(1).MaxCount(5)));

var line = V.Item("Part BOM")
    .Property("quantity", V.DecimalText().Min(0.001m).Max(10000m))
    .Property("sort_order", V.IntegerText().Min(1L))
    .Property("related_id", component);

var schema = V.Item("Part")
    .Property(V.Attribute("action", V.String().NotBlank()))
    .Property("item_number", code)
    .Property("name", V.String().NotBlank().MaxLength(128))
    .Property("description", V.String().Optional().Nullable())
    .Property(V.Relationships("Part BOM", V.Array(line).MinCount(1).MaxCount(100)));

var assembly = V.Parse(schema, this);
decimal totalWeight = 0m;

foreach (var row in assembly.Relationships["Part BOM"])
{
    totalWeight += row.quantity * (row.related_id.weight ?? 0m);
}

return this.getInnovator().newResult(
    totalWeight.ToString(System.Globalization.CultureInfo.InvariantCulture));
