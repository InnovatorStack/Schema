var schema = V.Item("Part")
    .Property("item_number", V.String().MaxLength(64))
    .Property("weight", V.Integer().Min(0));

var (item, inn) = V.ParseInput(schema, this);
var itemNo = item["item_number"];
var weight = item.weight;

return inn.newResult(itemNo + " weighs " + weight);
