var part = V.Item("Part")
    .ID()
    .Property("weight", V.Double().Min(0.0))
    .Property("effective_date", V.Date().Nullish());

V.AssertInput(V.Array(part).Min(1), this);
return this;
