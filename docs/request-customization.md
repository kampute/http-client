---
title: Request Customization
summary: Apply temporary request settings and intercept requests and responses.
---

# Request Customization

Configure a client as shown in [Getting started](getting-started.md), then apply settings at the level that needs them.

## Default Headers

Set [`client.DefaultRequestHeaders`](~/api/Kampute.HttpClient.HttpRestClient.html#Kampute_HttpClient_HttpRestClient_DefaultRequestHeaders) for headers shared by the client's requests. If you change that collection while requests are in flight, lock the collection as described in the [`HttpRestClient` reference](~/api/Kampute.HttpClient.HttpRestClient.html).

## Scoped Headers

Request scopes let you apply headers or properties to a group of operations without changing the client defaults. This is useful when a few endpoints need a different `Accept` header, tenant identifier, correlation value, or authentication state.

```csharp
using Kampute.HttpClient;

using var client = new HttpRestClient();

var csv = await client
    .WithScope()
    .SetHeader("Accept", MediaTypeNames.Text.Csv)
    .PerformAsync(scopedClient => scopedClient.GetAsStringAsync("https://api.example.com/report"));
```

You can also use explicit scopes when the same temporary configuration should apply to multiple requests. Here, `Customer` and `Order` are your application's JSON response models.

```csharp
using System.Collections.Generic;
using Kampute.HttpClient;
using Kampute.HttpClient.Json;

using var client = new HttpRestClient();
client.UseJson();

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

## Scoped Properties

Use [`BeginPropertyScope()`](~/api/Kampute.HttpClient.HttpRestClient.html#Kampute_HttpClient_HttpRestClient_BeginPropertyScope_System_Collections_Generic_IEnumerable{System_Collections_Generic_KeyValuePair{System_String_System_Object}}_) for temporary values attached to request messages, or [`WithScope()`](~/api/Kampute.HttpClient.HttpRestClientExtensions.html#Kampute_HttpClient_HttpRestClientExtensions_WithScope_Kampute_HttpClient_HttpRestClient_) followed by [`SetProperty()`](~/api/Kampute.HttpClient.HttpRequestScope.html#Kampute_HttpClient_HttpRequestScope_SetProperty_System_String_System_Object_) in the fluent API. Properties carry application context for message handlers and request hooks; headers carry values sent to the server.

## Request and Response Events

Subscribe to [`BeforeSendingRequest`](~/api/Kampute.HttpClient.HttpRestClient.html#Kampute_HttpClient_HttpRestClient_BeforeSendingRequest) and [`AfterReceivingResponse`](~/api/Kampute.HttpClient.HttpRestClient.html#Kampute_HttpClient_HttpRestClient_AfterReceivingResponse) when you need logging, diagnostics, request enrichment, or response inspection around every operation.

```csharp
using System;
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

[`AfterReceivingResponse`](~/api/Kampute.HttpClient.HttpRestClient.html#Kampute_HttpClient_HttpRestClient_AfterReceivingResponse) runs before further response processing. For streaming requests, inspect status and headers without reading the body: reading it consumes the stream intended for the caller.

See [`HttpRequestScope`](~/api/Kampute.HttpClient.HttpRequestScope.html) and [`HttpRestClient`](~/api/Kampute.HttpClient.HttpRestClient.html) for the complete scope and event contracts.
