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
- Register JSON, XML, or custom response deserializers.
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

client.AcceptJson();

var data = await client.GetAsync<MyModel>("https://api.example.com/resource");
```

[`AcceptJson()`](api/Kampute.HttpClient.Json.HttpRestClientJsonExtensions.html#Kampute_HttpClient_Json_HttpRestClientJsonExtensions_AcceptJson_Kampute_HttpClient_HttpRestClient_System_Text_Json_JsonSerializerOptions_) registers the JSON deserializer and lets the client advertise JSON through the `Accept` header when the request does not already provide one.

## Choosing Packages

The base package contains [`HttpRestClient`](api/Kampute.HttpClient.HttpRestClient.html), request helpers, scopes, retry strategies, error handlers, compression content wrappers, and the deserializer registry. Serializer packages are separate so applications only reference the serializers they use.

| Package                                                                           | Use it for                                                                     |
| --------------------------------------------------------------------------------- | ------------------------------------------------------------------------------ |
| [`Kampute.HttpClient`](api/Kampute.HttpClient.html)                               | Core HTTP client, request helpers, scopes, retry behavior, and error handling. |
| [`Kampute.HttpClient.Json`](api/Kampute.HttpClient.Json.html)                     | JSON APIs using `System.Text.Json`.                                            |
| [`Kampute.HttpClient.NewtonsoftJson`](api/Kampute.HttpClient.NewtonsoftJson.html) | JSON APIs that require `Newtonsoft.Json` features or compatibility.            |
| [`Kampute.HttpClient.Xml`](api/Kampute.HttpClient.Xml.html)                       | XML APIs using `XmlSerializer`.                                                |
| [`Kampute.HttpClient.DataContract`](api/Kampute.HttpClient.DataContract.html)     | XML APIs using `DataContractSerializer`.                                       |

You can combine serializer packages when an API can return more than one content type.

```csharp
using Kampute.HttpClient;
using Kampute.HttpClient.DataContract;
using Kampute.HttpClient.NewtonsoftJson;

using var client = new HttpRestClient();

client.AcceptJson();
client.AcceptXml();

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

client.AcceptJson();

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

## Serializer Packages

The base package does not include a default content deserializer. Each serializer package registers a deserializer with [`ResponseDeserializers`](api/Kampute.HttpClient.HttpRestClient.html) and exposes payload helpers for its content type.

- [`Kampute.HttpClient.Json`](api/Kampute.HttpClient.Json.html): JSON support through `System.Text.Json`.
- [`Kampute.HttpClient.NewtonsoftJson`](api/Kampute.HttpClient.NewtonsoftJson.html): JSON support through `Newtonsoft.Json`.
- [`Kampute.HttpClient.Xml`](api/Kampute.HttpClient.Xml.html): XML support through `XmlSerializer`.
- [`Kampute.HttpClient.DataContract`](api/Kampute.HttpClient.DataContract.html): XML support through `DataContractSerializer`.

You can also implement custom deserializers for application-specific content types.

```csharp
using Kampute.HttpClient.Content.Abstracts;

public sealed class VendorContentDeserializer
    : HttpContentDeserializer
{
    public VendorContentDeserializer()
        : base("application/vnd.example.resource+json")
    {
    }

    public override Task<object?> DeserializeAsync(
        HttpContent content,
        Type modelType,
        CancellationToken cancellationToken = default)
    {
        // Deserialize the vendor-specific payload here.
        throw new NotImplementedException();
    }
}
```

```csharp
using Kampute.HttpClient;

using var client = new HttpRestClient();

client.ResponseDeserializers.Add(new VendorContentDeserializer());
```

## Retry Behavior

Retry strategies help clients recover from transient connection failures without duplicating retry loops around every request. Set [`BackoffStrategy`](api/Kampute.HttpClient.HttpRestClient.html) to choose how long the client waits between attempts.

```csharp
using Kampute.HttpClient;

using var client = new HttpRestClient();

client.BackoffStrategy = BackoffStrategies.Fibonacci(
    maxAttempts: 5,
    initialDelay: TimeSpan.FromSeconds(1));
```

Built-in strategies include:

- [`BackoffStrategies.None`](api/Kampute.HttpClient.BackoffStrategies.html) for no retry delay.
- [`BackoffStrategies.Once()`](api/Kampute.HttpClient.BackoffStrategies.html) for a single retry after a delay.
- [`BackoffStrategies.Uniform()`](api/Kampute.HttpClient.BackoffStrategies.html) for a fixed delay.
- [`BackoffStrategies.Linear()`](api/Kampute.HttpClient.BackoffStrategies.html) for linearly increasing delays.
- [`BackoffStrategies.Exponential()`](api/Kampute.HttpClient.BackoffStrategies.html) for exponential backoff.
- [`BackoffStrategies.Fibonacci()`](api/Kampute.HttpClient.BackoffStrategies.html) for gradually increasing delays.

Retry strategies can be combined with limits and jitter where appropriate for the API you are calling.

## HTTP Error Handling

When a response status code indicates failure, the client raises an [`HttpResponseException`](api/Kampute.HttpClient.HttpResponseException.html) unless an error handler recovers from the response. Use [`ResponseErrorType`](api/Kampute.HttpClient.HttpRestClient.html) when the server returns structured error bodies, and register handlers when a status code needs custom recovery behavior.

```csharp
using Kampute.HttpClient;
using Kampute.HttpClient.ErrorHandlers;

using var unauthorizedErrorHandler = new HttpError401Handler(async (client, challenges, cancellationToken) =>
{
    var auth = await client.PostAsFormAsync<AuthToken>("https://api.example.com/auth",
    [
        KeyValuePair.Create("client_id", MY_APP_ID),
        KeyValuePair.Create("client_secret", MY_APP_SECRET)
    ]);

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

        _client.AcceptJson();
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

