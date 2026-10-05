---
title: Sending Requests
summary: Send HTTP requests, write payloads, and read typed or raw responses.
---

# Sending Requests

Start with a client configured as shown in [Getting started](getting-started.md). Replace the example endpoints with your API.

## Choose a Request Helper

The [core request extensions](~/api/Kampute.HttpClient.HttpRestClientExtensions.html) cover common request shapes:

- [`GetAsync<T>()`](~/api/Kampute.HttpClient.HttpRestClientExtensions.html#Kampute_HttpClient_HttpRestClientExtensions_GetAsync__1_Kampute_HttpClient_HttpRestClient_System_String_System_Threading_CancellationToken_), [`PostAsync<T>()`](~/api/Kampute.HttpClient.HttpRestClientExtensions.html#Kampute_HttpClient_HttpRestClientExtensions_PostAsync__1_Kampute_HttpClient_HttpRestClient_System_String_System_Net_Http_HttpContent_System_Threading_CancellationToken_), [`PutAsync<T>()`](~/api/Kampute.HttpClient.HttpRestClientExtensions.html#Kampute_HttpClient_HttpRestClientExtensions_PutAsync__1_Kampute_HttpClient_HttpRestClient_System_String_System_Net_Http_HttpContent_System_Threading_CancellationToken_), [`PatchAsync<T>()`](~/api/Kampute.HttpClient.HttpRestClientExtensions.html#Kampute_HttpClient_HttpRestClientExtensions_PatchAsync__1_Kampute_HttpClient_HttpRestClient_System_String_System_Net_Http_HttpContent_System_Threading_CancellationToken_), and [`DeleteAsync<T>()`](~/api/Kampute.HttpClient.HttpRestClientExtensions.html#Kampute_HttpClient_HttpRestClientExtensions_DeleteAsync__1_Kampute_HttpClient_HttpRestClient_System_String_System_Threading_CancellationToken_) deserialize response bodies.
- [`GetAsStringAsync()`](~/api/Kampute.HttpClient.HttpRestClientExtensions.html#Kampute_HttpClient_HttpRestClientExtensions_GetAsStringAsync_Kampute_HttpClient_HttpRestClient_System_String_System_Threading_CancellationToken_), [`GetAsByteArrayAsync()`](~/api/Kampute.HttpClient.HttpRestClientExtensions.html#Kampute_HttpClient_HttpRestClientExtensions_GetAsByteArrayAsync_Kampute_HttpClient_HttpRestClient_System_String_System_Threading_CancellationToken_), and [`GetAsStreamAsync()`](~/api/Kampute.HttpClient.HttpRestClientExtensions.html#Kampute_HttpClient_HttpRestClientExtensions_GetAsStreamAsync_Kampute_HttpClient_HttpRestClient_System_String_System_Threading_CancellationToken_) read raw response bodies without a content formatter.
- [`HeadAsync()`](~/api/Kampute.HttpClient.HttpRestClientExtensions.html#Kampute_HttpClient_HttpRestClientExtensions_HeadAsync_Kampute_HttpClient_HttpRestClient_System_String_System_Threading_CancellationToken_) and [`OptionsAsync()`](~/api/Kampute.HttpClient.HttpRestClientExtensions.html#Kampute_HttpClient_HttpRestClientExtensions_OptionsAsync_Kampute_HttpClient_HttpRestClient_System_String_System_Threading_CancellationToken_) return response headers.
- [`SendAsync()`](~/api/Kampute.HttpClient.HttpRestClient.html#Kampute_HttpClient_HttpRestClient_SendAsync_System_Net_Http_HttpMethod_System_String_System_Net_Http_HttpContent_System_Net_Http_HttpCompletionOption_System_Threading_CancellationToken_) provides control over the HTTP method and payload.

Typed response methods need a registered formatter that can read the response's media type. See [Content formats](content-formats.md) when adding another format.

## Send a JSON Payload

The JSON extension packages supply [`PostAsJsonAsync()`](~/api/Kampute.HttpClient.Json.HttpRestClientJsonExtensions.html#Kampute_HttpClient_Json_HttpRestClientJsonExtensions_PostAsJsonAsync_Kampute_HttpClient_HttpRestClient_System_String_System_Object_System_Threading_CancellationToken_), [`PutAsJsonAsync()`](~/api/Kampute.HttpClient.Json.HttpRestClientJsonExtensions.html#Kampute_HttpClient_Json_HttpRestClientJsonExtensions_PutAsJsonAsync_Kampute_HttpClient_HttpRestClient_System_String_System_Object_System_Threading_CancellationToken_), and [`PatchAsJsonAsync()`](~/api/Kampute.HttpClient.Json.HttpRestClientJsonExtensions.html#Kampute_HttpClient_Json_HttpRestClientJsonExtensions_PatchAsJsonAsync_Kampute_HttpClient_HttpRestClient_System_String_System_Object_System_Threading_CancellationToken_). The following POST creates a resource on your API and expects a JSON response matching the `Resource` model from [Getting started](getting-started.md).

```csharp
using Kampute.HttpClient;
using Kampute.HttpClient.Json;

using var client = new HttpRestClient();
client.UseJson();

var created = await client.PostAsJsonAsync<Resource>(
    "https://api.example.com/resources",
    new { name = "New resource" });
```

For XML payloads, use [`PostAsXmlAsync()`](~/api/Kampute.HttpClient.Xml.HttpRestClientXmlExtensions.html#Kampute_HttpClient_Xml_HttpRestClientXmlExtensions_PostAsXmlAsync_Kampute_HttpClient_HttpRestClient_System_String_System_Object_System_Threading_CancellationToken_), [`PutAsXmlAsync()`](~/api/Kampute.HttpClient.Xml.HttpRestClientXmlExtensions.html#Kampute_HttpClient_Xml_HttpRestClientXmlExtensions_PutAsXmlAsync_Kampute_HttpClient_HttpRestClient_System_String_System_Object_System_Threading_CancellationToken_), or [`PatchAsXmlAsync()`](~/api/Kampute.HttpClient.Xml.HttpRestClientXmlExtensions.html#Kampute_HttpClient_Xml_HttpRestClientXmlExtensions_PatchAsXmlAsync_Kampute_HttpClient_HttpRestClient_System_String_System_Object_System_Threading_CancellationToken_) from [`Kampute.HttpClient.Xml`](~/api/Kampute.HttpClient.Xml.html). For application-specific media types, use [`SendObjectAsync()`](~/api/Kampute.HttpClient.HttpRestClientExtensions.html#Kampute_HttpClient_HttpRestClientExtensions_SendObjectAsync_Kampute_HttpClient_HttpRestClient_System_Net_Http_HttpMethod_System_String_System_Object_System_String_System_Threading_CancellationToken_) with a [custom content formatter](content-formats.md#custom-formatters).

## Read a Raw Response

```csharp
using Kampute.HttpClient;

using var client = new HttpRestClient();
var text = await client.GetAsStringAsync("https://api.example.com/resource");
```

Dispose the stream returned by [`GetAsStreamAsync()`](~/api/Kampute.HttpClient.HttpRestClientExtensions.html#Kampute_HttpClient_HttpRestClientExtensions_GetAsStreamAsync_Kampute_HttpClient_HttpRestClient_System_String_System_Threading_CancellationToken_) when you finish reading it. Avoid reading the response body in an [`AfterReceivingResponse`](~/api/Kampute.HttpClient.HttpRestClient.html#Kampute_HttpClient_HttpRestClient_AfterReceivingResponse) handler for streaming requests, since this consumes the stream before the caller can read it.

## Cancellation and Failures

Request helpers accept a [`CancellationToken`](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken); pass the caller's token through your API wrapper. See [Error handling](error-handling.md) for retrying transient connection failures and handling HTTP errors.

See the [core extensions](~/api/Kampute.HttpClient.HttpRestClientExtensions.html), [JSON extensions](~/api/Kampute.HttpClient.Json.HttpRestClientJsonExtensions.html), and [XML extensions](~/api/Kampute.HttpClient.Xml.HttpRestClientXmlExtensions.html) for signatures and overloads.
