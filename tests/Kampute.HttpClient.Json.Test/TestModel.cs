namespace Kampute.HttpClient.Json.Test
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Text.RegularExpressions;

    public class TestModel
    {
        public string? Name { get; set; }

        public override bool Equals(object? obj)
        {
            return obj is TestModel other && Name == other.Name;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name);
        }

        public string ToJsonString()
        {
            return $"{{\"name\":{ToJson(Name)}}}";

            static string ToJson(string? value)
            {
                if (value is null)
                    return "null";

                var escaped = Regex.Replace(value, @"[\u0000-\u001F\\""]", match =>
                {
                    var c = match.Value[0];
                    return c switch
                    {
                        '\\' => @"\\",
                        '\"' => @"\""",
                        '\n' => @"\n",
                        '\r' => @"\r",
                        '\t' => @"\t",
                        '\b' => @"\b",
                        '\f' => @"\f",
                        _ => $@"\u{(int)c:X4}",
                    };
                });

                return $"\"{escaped}\"";
            }
        }

        public static JsonSerializerOptions JsonOption { get; } = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        };
    }
}
