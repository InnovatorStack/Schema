var inn = this.getInnovator();
var text = InnovatorSchema.Validator.String().Transform(value => value.Trim())
    .Check(InnovatorSchema.Validator.NotBlank("Enter an item number."))
    .Check(InnovatorSchema.Validator.MaxLength(64, "Use at most 64 characters."))
    .Check(InnovatorSchema.Validator.Matches("^P-", "Start with P-."))
    .WithMessage(InnovatorSchema.IssueCode.Required, "Enter an item number.");
var number = InnovatorSchema.Validator.Field("item_number", text);
var quantity = InnovatorSchema.Validator.Field("quantity", InnovatorSchema.Validator.Decimal()
    .Check(InnovatorSchema.Validator.Min(0.001m, "Quantity must be at least 0.001."))
    .Check(InnovatorSchema.Validator.Max(1000m, "Quantity must not exceed 1000.")));
var count = InnovatorSchema.Validator.Field("count",
    InnovatorSchema.Validator.Integer().Optional().Nullable());
var flag = InnovatorSchema.Validator.Field("flag", InnovatorSchema.Validator.Boolean().Optional());
var note = InnovatorSchema.Validator.Field("note",
    InnovatorSchema.Validator.String().Optional().Nullable());
var textNumber = InnovatorSchema.Validator.Field("text_number",
    InnovatorSchema.Validator.DecimalText().Optional());
var textFlag = InnovatorSchema.Validator.Field("text_flag", InnovatorSchema.Validator
    .BooleanText(InnovatorSchema.BooleanTextFormat.Either).Optional());
var unexpectedNull = InnovatorSchema.Validator.Field("unexpected_null",
    InnovatorSchema.Validator.String().Optional().Transform<string>(value => null));
var permittedNull = InnovatorSchema.Validator.Field("permitted_null",
    InnovatorSchema.Validator.String().Optional().TransformNullable<string>(value =>
        value == "null" ? null : value));
var lines = InnovatorSchema.Validator.Field("lines", InnovatorSchema.Validator.MaxCount(
    InnovatorSchema.Validator.MinCount(InnovatorSchema.Validator.Array(
        InnovatorSchema.Validator.Object(number, quantity)), 1), 10).Optional());
var schema = InnovatorSchema.Validator.Object(number, quantity, count, flag, note,
    textNumber, textFlag, unexpectedNull, permittedNull, lines)
    .UnknownProperties(InnovatorSchema.UnknownPropertyHandling.Reject)
    .Check(row => row.Get(number) != "P-LIMIT" || row.Get(quantity) <= 100m,
        "order.limit", "P-LIMIT allows a quantity of at most 100.");
var options = new InnovatorSchema.ValidationOptions(
    mode: this.getProperty("first_issue") == "1" ?
        InnovatorSchema.ValidationMode.FirstIssue : InnovatorSchema.ValidationMode.AllIssues,
    maximumIssues: this.getProperty("limit_issues") == "1" ? 2 : 100,
    maximumOperations: this.getProperty("limit_operations") == "1" ? 1 : 100000);
var result = InnovatorSchema.Validator.ValidateJson(
    schema, this.getProperty("payload"), options);
if (this.getProperty("return_error") == "1" && !result.IsValid)
{
    return inn.newError(result.GetErrorString());
}
object output = result.HasOutput ? new
{
    ItemNumber = result.Output.Get(number), Quantity = result.Output.Get(quantity),
    Count = result.Output.Read(count).HasValue ? (long?)result.Output.Get(count) : null,
    Flag = result.Output.Read(flag).HasValue ? (bool?)result.Output.Get(flag) : null,
    TextFlag = result.Output.Read(textFlag).HasValue ? (bool?)result.Output.Get(textFlag) : null,
    TextNumber = result.Output.Read(textNumber).HasValue ?
        (decimal?)result.Output.Get(textNumber) : null,
    CountState = result.Output.Read(count).State.ToString(),
    NoteState = result.Output.Read(note).State.ToString(),
    NullableOutputState = result.Output.Read(permittedNull).State.ToString(),
    LineCount = result.Output.Read(lines).HasValue ? result.Output.Get(lines).Count : 0
} : null;
return inn.newResult(Newtonsoft.Json.JsonConvert.SerializeObject(new
{
    Valid = result.IsValid, HasOutput = result.HasOutput,
    State = result.OutputValue.State.ToString(), Output = output,
    LibraryMvid = typeof(InnovatorSchema.Validator).Assembly.ManifestModule.ModuleVersionId,
    Runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
    Issues = result.Issues.Select(issue => new
    {
        Code = issue.Code.ToString(), Path = issue.Path.ToString(),
        Message = issue.Message, RuleId = issue.RuleId
    }).ToArray()
}));
