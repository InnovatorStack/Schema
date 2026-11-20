var part = V.Item("Part")
    .Property("item_number", V.String().NotBlank())
    .Property("weight", V.DecimalText().Min(0m).Optional());
var schema = V.Array(part).MinCount(1);
var parts = V.Parse(schema, this.getItemsByXPath("parts/Item"));
return this.getInnovator().newResult(parts[0].item_number);
