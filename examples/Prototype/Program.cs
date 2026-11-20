using System;
using System.Linq;
using System.Text.Json;
using V = InnovatorSchema.Validator;

namespace InnovatorSchema.Examples;

internal static class Program
{
    private static int Main(string[] args)
    {
        var code = V.Field("code", V.String().NotBlank().Transform(value => value.Trim())
            .Matches("^P-"));
        var quantity = V.Field("quantity", V.Decimal().Min(0.001m));
        var line = V.Object(code, quantity).Transform(row => new
        {
            Code = row.Get(code),
            Quantity = row.Get(quantity)
        });
        var lines = V.Field("lines", V.Array(line).MinCount(1));
        var order = V.Object(lines).Transform(row =>
        {
            var values = row.Get(lines).GetValues();

            return new
            {
                Lines = values,
                TotalQuantity = values.Sum(value => value.Quantity)
            };
        });

        if (args.Length == 2 && args[0] == "json")
        {
            return Show("JSON", V.ValidateJson(order, args[1])) ? 0 : 1;
        }

        if (args.Length == 2 && args[0] == "aml")
        {
            return ShowAml(args[1]) ? 0 : 1;
        }

        if (args.Length != 0)
        {
            Console.Error.WriteLine(
                "Usage: json '<JSON>' or aml '<AML>'; no arguments runs examples.");
            return 2;
        }

        Show("Valid JSON", V.ValidateJson(order,
            "{\"lines\":[{\"code\":\" P-100 \",\"quantity\":2.5}]}"));
        Show("Invalid JSON", V.ValidateJson(order,
            "{\"lines\":[{\"code\":\"X-100\",\"quantity\":0}]}"));
        ShowAml("<Item type='Part'><item_number>P-100</item_number><Relationships>" +
            "<Item type='Part BOM'><quantity>2.5</quantity></Item></Relationships></Item>");
        ShowAml("<Item type='Part'><item_number>P-100</item_number><Relationships>" +
            "<Item type='Part BOM'><quantity>0</quantity></Item></Relationships></Item>");

        return 0;
    }

    private static bool ShowAml(string aml)
    {
        var number = V.Field("item_number", V.String().NotBlank());
        var bom = V.Relationships("Part BOM", V.Array(V.Item("Part BOM")
            .Property("quantity", V.DecimalText().Min(0.001m))));
        var part = V.Item("Part", number, bom).Transform(row => new
        {
            Number = row.Get(number),
            BomCount = row.Get(bom).Count
        });

        return Show("AML", V.ValidateAml(part, aml));
    }

    private static bool Show<T>(string label, ValidationResult<T> result)
    {
        Console.WriteLine(label + ": " + (result.IsValid ? "valid" : "invalid"));

        if (result.HasOutput)
        {
            Console.WriteLine(JsonSerializer.Serialize(result.Output));
        }

        foreach (ValidationIssue issue in result.Issues)
        {
            Console.WriteLine(issue.Path + " [" + issue.Code + "] " + issue.Message);
        }

        Console.WriteLine();
        return result.IsValid;
    }
}
