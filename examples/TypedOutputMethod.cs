var line = V.Object()
    .Property("code", V.String().Transform(value => value.Trim()).NotBlank().Matches("^P-"))
    .Property("quantity", V.Decimal().Min(0.001m));
var schema = V.Object()
    .Property("lines", V.Array(line).MinCount(1));
var order = V.Parse(schema, this.getProperty("payload"));

decimal totalQuantity = 0m;

foreach (var entry in order.lines)
{
    totalQuantity += entry.quantity;
}

return this.getInnovator().newResult(
    totalQuantity.ToString(System.Globalization.CultureInfo.InvariantCulture));
