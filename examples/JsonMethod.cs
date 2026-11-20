var schema = V.Object()
    .Property("item_number", V.String().NotBlank("Enter an item number.").MaxLength(64))
    .Property("quantity", V.Decimal().Min(0.001m));
var result = V.Validate(schema, this.getProperty("payload"));

if (!result.IsValid)
{
    return this.getInnovator().newError(result.GetErrorString());
}

return this;
