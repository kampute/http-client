# Kampute.HttpClient

A .NET REST client built on `HttpClient`, with shared connections, scoped headers and properties, configurable retries, error handlers, and request/response events.

[Documentation](https://kampute.github.io/http-client/) · [Getting started](https://kampute.github.io/http-client/overview/getting-started.html) · [API reference](https://kampute.github.io/http-client/api/Kampute.HttpClient.html)

## Installation

```shell
dotnet add package Kampute.HttpClient
```

## Usage

Read a response as text without a content formatter. Replace the example URL with your API.

```csharp
using Kampute.HttpClient;

using var client = new HttpRestClient();
var text = await client.GetAsStringAsync("https://api.example.com/resource");
```

The core package registers no content formatter. For typed responses, call `UseXml()` from `Kampute.HttpClient.Xml`, or install a JSON extension and register its formatter.

## Documentation

- [Content formats](https://kampute.github.io/http-client/overview/content-formats.html): XML, JSON packages, and custom formatters.
- [Request customization](https://kampute.github.io/http-client/overview/request-customization.html): temporary headers, properties, and events.
- [Retries](https://kampute.github.io/http-client/overview/retries.html) and [error handling](https://kampute.github.io/http-client/overview/error-handling.html): connection failures and HTTP error responses.

## License

[MIT License](LICENSE).
