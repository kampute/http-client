---
title: Client Configuration
summary: Manage HttpClient lifetime, configure a base address, and wrap an API.
---

# Client Configuration

See [Getting started](getting-started.md) for package installation. Replace the example URLs and models with your API's values.

## HttpClient Lifetime

By default, [`HttpRestClient`](~/api/Kampute.HttpClient.HttpRestClient.html) acquires a shared [`HttpClient`](https://learn.microsoft.com/dotnet/api/system.net.http.httpclient) instance. This avoids creating a new connection pool for every short-lived client wrapper.

```csharp
using Kampute.HttpClient;

using var client = new HttpRestClient();
```

If your application already manages [`HttpClient`](https://learn.microsoft.com/dotnet/api/system.net.http.httpclient) instances, pass one in directly. This is useful when you configure handlers, proxies, default timeouts, or dependency-injection lifetimes elsewhere.

```csharp
using System;
using System.Net.Http;
using Kampute.HttpClient;

using var httpClient = new HttpClient
{
    Timeout = TimeSpan.FromSeconds(30)
};

using var client = new HttpRestClient(httpClient, disposeClient: false);
```

When passing an application-managed [`HttpClient`](https://learn.microsoft.com/dotnet/api/system.net.http.httpclient), use [`disposeClient: false`](~/api/Kampute.HttpClient.HttpRestClient.html#Kampute_HttpClient_HttpRestClient__ctor_System_Net_Http_HttpClient_System_Boolean_) to keep ownership with the application. Otherwise, disposing [`HttpRestClient`](~/api/Kampute.HttpClient.HttpRestClient.html) also disposes the supplied client.

## Base Address

Set [`BaseAddress`](~/api/Kampute.HttpClient.HttpRestClient.html#Kampute_HttpClient_HttpRestClient_BaseAddress) when most requests target the same API, and use relative request paths without a leading slash: a leading slash resolves the path from the host root and drops the path of the base address. The client adds a trailing slash to the base address if it has none. Register a formatter before reading typed responses.

```csharp
using System;
using Kampute.HttpClient;
using Kampute.HttpClient.Json;

using var client = new HttpRestClient
{
    BaseAddress = new Uri("https://api.example.com/v1/")
};

client.UseJson();
var resource = await client.GetAsync<Resource>("resources/42");
```

## Build an API Wrapper

A typical API wrapper keeps one configured [`HttpRestClient`](~/api/Kampute.HttpClient.HttpRestClient.html) and exposes domain-specific methods around it.

The `Account` type below is your application's response model. Both methods use relative URLs and pass cancellation to the request helper. `RenameAccountAsync` sends a PATCH that updates the remote account.

```csharp
using System;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
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

For temporary request settings, see [Request customization](request-customization.md).
