# Kampute.HttpClient.NewtonsoftJson

JSON support for [Kampute.HttpClient](https://www.nuget.org/packages/Kampute.HttpClient), using `Newtonsoft.Json` to read responses and write request payloads.

[Content formats guide](https://kampute.github.io/http-client/overview/content-formats.html) · [API reference](https://kampute.github.io/http-client/api/Kampute.HttpClient.NewtonsoftJson.html)

## Installation

The package includes the core client as a dependency.

```shell
dotnet add package Kampute.HttpClient.NewtonsoftJson
```

## Usage

Register the JSON formatter before requesting a typed response. Replace the example URL with your API; this model assumes a response such as `{"Id":42,"Name":"Example"}`.

```csharp
using Kampute.HttpClient;
using Kampute.HttpClient.NewtonsoftJson;

using var client = new HttpRestClient();
client.UseNewtonsoftJson();

var resource = await client.GetAsync<Resource>("https://api.example.com/resource");

public sealed class Resource
{
    public int Id { get; set; }
    public string? Name { get; set; }
}
```

Use `PostAsJsonAsync`, `PutAsJsonAsync`, or `PatchAsJsonAsync` to send JSON payloads. See [Sending requests](https://kampute.github.io/http-client/overview/sending-requests.html) for examples and [Content formats](https://kampute.github.io/http-client/overview/content-formats.html) for serializer configuration.

## License

Kampute.HttpClient.NewtonsoftJson is released under the [MIT License](LICENSE).
