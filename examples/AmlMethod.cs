var itemNumber = V.String().NotBlank("Enter an item number.").MaxLength(64);
var part = V.Item("Part")
    .Property("item_number", itemNumber);
var line = V.Item("Part BOM")
    .Property("quantity", V.DecimalText().Min(0.001m))
    .Property("related_id", part);
var schema = part.Property(V.Relationships("Part BOM", V.Array(line).MinCount(1)));

var result = V.Validate(schema, this.getProperty("payload"), InputFormat.Aml);

if (!result.IsValid)
{
    return this.getInnovator().newError(result.GetErrorString());
}

return this;
