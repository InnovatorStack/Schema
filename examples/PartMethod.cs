var schema = V.Item("Part")
    .Property("item_number", V.String().NotBlank("Enter an item number.").MaxLength(64))
    .Property("weight", V.DecimalText().Min(0m).Optional());
var result = V.Validate(schema, this);

if (!result.IsValid)
{
    return this.getInnovator().newError(result.GetErrorString());
}

return this;
