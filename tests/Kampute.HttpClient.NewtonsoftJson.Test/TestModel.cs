namespace Kampute.HttpClient.NewtonsoftJson.Test
{
    using Newtonsoft.Json;
    using System;
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
            return $"{{{ToJson(nameof(Name))}:{ToJson(Name)}}}";

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

        public static JsonSerializerSettings JsonSettings { get; } = new()
        {
            NullValueHandling = NullValueHandling.Include,
            MissingMemberHandling = MissingMemberHandling.Error,
        };
    }
}
