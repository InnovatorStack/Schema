var part = V.Item("Part")
    .ID()
    .Property("weight", V.Double().Min(0.0))
    .Property("effective_date", V.Date().Optional());

V.AssertInput(V.Items(part).Min(1), this);
return this;
