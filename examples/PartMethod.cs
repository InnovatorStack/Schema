var schema = V.Item("Part")
    .ID()
    .Attribute("action", V.String().Enum("edit", "add", "update"))
    .Property("item_number", V.String().MaxLength(64).Optional())
    .Property("related_id", V.Item("Part").ID())
    .Property("weight", V.Integer().Min(0, "Weight must be positive or zero."));

V.AssertInput(schema, this);
return this;
