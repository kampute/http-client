---
title: Content Formats
summary: Register JSON, XML, or custom formatters to read responses and write payloads.
---

# Content Formats

The base package registers no content formatter. Each format registers its formatter in [`ContentFormatters`](~/api/Kampute.HttpClient.HttpRestClient.html#Kampute_HttpClient_HttpRestClient_ContentFormatters) and exposes payload helpers for its content type.

- [`Kampute.HttpClient.Xml`](~/api/Kampute.HttpClient.Xml.html), in the base package: XML support through [`XmlSerializer`](https://learn.microsoft.com/dotnet/api/system.xml.serialization.xmlserializer) and [`DataContractSerializer`](https://learn.microsoft.com/dotnet/api/system.runtime.serialization.datacontractserializer).
- [`Kampute.HttpClient.Json`](~/api/Kampute.HttpClient.Json.html): JSON support through [`System.Text.Json`](https://learn.microsoft.com/dotnet/api/system.text.json).
- [`Kampute.HttpClient.NewtonsoftJson`](~/api/Kampute.HttpClient.NewtonsoftJson.html): JSON support through [`Newtonsoft.Json`](https://www.newtonsoft.com/json/help/html/N_Newtonsoft_Json.htm).

## JSON

Both JSON formatters read `application/json`, `application/problem+json`, and any other media type with the `+json` suffix, such as `application/vnd.example+json`, and write `application/json`. Install either JSON package as described in [Getting started](getting-started.md). Pass serializer options when registering the formatter:

```csharp
using System.Text.Json;
using Kampute.HttpClient;
using Kampute.HttpClient.Json;

using var client = new HttpRestClient();
client.UseJson(new JsonSerializerOptions(JsonSerializerDefaults.Web));
```

For [`Newtonsoft.Json`](https://www.newtonsoft.com/json/help/html/N_Newtonsoft_Json.htm), import [`Kampute.HttpClient.NewtonsoftJson`](~/api/Kampute.HttpClient.NewtonsoftJson.html) and call [`UseNewtonsoftJson(settings)`](~/api/Kampute.HttpClient.NewtonsoftJson.HttpRestClientJsonExtensions.html#Kampute_HttpClient_NewtonsoftJson_HttpRestClientJsonExtensions_UseNewtonsoftJson_Kampute_HttpClient_HttpRestClient_Newtonsoft_Json_JsonSerializerSettings_), passing a [`JsonSerializerSettings`](https://www.newtonsoft.com/json/help/html/T_Newtonsoft_Json_JsonSerializerSettings.htm) instance. The registered options or settings apply to both reading responses and writing payloads.

## XML

[`UseXml()`](~/api/Kampute.HttpClient.Xml.HttpRestClientXmlExtensions.html#Kampute_HttpClient_Xml_HttpRestClientXmlExtensions_UseXml_Kampute_HttpClient_HttpRestClient_System_Action{Kampute_HttpClient_Xml_XmlFormatter}_) registers an [`XmlFormatter`](~/api/Kampute.HttpClient.Xml.XmlFormatter.html), which reads `application/xml`, `text/xml`, `application/problem+xml`, and any other media type with the `+xml` suffix, and writes `application/xml`. Its [`Serializer`](~/api/Kampute.HttpClient.Xml.XmlFormatter.html#Kampute_HttpClient_Xml_XmlFormatter_Serializer) setting chooses the serializer. With the default, [`XmlSerializerKind.Auto`](~/api/Kampute.HttpClient.Xml.XmlSerializerKind.html#fields), types marked with [`[DataContract]`](https://learn.microsoft.com/dotnet/api/system.runtime.serialization.datacontractattribute) or [`[CollectionDataContract]`](https://learn.microsoft.com/dotnet/api/system.runtime.serialization.collectiondatacontractattribute) use [`DataContractSerializer`](https://learn.microsoft.com/dotnet/api/system.runtime.serialization.datacontractserializer), and all other types use [`XmlSerializer`](https://learn.microsoft.com/dotnet/api/system.xml.serialization.xmlserializer). The rule applies to responses by the requested type and to payloads by their runtime type. Set [`XmlSerializerKind.XmlSerializer`](~/api/Kampute.HttpClient.Xml.XmlSerializerKind.html#fields) or [`XmlSerializerKind.DataContractSerializer`](~/api/Kampute.HttpClient.Xml.XmlSerializerKind.html#fields) to use one serializer for every type, and [`DataContractSettings`](~/api/Kampute.HttpClient.Xml.XmlFormatter.html#Kampute_HttpClient_Xml_XmlFormatter_DataContractSettings) to configure [`DataContractSerializer`](https://learn.microsoft.com/dotnet/api/system.runtime.serialization.datacontractserializer).

The following POST writes to your API; `Resource` is your application's serializable model.

```csharp
using Kampute.HttpClient;
using Kampute.HttpClient.Xml;

using var client = new HttpRestClient();

client.UseXml(xml => xml.Serializer = XmlSerializerKind.DataContractSerializer);

await client.PostAsXmlAsync("https://api.example.com/resources", new Resource { Name = "Example" });
```

## Custom Formatters

A response with a `+json` or `+xml` media type, such as `application/vnd.example.resource+json`, does not need its own formatter, because the JSON and XML formatters read it. Implement a custom formatter when you need to write such a media type, advertise it in the `Accept` header, or read it differently. The client reads a response with the first registered formatter that can read it, so add a custom formatter for a `+json` or `+xml` media type before the JSON or XML formatter.

You can also implement a content formatter for an application-specific content type. Derive from [`HttpContentFormatter`](~/api/Kampute.HttpClient.Content.Abstracts.HttpContentFormatter.html) and pass the media types it reads and the media types it writes to the base constructor. Override [`ReadContentAsync`](~/api/Kampute.HttpClient.Content.Abstracts.HttpContentFormatter.html#Kampute_HttpClient_Content_Abstracts_HttpContentFormatter_ReadContentAsync_System_Net_Http_HttpContent_System_Type_System_Threading_CancellationToken_) to read responses, [`CreateContent`](~/api/Kampute.HttpClient.Content.Abstracts.HttpContentFormatter.html#Kampute_HttpClient_Content_Abstracts_HttpContentFormatter_CreateContent_System_Object_System_String_) to write request payloads, or both. A formatter that only reads passes an empty list of writable media types, and one that only writes passes an empty list of readable media types. To read media types beyond the listed ones, override [`CanReadMediaType`](~/api/Kampute.HttpClient.Content.Abstracts.HttpContentFormatter.html#Kampute_HttpClient_Content_Abstracts_HttpContentFormatter_CanReadMediaType_System_String_); [`HasStructuredSyntaxSuffix`](~/api/Kampute.HttpClient.Content.Abstracts.HttpContentFormatter.html#Kampute_HttpClient_Content_Abstracts_HttpContentFormatter_HasStructuredSyntaxSuffix_System_String_System_String_) tells whether a media type ends with a suffix such as `+json`. Media types accepted this way are read but not advertised in the `Accept` header.

This skeleton shows the overrides to implement; replace both [`NotImplementedException`](https://learn.microsoft.com/dotnet/api/system.notimplementedexception) statements with your format's read and write logic before registering it.

```csharp
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Kampute.HttpClient.Content.Abstracts;

public sealed class VendorFormatter : HttpContentFormatter
{
    private const string VendorMediaType = "application/vnd.example.resource+json";

    public VendorFormatter()
        : base([VendorMediaType], [VendorMediaType])
    {
    }

    protected override Task<object?> ReadContentAsync(
        HttpContent content,
        Type modelType,
        CancellationToken cancellationToken)
    {
        // Read the vendor-specific payload here.
        throw new NotImplementedException();
    }

    protected override HttpContent CreateContent(object payload, string mediaType)
    {
        // Write the vendor-specific payload here.
        throw new NotImplementedException();
    }
}
```

Once you have implemented the formatter, register it with the client. The following POST uses your application's `Resource` model and `resource` payload. Responses with its media type are then read into the requested .NET type, the media type is added to the `Accept` header, and [`SendObjectAsync`](~/api/Kampute.HttpClient.HttpRestClientExtensions.html#Kampute_HttpClient_HttpRestClientExtensions_SendObjectAsync_Kampute_HttpClient_HttpRestClient_System_Net_Http_HttpMethod_System_String_System_Object_System_String_System_Threading_CancellationToken_) writes request payloads with it.

```csharp
using System.Net.Http;
using Kampute.HttpClient;

using var client = new HttpRestClient();

client.ContentFormatters.Add(new VendorFormatter());

var created = await client.SendObjectAsync<Resource>(
    HttpMethod.Post,
    "https://api.example.com/resources",
    resource,
    "application/vnd.example.resource+json");
```

[`SendObjectAsync`](~/api/Kampute.HttpClient.HttpRestClientExtensions.html#Kampute_HttpClient_HttpRestClientExtensions_SendObjectAsync_Kampute_HttpClient_HttpRestClient_System_Net_Http_HttpMethod_System_String_System_Object_System_String_System_Threading_CancellationToken_) throws [`InvalidOperationException`](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception) before sending anything if no registered formatter can write the payload in the requested media type. A payload that is already an [`HttpContent`](https://learn.microsoft.com/dotnet/api/system.net.http.httpcontent) is sent as it is.

## Combine Formats

You can combine formats when an API can return more than one content type. Here, `Resource` is the response model from [Getting started](getting-started.md).

```csharp
using Kampute.HttpClient;
using Kampute.HttpClient.NewtonsoftJson;
using Kampute.HttpClient.Xml;

using var client = new HttpRestClient();

client.UseNewtonsoftJson();
client.UseXml();

var result = await client.GetAsync<Resource>("https://api.example.com/resource");
```
