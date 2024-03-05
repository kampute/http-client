namespace Kampute.HttpClient.DataContract.Test
{
    using System;
    using System.Net;
    using System.Runtime.Serialization;
    using System.Text;

    [DataContract]
    public class TestModel
    {
        [DataMember]
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
                 + "<TestModel xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns=\"http://schemas.datacontract.org/2004/07/Kampute.HttpClient.DataContract.Test\">"
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
