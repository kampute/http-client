# Kampute.HttpClient

A .NET library for REST API clients built on `HttpClient`, with scoped request configuration, typed responses, configurable retries, and error handlers.

[Documentation](https://kampute.github.io/http-client/) · [Getting started](https://kampute.github.io/http-client/overview/getting-started.html) · [API reference](https://kampute.github.io/http-client/api/)

## Features

- Shared connections or an application-managed `HttpClient`.
- Temporary headers and request properties through scopes.
- JSON, XML, and custom content formatters for requests and responses.
- Retry strategies for transient connection failures and handlers for HTTP errors.
- Request and response events for customization and inspection.

## Packages

| Package | Purpose |
| --- | --- |
| [Kampute.HttpClient](https://www.nuget.org/packages/Kampute.HttpClient) | Core client, scopes, error handlers, and XML support. |
| [Kampute.HttpClient.Json](https://www.nuget.org/packages/Kampute.HttpClient.Json) | JSON with System.Text.Json. |
| [Kampute.HttpClient.NewtonsoftJson](https://www.nuget.org/packages/Kampute.HttpClient.NewtonsoftJson) | JSON with Newtonsoft.Json. |
| [Kampute.Retry](https://www.nuget.org/packages/Kampute.Retry) | Retry strategies for HTTP and other operations; included with the core client. |

## Quick Start

For a JSON API, install the System.Text.Json extension. It includes the core client as a dependency.

```shell
dotnet add package Kampute.HttpClient.Json
```

In a .NET application, register the formatter before requesting a typed response. Replace the example URL with your API; this model assumes a JSON response such as `{"Id":42,"Name":"Example"}`.

```csharp
using Kampute.HttpClient;
using Kampute.HttpClient.Json;

using var client = new HttpRestClient();
client.UseJson();

var resource = await client.GetAsync<Resource>("https://api.example.com/resource");

public sealed class Resource
{
    public int Id { get; set; }
    public string? Name { get; set; }
}
```

See [Getting started](https://kampute.github.io/http-client/overview/getting-started.html) for package selection and [the user guide](https://kampute.github.io/http-client/overview/index.html) for request configuration, content formats, retries, and error handling.

## Contributing

Follow the existing code and documentation conventions. From the repository root:

```shell
dotnet build -c Release
dotnet test --verbosity minimal
kampose build
```

Documentation sources are in [docs](docs/); [kampose.json](kampose.json) controls site generation. Submit changes through a pull request.

## License

[MIT License](LICENSE).
