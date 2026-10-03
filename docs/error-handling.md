---
title: Error Handling
summary: Handle structured error bodies and recover from selected HTTP status codes.
---

# Error Handling

When an HTTP response indicates failure and no handler retries it, the client raises an [`HttpResponseException`](~/api/Kampute.HttpClient.HttpResponseException.html). Configure structured error parsing or register handlers when your API needs recovery behavior.

## Structured Error Bodies

Set [`ResponseErrorType`](~/api/Kampute.HttpClient.HttpRestClient.html#Kampute_HttpClient_HttpRestClient_ResponseErrorType) to your API's error model. Register a [content formatter](content-formats.md) that can read that model from the response media type.

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

Add handlers to [`client.ErrorHandlers`](~/api/Kampute.HttpClient.HttpRestClient.html#Kampute_HttpClient_HttpRestClient_ErrorHandlers). Consult each handler's reference for its default policy and handling of `Retry-After`; see [Retries](retries.md) for strategy selection and separate retry budgets.

## Content Failures

Typed response reads fail with [`HttpContentException`](~/api/Kampute.HttpClient.HttpContentException.html) when the body is empty, its media type cannot be read by a registered formatter, or parsing fails. Check the response's `Content-Type`, register the matching formatter, and ensure your model matches the payload.

A missing writer for [`SendObjectAsync()`](~/api/Kampute.HttpClient.HttpRestClientExtensions.html#Kampute_HttpClient_HttpRestClientExtensions_SendObjectAsync_Kampute_HttpClient_HttpRestClient_System_Net_Http_HttpMethod_System_String_System_Object_System_String_System_Threading_CancellationToken_) raises [`InvalidOperationException`](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception) when the method is called, before sending the request. Register a formatter that can write the payload type in the requested media type.
