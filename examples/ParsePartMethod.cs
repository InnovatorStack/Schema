var schema = V.Item("Part")
    .Property("item_number", V.String().NotBlank("Enter an item number.").MaxLength(64))
    .Property("weight", V.DecimalText().Min(0m).Optional());
var part = V.Parse(schema, this);
return this.getInnovator().newResult(part.item_number);
