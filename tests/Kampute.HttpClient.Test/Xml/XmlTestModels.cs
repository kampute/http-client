namespace Kampute.HttpClient.Test.Xml
{
    using System;
    using System.Net;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Xml.Serialization;

    public class PlainModel
    {
        public string? Name { get; set; }

        public override bool Equals(object? obj) => obj is PlainModel other && Name == other.Name;

        public override int GetHashCode() => HashCode.Combine(Name);

        public string ToXmlSerializerString(Encoding encoding) => XmlStrings.XmlSerializerFormat(encoding, nameof(PlainModel), Name);

        public string ToDataContractString(Encoding encoding) => XmlStrings.DataContractFormat(encoding, nameof(PlainModel), Name);
    }

    [DataContract]
    public class ContractModel
    {
        [DataMember]
        public string? Name { get; set; }

        public override bool Equals(object? obj) => obj is ContractModel other && Name == other.Name;

        public override int GetHashCode() => HashCode.Combine(Name);

        public string ToXmlSerializerString(Encoding encoding) => XmlStrings.XmlSerializerFormat(encoding, nameof(ContractModel), Name);

        public string ToDataContractString(Encoding encoding) => XmlStrings.DataContractFormat(encoding, nameof(ContractModel), Name);
    }

    [DataContract]
    [XmlRoot(nameof(DualModel))]
    public class DualModel
    {
        [DataMember]
        [XmlElement]
        public string? Name { get; set; }

        public override bool Equals(object? obj) => obj is DualModel other && Name == other.Name;

        public override int GetHashCode() => HashCode.Combine(Name);

        public string ToDataContractString(Encoding encoding) => XmlStrings.DataContractFormat(encoding, nameof(DualModel), Name);
    }

    public static class XmlStrings
    {
        public const string DataContractNamespacePrefix = "http://schemas.datacontract.org/2004/07/";

        public static string XmlSerializerFormat(Encoding encoding, string rootName, string? name)
        {
            return $"<?xml version=\"1.0\" encoding=\"{encoding.WebName}\"?>"
                 + $"<{rootName} xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">"
                 + $"<Name>{WebUtility.HtmlEncode(name)}</Name>"
                 + $"</{rootName}>";
        }

        public static string DataContractFormat(Encoding encoding, string rootName, string? name)
        {
            return $"<?xml version=\"1.0\" encoding=\"{encoding.WebName}\"?>"
                 + $"<{rootName} xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns=\"{DataContractNamespacePrefix}{typeof(XmlStrings).Namespace}\">"
                 + $"<Name>{WebUtility.HtmlEncode(name)}</Name>"
                 + $"</{rootName}>";
        }
    }
}
