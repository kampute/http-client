---
title: Home
summary: Build REST API clients on top of HttpClient with scoped request configuration, content deserialization, retry strategies, structured error handling, and request/response interception.
---

# Welcome to Kampute.HttpClient

`Kampute.HttpClient` is a lightweight .NET library for building REST API clients on top of the native [`HttpClient`](https://learn.microsoft.com/dotnet/api/system.net.http.httpclient). It keeps the familiar .NET HTTP stack while adding the pieces most REST integrations need around it: reusable clients, request scopes, typed response deserialization, retry strategies, structured error handling, and request/response hooks.

Use it when you want a small client layer instead of a generated API SDK, or when you need direct control over [`HttpClient`](https://learn.microsoft.com/dotnet/api/system.net.http.httpclient) while still avoiding repeated boilerplate in every request.

## Core Capabilities

[`HttpRestClient`](api/Kampute.HttpClient.HttpRestClient.html) wraps [`HttpClient`](https://learn.microsoft.com/dotnet/api/system.net.http.httpclient) and focuses on common REST workflows:

- Send common HTTP methods through concise async helpers.
- Deserialize successful responses into typed .NET objects.
- Read raw response bodies as strings, streams, or byte arrays when needed.
- Register JSON, XML, or custom content formatters that read responses and write request payloads.
- Apply headers and request properties globally or inside temporary scopes.
- Configure retry behavior for transient connection failures.
- Handle HTTP error responses with reusable handlers.
- Inspect outgoing requests and incoming responses through lifecycle events.

The library does not hide [`HttpClient`](https://learn.microsoft.com/dotnet/api/system.net.http.httpclient). You can provide your own instance, configure handlers and timeouts yourself, or let [`HttpRestClient`](api/Kampute.HttpClient.HttpRestClient.html) use a shared client instance.

## Quick Start

Install the base package and one serializer package for the content type you want to consume. For most APIs, start with the `System.Text.Json` package.

```shell
dotnet add package Kampute.HttpClient.Json
```

Create an [`HttpRestClient`](api/Kampute.HttpClient.HttpRestClient.html), configure accepted response formats, and send requests asynchronously.

```csharp
using Kampute.HttpClient;
using Kampute.HttpClient.Json;

using var client = new HttpRestClient();

client.UseJson();

var data = await client.GetAsync<MyModel>("https://api.example.com/resource");
```

[`UseJson()`](api/Kampute.HttpClient.Json.HttpRestClientJsonExtensions.html#Kampute_HttpClient_Json_HttpRestClientJsonExtensions_UseJson_Kampute_HttpClient_HttpRestClient_System_Text_Json_JsonSerializerOptions_) registers the JSON formatter, which reads JSON responses and writes JSON payloads with the same options, and lets the client advertise JSON through the `Accept` header when the request does not already provide one.

## Choosing Packages

The base package contains [`HttpRestClient`](api/Kampute.HttpClient.HttpRestClient.html), request helpers, scopes, retry strategies, error handlers, compression content wrappers, the content formatter registry, and XML support. JSON packages are separate so applications only reference the JSON library they use.

| Package                                                                           | Use it for                                                                                 |
| --------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------ |
| [`Kampute.HttpClient`](api/Kampute.HttpClient.html)                               | Core HTTP client, request helpers, scopes, retry behavior, error handling, and XML APIs.   |
| [`Kampute.HttpClient.Json`](api/Kampute.HttpClient.Json.html)                     | JSON APIs using `System.Text.Json`.                                                        |
| [`Kampute.HttpClient.NewtonsoftJson`](api/Kampute.HttpClient.NewtonsoftJson.html) | JSON APIs that require `Newtonsoft.Json` features or compatibility.                        |

You can combine formats when an API can return more than one content type.

```csharp
using Kampute.HttpClient;
using Kampute.HttpClient.NewtonsoftJson;
using Kampute.HttpClient.Xml;

using var client = new HttpRestClient();

client.UseNewtonsoftJson();
client.UseXml();

var result = await client.GetAsync<MyResource>("https://api.example.com/resource");
```

## Working With HttpClient

By default, [`HttpRestClient`](api/Kampute.HttpClient.HttpRestClient.html) acquires a shared [`HttpClient`](https://learn.microsoft.com/dotnet/api/system.net.http.httpclient) instance. This avoids creating a new connection pool for every short-lived client wrapper.

```csharp
using Kampute.HttpClient;

using var client = new HttpRestClient();
```

If your application already manages [`HttpClient`](https://learn.microsoft.com/dotnet/api/system.net.http.httpclient) instances, pass one in directly. This is useful when you configure handlers, proxies, default timeouts, or dependency-injection lifetimes elsewhere.

```csharp
using Kampute.HttpClient;

var httpClient = new HttpClient
{
    Timeout = TimeSpan.FromSeconds(30)
};

using var client = new HttpRestClient(httpClient);
```

Set [`BaseAddress`](api/Kampute.HttpClient.HttpRestClient.html) when most requests target the same API. The client normalizes missing trailing slashes so relative paths resolve predictably.

```csharp
using var client = new HttpRestClient
{
    BaseAddress = new Uri("https://api.example.com/v1")
};

var account = await client.GetAsync<Account>("accounts/current");
```

## Sending Requests

The core package includes helpers for common request shapes:

- [`GetAsync<T>()`](api/Kampute.HttpClient.HttpRestClientExtensions.html#Kampute_HttpClient_HttpRestClientExtensions_GetAsync__1_Kampute_HttpClient_HttpRestClient_System_String_System_Threading_CancellationToken_), [`PostAsync<T>()`](api/Kampute.HttpClient.HttpRestClientExtensions.html#Kampute_HttpClient_HttpRestClientExtensions_PostAsync__1_Kampute_HttpClient_HttpRestClient_System_String_System_Net_Http_HttpContent_System_Threading_CancellationToken_), [`PutAsync<T>()`](api/Kampute.HttpClient.HttpRestClientExtensions.html#Kampute_HttpClient_HttpRestClientExtensions_PutAsync__1_Kampute_HttpClient_HttpRestClient_System_String_System_Net_Http_HttpContent_System_Threading_CancellationToken_), [`PatchAsync<T>()`](api/Kampute.HttpClient.HttpRestClientExtensions.html#Kampute_HttpClient_HttpRestClientExtensions_PatchAsync__1_Kampute_HttpClient_HttpRestClient_System_String_System_Net_Http_HttpContent_System_Threading_CancellationToken_), and [`DeleteAsync<T>()`](api/Kampute.HttpClient.HttpRestClientExtensions.html#Kampute_HttpClient_HttpRestClientExtensions_DeleteAsync__1_Kampute_HttpClient_HttpRestClient_System_String_System_Threading_CancellationToken_) for typed responses.
- [`GetAsStringAsync()`](api/Kampute.HttpClient.HttpRestClientExtensions.html#Kampute_HttpClient_HttpRestClientExtensions_GetAsStringAsync_Kampute_HttpClient_HttpRestClient_System_String_System_Threading_CancellationToken_), [`GetAsByteArrayAsync()`](api/Kampute.HttpClient.HttpRestClientExtensions.html#Kampute_HttpClient_HttpRestClientExtensions_GetAsByteArrayAsync_Kampute_HttpClient_HttpRestClient_System_String_System_Threading_CancellationToken_), and [`GetAsStreamAsync()`](api/Kampute.HttpClient.HttpRestClientExtensions.html#Kampute_HttpClient_HttpRestClientExtensions_GetAsStreamAsync_Kampute_HttpClient_HttpRestClient_System_String_System_Threading_CancellationToken_) for raw response bodies.
- [`HeadAsync()`](api/Kampute.HttpClient.HttpRestClientExtensions.html#Kampute_HttpClient_HttpRestClientExtensions_HeadAsync_Kampute_HttpClient_HttpRestClient_System_String_System_Threading_CancellationToken_) and [`OptionsAsync()`](api/Kampute.HttpClient.HttpRestClientExtensions.html#Kampute_HttpClient_HttpRestClientExtensions_OptionsAsync_Kampute_HttpClient_HttpRestClient_System_String_System_Threading_CancellationToken_) for response headers.
- [`SendAsync()`](api/Kampute.HttpClient.HttpRestClient.html) for lower-level control over the HTTP method and payload.

Use content-specific packages for convenient request payload helpers such as [`PostAsJsonAsync()`](api/Kampute.HttpClient.Json.HttpRestClientJsonExtensions.html#Kampute_HttpClient_Json_HttpRestClientJsonExtensions_PostAsJsonAsync_Kampute_HttpClient_HttpRestClient_System_String_System_Object_System_Threading_CancellationToken_), [`PatchAsJsonAsync()`](api/Kampute.HttpClient.Json.HttpRestClientJsonExtensions.html#Kampute_HttpClient_Json_HttpRestClientJsonExtensions_PatchAsJsonAsync_Kampute_HttpClient_HttpRestClient_System_String_System_Object_System_Threading_CancellationToken_), and [`PostAsXmlAsync()`](api/Kampute.HttpClient.Xml.HttpRestClientXmlExtensions.html#Kampute_HttpClient_Xml_HttpRestClientXmlExtensions_PostAsXmlAsync_Kampute_HttpClient_HttpRestClient_System_String_System_Object_System_Threading_CancellationToken_).

```csharp
using Kampute.HttpClient;
using Kampute.HttpClient.Json;

using var client = new HttpRestClient();

client.UseJson();

var created = await client.PostAsJsonAsync<MyResource>(
    "https://api.example.com/resources",
    new { name = "New resource" });
```

## Scoped Requests

Request scopes let you apply headers or properties to a group of operations without changing the client defaults. This is useful when a few endpoints need a different `Accept` header, tenant identifier, correlation value, or authentication state.

```csharp
using Kampute.HttpClient;

using var client = new HttpRestClient();

var csv = await client
    .WithScope()
    .SetHeader("Accept", MediaTypeNames.Text.Csv)
    .PerformAsync(scopedClient => scopedClient.GetAsStringAsync("https://api.example.com/report"));
```

You can also use explicit scopes when the same temporary configuration should apply to multiple requests.

```csharp
using Kampute.HttpClient;

using var client = new HttpRestClient();

using (client.BeginHeaderScope(new Dictionary<string, string?>
{
    ["X-Tenant"] = "northwind"
}))
{
    var customer = await client.GetAsync<Customer>("https://api.example.com/customers/42");
    var orders = await client.GetAsync<Order[]>("https://api.example.com/customers/42/orders");
}
```

When the scope is disposed, the temporary headers and properties are removed.

## Content Formats

The base package registers no content formatter. Each format registers its formatter in [`ContentFormatters`](api/Kampute.HttpClient.HttpRestClient.html) and exposes payload helpers for its content type.

- [`Kampute.HttpClient.Xml`](api/Kampute.HttpClient.Xml.html), in the base package: XML support through `XmlSerializer` and `DataContractSerializer`.
- [`Kampute.HttpClient.Json`](api/Kampute.HttpClient.Json.html): JSON support through `System.Text.Json`.
- [`Kampute.HttpClient.NewtonsoftJson`](api/Kampute.HttpClient.NewtonsoftJson.html): JSON support through `Newtonsoft.Json`.

[`UseXml()`](api/Kampute.HttpClient.Xml.HttpRestClientXmlExtensions.html) registers an [`XmlFormatter`](api/Kampute.HttpClient.Xml.XmlFormatter.html). Its `Serializer` setting chooses the serializer. With the default, `XmlSerializerKind.Auto`, types marked with `[DataContract]` or `[CollectionDataContract]` use `DataContractSerializer`, and all other types use `XmlSerializer`. The rule applies to responses by the requested type and to payloads by their runtime type. Set `XmlSerializerKind.XmlSerializer` or `XmlSerializerKind.DataContractSerializer` to use one serializer for every type, and `DataContractSettings` to configure `DataContractSerializer`.

```csharp
using Kampute.HttpClient;
using Kampute.HttpClient.Xml;

using var client = new HttpRestClient();

client.UseXml(xml => xml.Serializer = XmlSerializerKind.DataContractSerializer);

await client.PostAsXmlAsync("https://api.example.com/resources", resource);
```

You can also implement a content formatter for an application-specific content type. Derive from [`HttpContentFormatter`](api/Kampute.HttpClient.Content.Abstracts.HttpContentFormatter.html) and pass the media types it reads and the media types it writes to the base constructor. Override `ReadContentAsync` to read responses, `CreateContent` to write request payloads, or both. A formatter that only reads passes an empty list of writable media types, and one that only writes passes an empty list of readable media types.

```csharp
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

Register the formatter with the client. Responses with its media type are then read into the requested .NET type, the media type is added to the `Accept` header, and [`SendObjectAsync`](api/Kampute.HttpClient.HttpRestClientExtensions.html) writes request payloads with it.

```csharp
using Kampute.HttpClient;

using var client = new HttpRestClient();

client.ContentFormatters.Add(new VendorFormatter());

var created = await client.SendObjectAsync<Resource>(
    HttpMethod.Post,
    "https://api.example.com/resources",
    resource,
    "application/vnd.example.resource+json");
```

`SendObjectAsync` throws `InvalidOperationException` before sending anything if no registered formatter can write the payload in the requested media type. A payload that is already an `HttpContent` is sent as it is.

## Retry Behavior

Retry policies help clients recover from transient connection failures without duplicating retry loops around every request. Set [`RetryPolicy`](api/Kampute.HttpClient.HttpRestClient.html) to choose whether and how long the client waits between attempts. The default, [`HttpRetryPolicy.None`](api/Kampute.HttpClient.HttpRetryPolicy.html), does not retry.

```csharp
using Kampute.HttpClient;
using Kampute.Retry;

using var client = new HttpRestClient();

client.RetryPolicy = RetryStrategies.Fibonacci(TimeSpan.FromSeconds(1))
    .WithMaxAttempts(5)
    .ToHttpRetryPolicy();
```

A policy is built from a retry strategy of the [`Kampute.Retry`](api/Kampute.Retry.html) package, which the client depends on. [`RetryStrategies`](api/Kampute.Retry.RetryStrategies.html) creates the built-in strategies:

- `None` for no retry.
- `Once()` for a single retry after a delay.
- `Uniform()` for a fixed delay.
- `Linear()` for linearly increasing delays.
- `Fibonacci()` for delays that grow with the Fibonacci sequence.
- `Exponential()` for exponential backoff.

Except for `None` and `Once()`, they retry without limit. Chain `WithMaxAttempts()`, `WithTimeout()` and `WithJitter()` in any combination to limit them and to spread their delays. To choose the strategy from the failure, use [`HttpRetryPolicy.Dynamic()`](api/Kampute.HttpClient.HttpRetryPolicy.html).

The same strategies retry any operation, not only HTTP requests:

```csharp
using Kampute.Retry;

await RetryStrategies.Exponential(TimeSpan.FromSeconds(1))
    .WithMaxAttempts(5)
    .ExecuteAsync(ct => CopyFileAsync(source, target, ct),
        retryOn: ex => ex is IOException, cancellationToken);
```

## HTTP Error Handling

When a response status code indicates failure, the client raises an [`HttpResponseException`](api/Kampute.HttpClient.HttpResponseException.html) unless an error handler recovers from the response. Use [`ResponseErrorType`](api/Kampute.HttpClient.HttpRestClient.html) when the server returns structured error bodies, and register handlers when a status code needs custom recovery behavior.

```csharp
using Kampute.HttpClient;
using Kampute.HttpClient.ErrorHandlers;

using var unauthorizedErrorHandler = new HttpError401Handler(async (ctx, cancellationToken) =>
{
    var auth = await ctx.Client.PostAsFormAsync<AuthToken>("https://api.example.com/auth",
    [
        KeyValuePair.Create("client_id", MY_APP_ID),
        KeyValuePair.Create("client_secret", MY_APP_SECRET)
    ], cancellationToken);

    return new AuthenticationHeaderValue(AuthSchemes.Bearer, auth.Token);
});

using var client = new HttpRestClient();

client.ErrorHandlers.Add(unauthorizedErrorHandler);
```

The core package includes handlers for common retry and authentication scenarios, including [`HttpError401Handler`](api/Kampute.HttpClient.ErrorHandlers.HttpError401Handler.html), [`HttpError429Handler`](api/Kampute.HttpClient.ErrorHandlers.HttpError429Handler.html), [`HttpError503Handler`](api/Kampute.HttpClient.ErrorHandlers.HttpError503Handler.html), and [`TransientHttpErrorHandler`](api/Kampute.HttpClient.ErrorHandlers.TransientHttpErrorHandler.html).

## Request And Response Events

Subscribe to [`BeforeSendingRequest`](api/Kampute.HttpClient.HttpRestClient.html) and [`AfterReceivingResponse`](api/Kampute.HttpClient.HttpRestClient.html) when you need logging, diagnostics, request enrichment, or response inspection around every operation.

```csharp
using Kampute.HttpClient;

using var client = new HttpRestClient();

client.BeforeSendingRequest += (_, args) =>
{
    args.Request.Headers.TryAddWithoutValidation("X-Correlation-Id", Guid.NewGuid().ToString("N"));
};

client.AfterReceivingResponse += (_, args) =>
{
    Console.WriteLine($"{(int)args.Response.StatusCode} {args.Response.ReasonPhrase}");
};
```

Event handlers run around the actual HTTP operation, so keep them small and predictable.

## Common Integration Shape

A typical API wrapper keeps one configured [`HttpRestClient`](api/Kampute.HttpClient.HttpRestClient.html) and exposes domain-specific methods around it.

```csharp
using Kampute.HttpClient;
using Kampute.HttpClient.Json;

public sealed class AccountApiClient : IDisposable
{
    private readonly HttpRestClient _client;

    public AccountApiClient(Uri baseAddress, string bearerToken)
    {
        _client = new HttpRestClient
        {
            BaseAddress = baseAddress
        };

        _client.UseJson();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
    }

    public Task<Account?> GetCurrentAccountAsync(CancellationToken cancellationToken = default)
    {
        return _client.GetAsync<Account>("accounts/current", cancellationToken);
    }

    public Task<Account?> RenameAccountAsync(string name, CancellationToken cancellationToken = default)
    {
        return _client.PatchAsJsonAsync<Account>("accounts/current", new { name }, cancellationToken);
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}
```

This keeps the rest of the application focused on business operations instead of repeated HTTP setup.

