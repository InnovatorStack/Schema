var inn = this.getInnovator();
var number = V.Field("item_number", V.String().NotBlank("Enter an item number."));
var weight = V.Field("weight", V.DecimalText().Min(0m).Optional().Nullable());
Schema<ValidatedObject> row = V.Object(number, weight);
string format = this.getProperty("format", "json");
if (format == "aml" || format == "iom")
{
    row = V.Item("Part", number, weight);
}
var schema = V.Array(row);
if (this.getProperty("callback") == "1")
{
    schema = schema.Check(values => { throw new FormatException("Caller failure."); },
        "caller", "Caller rule.");
}
InputFormat kind;
Enum.TryParse(format, true, out kind);
var items = inn.newItem();
string input = this.getProperty("payload");
if (format == "iom")
{
    items.loadAML(input);
}
string before = items == null ? null : items.ToString();
try
{
    object output;
    ValidationError error = null;
    if (this.getProperty("call") == "parse")
    {
        output = format == "iom" ? V.Parse(schema, items) : V.Parse(schema, input, kind);
    }
    else
    {
        SafeParseResult result = format == "iom" ? V.SafeParse(schema, items) :
            V.SafeParse(schema, input, kind);
        error = result.Error;
        output = result.Success ? (object)result.Data : null;
    }
    var issues = error == null ? new ValidationIssue[0] : error.GetIssues();
    return inn.newResult(Newtonsoft.Json.JsonConvert.SerializeObject(new
    {
        Valid = error == null, Data = output,
        Summary = error == null ? "" : error.GetErrorString(),
        Issues = issues.Select(issue => new { Code = issue.Code.ToString(),
            Path = issue.Path.ToString(), Message = issue.Message }).ToArray(),
        InputUnchanged = format != "iom" || items.ToString() == before,
        LibraryMvid = typeof(V).Assembly.ManifestModule.ModuleVersionId
    }));
}
catch (ValidationException error)
{
    return inn.newResult(Newtonsoft.Json.JsonConvert.SerializeObject(new
    {
        ExceptionType = error.GetType().FullName, Summary = error.GetErrorString(),
        Issues = error.GetIssues().Select(issue => new { Code = issue.Code.ToString(),
            Path = issue.Path.ToString(), Message = issue.Message }).ToArray(),
        LibraryMvid = typeof(V).Assembly.ManifestModule.ModuleVersionId
    }));
}
catch (FormatException error)
{
    return inn.newResult(Newtonsoft.Json.JsonConvert.SerializeObject(new
    {
        ExceptionType = error.GetType().FullName, error.Message,
        LibraryMvid = typeof(V).Assembly.ManifestModule.ModuleVersionId
    }));
}
