var part = V.Item("Part")
    .ID()
    .Property("item_number", V.String().MaxLength(64))
    .Property("weight", V.Double().Min(0.0).Max(10000.0));

var line = V.Item("Part BOM")
    .Property("quantity", V.Double().Min(0.0).Max(10000.0))
    .Property("reference_designator", V.String().MaxLength(64).Optional())
    .Property("related_id", part);

var (lines, inn) = V.ParseInput(V.Array(line).Min(1).Max(100), this);
var summary = new System.Text.StringBuilder();
var culture = System.Globalization.CultureInfo.InvariantCulture;
double total = 0;

foreach (var row in lines)
{
    var related = row.related_id;
    string itemNumber = related["item_number"];
    double quantity = row.quantity;
    double weight = related.weight;
    string reference = row.reference_designator;
    double lineWeight = quantity * weight;
    total += lineWeight;

    summary.Append(itemNumber);

    if (reference != null)
    {
        summary.Append(" (").Append(reference).Append(')');
    }

    summary.Append(": ").Append(lineWeight.ToString("0.##", culture)).Append('\n');
}

summary.Append("Total weight: ").Append(total.ToString("0.##", culture));
return inn.newResult(summary.ToString());
