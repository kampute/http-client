---
title: Error Handling
summary: Retry connection failures, handle structured error bodies, and recover from selected HTTP status codes.
---

# Error Handling

A request can fail in two ways: without a response, such as when the connection fails or times out, or with an error response. The client retries connection failures as its retry policy decides, and offers error responses to its error handlers. When an error response is not retried, the client raises an [`HttpResponseException`](~/api/Kampute.HttpClient.HttpResponseException.html).

## Retry Connection Failures

A request that fails without a response, because the connection is refused or reset, the host cannot be reached, or [`HttpClient.Timeout`](https://learn.microsoft.com/dotnet/api/system.net.http.httpclient.timeout) elapses, is retried only if you set [`RetryPolicy`](~/api/Kampute.HttpClient.HttpRestClient.html#Kampute_HttpClient_HttpRestClient_RetryPolicy). The default, [`HttpRetryPolicy.None`](~/api/Kampute.HttpClient.HttpRetryPolicy.html#Kampute_HttpClient_HttpRetryPolicy_None), does not retry.

```csharp
using System;
using Kampute.HttpClient;
using Kampute.Resilience;

using var client = new HttpRestClient();

client.RetryPolicy = RetryStrategies.Fibonacci(TimeSpan.FromSeconds(1))
    .WithMaxRetries(5)
    .ToHttpRetryPolicy();
```

The retry strategy comes from the [`Kampute.Resilience`](https://kampute.github.io/resilience/) package, which the core client installs as a dependency. `RetryStrategies` creates constant, linear, Fibonacci, and exponential delays, and modifiers such as `WithMaxRetries()`, `WithMaxElapsedTime()`, `WithMaxDelay()`, and `WithJitter()` limit the retries and shape their delays. A strategy without a limiting modifier retries without limit. See [Retry Strategies](https://kampute.github.io/resilience/overview.html#retry-strategies) in the Kampute.Resilience user guide for every strategy and modifier, and use [`HttpRetryPolicy.Dynamic()`](~/api/Kampute.HttpClient.HttpRetryPolicy.html#Kampute_HttpClient_HttpRetryPolicy_Dynamic_System_Func{Kampute_HttpClient_HttpRequestErrorContext_Kampute_Resilience_IRetryStrategy}_) to choose a strategy from the failure.

## Structured Error Bodies

Set [`ResponseErrorType`](~/api/Kampute.HttpClient.HttpRestClient.html#Kampute_HttpClient_HttpRestClient_ResponseErrorType) to your API's error model. Register a [content formatter](content-formats.md) that can read that model from the response media type. The JSON and XML formatters also read RFC 9457 problem details, `application/problem+json` and `application/problem+xml`, so an error model with the problem details fields works with them.

The deserialized model is available through [`HttpResponseException.ResponseObject`](~/api/Kampute.HttpClient.HttpResponseException.html#Kampute_HttpClient_HttpResponseException_ResponseObject). If the model implements [`IHttpErrorResponse`](~/api/Kampute.HttpClient.Interfaces.IHttpErrorResponse.html), its [`ToException()`](~/api/Kampute.HttpClient.Interfaces.IHttpErrorResponse.html#Kampute_HttpClient_Interfaces_IHttpErrorResponse_ToException_System_Net_HttpStatusCode_) method constructs the exception. See the reference for the full error-response contract.

## Refresh Authorization

[`HttpError401Handler`](~/api/Kampute.HttpClient.ErrorHandlers.HttpError401Handler.html) can obtain new authorization details and retry a rejected request. The following fragment assumes your application provides `RefreshAccessTokenAsync(CancellationToken)`, which contacts your authentication service and returns a bearer token.

```csharp
using System.Net.Http.Headers;
using Kampute.HttpClient;
using Kampute.HttpClient.ErrorHandlers;

using var unauthorizedErrorHandler = new HttpError401Handler(async (ctx, cancellationToken) =>
{
    var token = await RefreshAccessTokenAsync(cancellationToken);
    return new AuthenticationHeaderValue(AuthSchemes.Bearer, token);
});

using var client = new HttpRestClient();
client.ErrorHandlers.Add(unauthorizedErrorHandler);
```

Register a response formatter as well if you make typed requests with this client. Keep the handler alive while the client uses it.

## Other Recovery Handlers

The core package includes handlers for these cases:

| Handler | Purpose |
| --- | --- |
| [`HttpError401Handler`](~/api/Kampute.HttpClient.ErrorHandlers.HttpError401Handler.html) | Refresh authorization after 401 Unauthorized. |
| [`HttpError429Handler`](~/api/Kampute.HttpClient.ErrorHandlers.HttpError429Handler.html) | Schedule retries after 429 Too Many Requests. |
| [`HttpError503Handler`](~/api/Kampute.HttpClient.ErrorHandlers.HttpError503Handler.html) | Schedule retries after 503 Service Unavailable. |
| [`TransientHttpErrorHandler`](~/api/Kampute.HttpClient.ErrorHandlers.TransientHttpErrorHandler.html) | Retry selected transient HTTP error responses. |

Add handlers to [`client.ErrorHandlers`](~/api/Kampute.HttpClient.HttpRestClient.html#Kampute_HttpClient_HttpRestClient_ErrorHandlers). When several handlers accept a status code, they are asked in the order they were added, until one retries.

## Retry Error Responses

The 429, 503, and transient error handlers decide how to retry when they handle the first error response of a call:

- If the response suggests a retry time, the request is retried once, at that time. The 503 and transient handlers read the `Retry-After` header; the 429 handler also reads rate limit reset headers such as `x-ratelimit-reset`. If the retry receives another error response that the same handler handles, the error reaches the caller.
- If the response suggests no retry time, the 503 and transient handlers retry as the client's [`RetryPolicy`](#retry-connection-failures) decides, and the 429 handler does not retry.

A server cannot keep a call waiting: retry times suggested by later responses do not change these delays, and a suggested time more than [`MaxRetryDelay`](~/api/Kampute.HttpClient.ErrorHandlers.Abstracts.RetryableHttpErrorHandler.html#Kampute_HttpClient_ErrorHandlers_Abstracts_RetryableHttpErrorHandler_MaxRetryDelay) away, five minutes by default, ends the retries and the error reaches the caller.

To choose the retries yourself, set [`OnRetryPolicy`](~/api/Kampute.HttpClient.ErrorHandlers.Abstracts.RetryableHttpErrorHandler.html#Kampute_HttpClient_ErrorHandlers_Abstracts_RetryableHttpErrorHandler_OnRetryPolicy). It is called once per call, with the first error response and the retry time it suggests, and the policy it returns applies to that handler for the rest of the call; return `null` to keep the default. This handler retries a 503 response without `Retry-After` up to three times with growing delays, and accepts suggested retry times up to one minute away:

```csharp
using System;
using Kampute.HttpClient;
using Kampute.HttpClient.ErrorHandlers;
using Kampute.Resilience;

using var client = new HttpRestClient();

client.ErrorHandlers.Add(new HttpError503Handler
{
    MaxRetryDelay = TimeSpan.FromMinutes(1),
    OnRetryPolicy = (ctx, retryTime) => retryTime is null
        ? RetryStrategies.Exponential(TimeSpan.FromSeconds(1)).WithMaxRetries(3).ToHttpRetryPolicy()
        : null
});
```

Each handler counts only its own retries, separately from the connection retry policy and from other handlers.

## Content Failures

Typed response reads fail with [`HttpContentException`](~/api/Kampute.HttpClient.HttpContentException.html) when the body is empty, its media type cannot be read by a registered formatter, or parsing fails. Check the response's `Content-Type`, register the matching formatter, and ensure your model matches the payload.

A missing writer for [`SendObjectAsync()`](~/api/Kampute.HttpClient.HttpRestClientExtensions.html#Kampute_HttpClient_HttpRestClientExtensions_SendObjectAsync_Kampute_HttpClient_HttpRestClient_System_Net_Http_HttpMethod_System_String_System_Object_System_String_System_Threading_CancellationToken_) raises [`InvalidOperationException`](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception) when the method is called, before sending the request. Register a formatter that can write the payload type in the requested media type.
