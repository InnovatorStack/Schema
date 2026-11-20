using System.Collections.Generic;

namespace InnovatorSchema.HostTests;

internal static partial class ServerMethodCases
{
    private static IEnumerable<ServerMethodCase> JsonCases()
    {
        yield return Json("minimal", Order()) with
        {
            Output = "{\"ItemNumber\":\"P-100\",\"Quantity\":2.5,\"CountState\":\"Missing\"," +
                "\"NoteState\":\"Missing\",\"NullableOutputState\":\"Missing\",\"LineCount\":0}"
        };
        yield return Json("trim", Order("\" P-100 \"")) with
        {
            Output = "{\"ItemNumber\":\"P-100\"}"
        };
        yield return Json("unicode", Order("\"P-\\uD83D\\uDE80\"")) with
        {
            Output = "{\"ItemNumber\":\"P-🚀\"}"
        };
        yield return Json("missing fields", "{}",
            new ExpectedIssue("Required", "$.item_number", "Enter an item number."),
            new ExpectedIssue("Required", "$.quantity"));
        yield return Json("null", Order("null"),
            new ExpectedIssue("NullNotAllowed", "$.item_number"));
        yield return Json("number type", Order("12"),
            new ExpectedIssue("TypeMismatch", "$.item_number"));
        yield return Json("blank", Order("\" \""),
            new ExpectedIssue("NotBlank", "$.item_number", "Enter an item number."),
            new ExpectedIssue("Pattern", "$.item_number", "Start with P-."));
        yield return Json("prefix", Order("\"X-100\""),
            new ExpectedIssue("Pattern", "$.item_number", "Start with P-."));
        yield return Json("length boundary", Order("\"P-" + new string('x', 62) + "\""));
        yield return Json("too long", Order("\"P-" + new string('x', 63) + "\""),
            new ExpectedIssue("MaximumLength", "$.item_number", "Use at most 64 characters."));

        foreach (ServerMethodCase test in JsonNumbers())
        {
            yield return test;
        }

        foreach (ServerMethodCase test in JsonBooleans())
        {
            yield return test;
        }

        foreach (ServerMethodCase test in JsonPresenceAndCollections())
        {
            yield return test;
        }

        foreach (ServerMethodCase test in JsonFailures())
        {
            yield return test;
        }
    }

    private static IEnumerable<ServerMethodCase> JsonNumbers()
    {
        yield return Json("minimum", Order(quantity: "0.001"));
        yield return Json("maximum", Order(quantity: "1000"));
        yield return Json("below minimum", Order(quantity: "0"),
            new ExpectedIssue("Minimum", "$.quantity", "Quantity must be at least 0.001."));
        yield return Json("above maximum", Order(quantity: "1000.001"),
            new ExpectedIssue("Maximum", "$.quantity", "Quantity must not exceed 1000."));
        yield return Json("native number requires number", Order(quantity: "\"2.5\""),
            new ExpectedIssue("TypeMismatch", "$.quantity"));
        yield return Json("exact exponent", Order(quantity: "25e-1")) with
        {
            Output = "{\"Quantity\":2.5}"
        };
        yield return Json("decimal precision", Order(quantity: "1.00000000000000000000000000001"),
            new ExpectedIssue("InvalidNumber", "$.quantity"));
        yield return Json("decimal overflow", Order(quantity: "1e30"),
            new ExpectedIssue("InvalidNumber", "$.quantity"));
        yield return Json("integer maximum", Order(extra: ",\"count\":9223372036854775807")) with
        {
            Output = "{\"Count\":9223372036854775807}"
        };
        yield return Json("integer exponent", Order(extra: ",\"count\":10e1")) with
        {
            Output = "{\"Count\":100}"
        };
        yield return Json("fractional integer", Order(extra: ",\"count\":1.01"),
            new ExpectedIssue("InvalidNumber", "$.count"));
        yield return Json("integer overflow", Order(extra: ",\"count\":9223372036854775808"),
            new ExpectedIssue("InvalidNumber", "$.count"));
        yield return Json("text decimal", Order(extra: ",\"text_number\":\"2.50e-1\"")) with
        {
            Output = "{\"TextNumber\":0.25}"
        };
        yield return Json("text decimal representation", Order(extra: ",\"text_number\":2.5"),
            new ExpectedIssue("TypeMismatch", "$.text_number"));

        foreach (string token in new[] { "\" 2\"", "\"2,5\"", "\"NaN\"", "\"\"" })
        {
            yield return Json("invalid decimal " + token, Order(extra: ",\"text_number\":" + token),
                new ExpectedIssue("InvalidNumber", "$.text_number"));
        }
    }

    private static IEnumerable<ServerMethodCase> JsonBooleans()
    {
        foreach (string token in new[] { "true", "false" })
        {
            yield return Json("native boolean " + token, Order(extra: ",\"flag\":" + token)) with
            {
                Output = "{\"Flag\":" + token + "}"
            };
        }

        foreach (string token in new[] { "1", "\"true\"" })
        {
            yield return Json("invalid native boolean " + token, Order(extra: ",\"flag\":" + token),
                new ExpectedIssue("TypeMismatch", "$.flag"));
        }

        foreach (string token in new[] { "0", "1", "false", "true" })
        {
            yield return Json("text boolean " + token,
                Order(extra: ",\"text_flag\":\"" + token + "\"")) with
            {
                Output = "{\"TextFlag\":" + (token is "1" or "true" ? "true" : "false") + "}"
            };
        }

        yield return Json("boolean case", Order(extra: ",\"text_flag\":\"True\""),
            new ExpectedIssue("InvalidBoolean", "$.text_flag"));
    }

    private static IEnumerable<ServerMethodCase> JsonPresenceAndCollections()
    {
        yield return Json("nullable fields", Order(extra: ",\"count\":null,\"note\":null")) with
        {
            Output = "{\"CountState\":\"Null\",\"NoteState\":\"Null\"}"
        };
        yield return Json("present fields", Order(extra: ",\"count\":2,\"note\":\"\"")) with
        {
            Output = "{\"CountState\":\"Present\",\"NoteState\":\"Present\"}"
        };
        yield return Json("nonnullable flag", Order(extra: ",\"flag\":null"),
            new ExpectedIssue("NullNotAllowed", "$.flag"));
        yield return Json("null transform rejected", Order(extra: ",\"unexpected_null\":\"x\""),
            new ExpectedIssue("NullNotAllowed", "$.unexpected_null"));
        yield return Json("null transform accepted",
            Order(extra: ",\"permitted_null\":\"null\"")) with
        {
            Output = "{\"NullableOutputState\":\"Null\"}"
        };
        yield return Json("nested lines", Order(extra: ",\"lines\":[" + Order() + "]")) with
        {
            Output = "{\"LineCount\":1}"
        };
        yield return Json("nested missing", Order(extra: ",\"lines\":[{}]"),
            new ExpectedIssue("Required", "$.lines[0].item_number", "Enter an item number."),
            new ExpectedIssue("Required", "$.lines[0].quantity"));
        yield return Json("array required", Order(extra: ",\"lines\":{}"),
            new ExpectedIssue("TypeMismatch", "$.lines"));
        yield return Json("empty array", Order(extra: ",\"lines\":[]"),
            new ExpectedIssue("MinimumCount", "$.lines"));
        string rows = string.Join(",", System.Linq.Enumerable.Repeat(Order(), 11));
        yield return Json("too many rows", Order(extra: ",\"lines\":[" + rows + "]"),
            new ExpectedIssue("MaximumCount", "$.lines"));
        yield return Json("root check", Order("\"P-LIMIT\"", "101"),
            new ExpectedIssue("Custom", "$",
                "P-LIMIT allows a quantity of at most 100.", "order.limit"));
    }

    private static IEnumerable<ServerMethodCase> JsonFailures()
    {
        yield return Json("unknown property", Order(extra: ",\"surprise\":1"),
            new ExpectedIssue("UnknownProperty", "$.surprise"));
        yield return Json("duplicate", Order(extra: ",\"quantity\":3"),
            new ExpectedIssue("DuplicateProperty", "$.quantity"));
        yield return Json("root type", "[]", new ExpectedIssue("TypeMismatch", "$"));
        yield return Json("root null", "null", new ExpectedIssue("NullNotAllowed", "$"));
        yield return Json("malformed", "{", new ExpectedIssue("MalformedJson", "$"));
        yield return Json("trailing comma", "{\"item_number\":\"P-100\",}",
            new ExpectedIssue("MalformedJson", "$"));
        yield return Json("unpaired value surrogate", Order("\"P-\\uD800\""),
            new ExpectedIssue("MalformedJson", "$"));
        yield return Json("unpaired key surrogate", Order(extra: ",\"\\uD800\":1"),
            new ExpectedIssue("MalformedJson", "$"));
        yield return Json("first issue", "{}",
            new ExpectedIssue("Required", "$.item_number", "Enter an item number.")) with
        {
            Options = Option("first_issue")
        };
        yield return Json("issue bound", "{\"item_number\":\" \",\"quantity\":0}",
            new ExpectedIssue("NotBlank", "$.item_number", "Enter an item number."),
            new ExpectedIssue("LimitExceeded", "$.quantity")) with
        { Options = Option("limit_issues") };
        yield return Json("operation bound", Order(),
            new ExpectedIssue("LimitExceeded", "$.item_number")) with
        {
            Options = Option("limit_operations")
        };
        yield return Json("Aras error", "{}") with
        {
            Error = "$.item_number: Enter an item number."
        };
        yield return Json("escaped error path", Order(extra: ",\"a<&b\":1")) with
        {
            Error = "$[\"a\\u003C\\u0026b\"]: Property is not declared by this schema."
        };
    }
}
