var schema = V.Item("Part")
    .ID()
    .Attribute("action", V.Enum("edit", "add", "update"))
    .Property("item_number", V.String().MaxLength(64).Nullish())
    .Property("related_id", V.Item("Part").ID())
    .Property("weight", V.Integer().Min(0, "Weight must be positive or zero."));

V.AssertInput(schema, this);
return this;
