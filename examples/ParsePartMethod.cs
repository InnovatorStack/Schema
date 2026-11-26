var schema = V.Item("Part")
    .Property("item_number", V.String().Max(64))
    .Property("weight", V.Integer().Min(0));

var item = V.ParseInput(schema, this);
var itemNo = item["item_number"];
var weight = item.weight;

return this.getInnovator().newResult(itemNo + " weighs " + weight);
