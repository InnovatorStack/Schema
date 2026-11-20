var inn = this.getInnovator();
var number = InnovatorSchema.Validator.Field("item_number", InnovatorSchema.Validator.String()
    .Transform(value => value.Trim())
    .Check(InnovatorSchema.Validator.NotBlank("Enter an item number."))
    .Check(InnovatorSchema.Validator.MaxLength(64)));
var quantity = InnovatorSchema.Validator.Field("quantity", InnovatorSchema.Validator.DecimalText()
    .Check(InnovatorSchema.Validator.Min(0.001m, "Quantity must be at least 0.001.")));
var related = InnovatorSchema.Validator.Field("related_id",
    InnovatorSchema.Validator.Item("Part", number));
var line = InnovatorSchema.Validator.Item("Part BOM", quantity, related);
var rows = InnovatorSchema.Validator.MinCount(InnovatorSchema.Validator.Array(line), 1)
    .Check(values => values.Count <= 100, "bom.limit", "Use at most 100 BOM rows.");
var bom = InnovatorSchema.Validator.Relationships("Part BOM", rows);
var action = InnovatorSchema.Validator.Attribute("action", InnovatorSchema.Validator.String()
    .Check(value => value == "add", "action.add", "An add action is required."));
var weight = InnovatorSchema.Validator.Field("weight",
    InnovatorSchema.Validator.DecimalText().Optional().Nullable());
var flag = InnovatorSchema.Validator.Field("flag",
    InnovatorSchema.Validator.BooleanText().Optional());
var schema = InnovatorSchema.Validator.Item("Part", number, action, weight, flag, bom);
string payload = this.getProperty("payload");
Aras.IOM.Item supplied = null;
string before = null;
if (this.getProperty("input") == "iom")
{
    supplied = inn.newItem();
    supplied.loadAML(payload);
    before = supplied.ToString();
}
var result = supplied == null ?
    InnovatorSchema.Validator.ValidateAml(schema, payload) :
    InnovatorSchema.Validator.Validate(schema, supplied);
if (this.getProperty("return_error") == "1" && !result.IsValid)
{
    return inn.newError(result.GetErrorString());
}
object output = result.HasOutput ? new
{
    ItemNumber = result.Output.Get(number), RowCount = result.Output.Get(bom).Count,
    WeightState = result.Output.Read(weight).State.ToString(),
    Quantity = result.Output.Get(bom)[0].Value.Get(quantity),
    RelatedNumber = result.Output.Get(bom)[0].Value.Get(related).Get(number)
} : null;
return inn.newResult(Newtonsoft.Json.JsonConvert.SerializeObject(new
{
    Valid = result.IsValid, HasOutput = result.HasOutput, Output = output,
    InputUnchanged = supplied == null || before == supplied.ToString(),
    LibraryMvid = typeof(InnovatorSchema.Validator).Assembly.ManifestModule.ModuleVersionId,
    Runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
    Issues = result.Issues.Select(issue => new
    {
        Code = issue.Code.ToString(), Path = issue.Path.ToString(),
        Message = issue.Message, RuleId = issue.RuleId
    }).ToArray()
}));
