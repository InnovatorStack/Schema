var inn = this.getInnovator();
var name = InnovatorSchema.Validator.Field("name", InnovatorSchema.Validator.String());
var item = InnovatorSchema.Validator.Field("Item", InnovatorSchema.Validator.Object(name));
var label = InnovatorSchema.Validator.Field("label",
    InnovatorSchema.Validator.String().Optional());
var action = InnovatorSchema.Validator.Attribute("action",
    InnovatorSchema.Validator.String().Optional());
var schema = InnovatorSchema.Validator.Object(item, label, action)
    .UnknownProperties(InnovatorSchema.UnknownPropertyHandling.Reject);
var result = InnovatorSchema.Validator.ValidateXml(schema, this.getProperty("payload"));
if (this.getProperty("return_error") == "1" && !result.IsValid)
{
    return inn.newError(result.GetErrorString());
}
object output = result.HasOutput ? new
{
    Name = result.Output.Get(item).Get(name),
    LabelState = result.Output.Read(label).State.ToString(),
    Label = result.Output.Read(label).HasValue ? result.Output.Get(label) : null,
    Action = result.Output.Read(action).HasValue ? result.Output.Get(action) : null
} : null;
return inn.newResult(Newtonsoft.Json.JsonConvert.SerializeObject(new
{
    Valid = result.IsValid, HasOutput = result.HasOutput, Output = output,
    LibraryMvid = typeof(InnovatorSchema.Validator).Assembly.ManifestModule.ModuleVersionId,
    Runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
    Issues = result.Issues.Select(issue => new
    {
        Code = issue.Code.ToString(), Path = issue.Path.ToString(),
        Message = issue.Message, RuleId = issue.RuleId
    }).ToArray()
}));
