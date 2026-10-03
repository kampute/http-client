---
title: Getting Started
summary: Choose a package, register a content formatter, and read a typed response.
---

# Getting Started

Use these examples in a .NET application with asynchronous calling code. The URLs are placeholders: replace them with endpoints you can access, and use models that match their response bodies.

## Choose a Package

The base package contains [`HttpRestClient`](~/api/Kampute.HttpClient.HttpRestClient.html), request helpers, scopes, retry policies, error handlers, compression content wrappers, the content formatter registry, and XML support. It depends on [`Kampute.Retry`](~/api/Kampute.Retry.html), which provides the retry strategies. JSON packages are separate so applications only reference the JSON library they use.

| Package                                                                           | Use it for                                                                                 |
| --------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------ |
| [`Kampute.HttpClient`](~/api/Kampute.HttpClient.html)                               | Core HTTP client, request helpers, scopes, retry behavior, error handling, and XML APIs.   |
| [`Kampute.HttpClient.Json`](~/api/Kampute.HttpClient.Json.html)                     | JSON APIs using [`System.Text.Json`](https://learn.microsoft.com/dotnet/api/system.text.json).                                                        |
| [`Kampute.HttpClient.NewtonsoftJson`](~/api/Kampute.HttpClient.NewtonsoftJson.html) | JSON APIs that require [`Newtonsoft.Json`](https://www.newtonsoft.com/json/help/html/N_Newtonsoft_Json.htm) features or compatibility.                        |
| [`Kampute.Retry`](~/api/Kampute.Retry.html)                                         | Retry strategies, also for operations other than HTTP requests. Installed with the core.   |

## Install

For a JSON API using [`System.Text.Json`](https://learn.microsoft.com/dotnet/api/system.text.json), install the extension package. It brings in the core client and [`Kampute.Retry`](~/api/Kampute.Retry.html) as dependencies.

```shell
dotnet add package Kampute.HttpClient.Json
```

For XML or raw response bodies, install [`Kampute.HttpClient`](~/api/Kampute.HttpClient.html) instead. For JSON using [`Newtonsoft.Json`](https://www.newtonsoft.com/json/help/html/N_Newtonsoft_Json.htm), install [`Kampute.HttpClient.NewtonsoftJson`](~/api/Kampute.HttpClient.NewtonsoftJson.html). See [Content formats](content-formats.md) for registration and serializer settings.

## Read a Typed Response

The core client starts with no content formatter. Register one before reading a response as a .NET object. This example expects `application/json` content such as `{"Id":42,"Name":"Example"}`.

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

[`UseJson()`](~/api/Kampute.HttpClient.Json.HttpRestClientJsonExtensions.html#Kampute_HttpClient_Json_HttpRestClientJsonExtensions_UseJson_Kampute_HttpClient_HttpRestClient_System_Text_Json_JsonSerializerOptions_) registers the formatter for reading responses and writing payloads. The client uses its readable media types to populate `Accept` when the request does not already set that header.

## Next Steps

- [Send requests and payloads](sending-requests.md).
- [Configure the client and its lifetime](client-configuration.md).
- [Customize individual requests](request-customization.md).
- [Configure content formats](content-formats.md), [retries](retries.md), and [error handling](error-handling.md).
