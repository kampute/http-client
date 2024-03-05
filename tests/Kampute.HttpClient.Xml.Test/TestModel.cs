namespace Kampute.HttpClient.Xml.Test
{
    using System;
    using System.Net;
    using System.Text;

    [Serializable]
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

        public string ToXmlString(Encoding encoding)
        {
            return $"<?xml version=\"1.0\" encoding=\"{encoding.WebName}\"?>"
                 + "<TestModel xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">"
                 + Element(nameof(Name), Name)
                 + "</TestModel>";

            static string Element(string name, string? value)
            {
                return value is null
                    ? $"<{name} xsi:nil=\"true\"/>"
                    : $"<{name}>{WebUtility.HtmlEncode(value)}</{name}>";
            }
        }
    }
}
