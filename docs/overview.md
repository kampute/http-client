---
title: User Guide
summary: Understand how clients, request scopes, content formatters, and recovery policies work together.
---

# User Guide

An integration typically keeps a configured [`HttpRestClient`](~/api/Kampute.HttpClient.HttpRestClient.html) behind an API wrapper. The wrapper exposes application operations, while the client prepares HTTP messages, processes responses, and applies the recovery rules you select.

## Configuration and Lifetime

The underlying [`HttpClient`](https://learn.microsoft.com/dotnet/api/system.net.http.httpclient) owns the HTTP transport. [`HttpRestClient`](~/api/Kampute.HttpClient.HttpRestClient.html) adds the base address, default request headers, content formatters, retry policy, and error handlers used by the integration. Configure these before making requests.

When the application supplies an [`HttpClient`](https://learn.microsoft.com/dotnet/api/system.net.http.httpclient), decide who owns its lifetime: by default, disposing the wrapper disposes the supplied client. Pass [`disposeClient: false`](~/api/Kampute.HttpClient.HttpRestClient.html#Kampute_HttpClient_HttpRestClient__ctor_System_Net_Http_HttpClient_System_Boolean_) when the application manages that lifetime. The [client configuration guide](client-configuration.md) covers both supplied and shared clients.

A request scope supplies temporary headers or properties for its lifetime. Use it for operation-specific context, and dispose it to restore the previous settings. The [request customization guide](request-customization.md) explains explicit scopes, the fluent scope API, and request/response events.

## The Request Lifecycle

A request passes through these stages:

1. The client creates the HTTP message using its default and scoped configuration.
2. [`BeforeSendingRequest`](~/api/Kampute.HttpClient.HttpRestClient.html#Kampute_HttpClient_HttpRestClient_BeforeSendingRequest) gives subscribers an opportunity to modify the outgoing message.
3. The underlying [`HttpClient`](https://learn.microsoft.com/dotnet/api/system.net.http.httpclient) sends it. A configured connection retry policy can schedule another attempt after a transient connection failure.
4. [`AfterReceivingResponse`](~/api/Kampute.HttpClient.HttpRestClient.html#Kampute_HttpClient_HttpRestClient_AfterReceivingResponse) exposes a received response before further processing.
5. A successful response is read in the form requested by the caller. An error response is offered to registered error handlers; if none recovers by retrying, the call fails with [`HttpResponseException`](~/api/Kampute.HttpClient.HttpResponseException.html).

The [request helpers](sending-requests.md) let you choose typed objects, raw bodies, or lower-level HTTP control. Typed responses need a registered formatter that can read the response's media type into the requested model; [content formatters](content-formats.md) also write object payloads in a selected format.

## Failure and Recovery

Connection failures, HTTP error responses, and content failures need different treatment. A retry policy controls transient connection failures. Error handlers decide whether an HTTP error response can be retried, while formatter or model problems must be corrected to read the response successfully.

Set retry limits deliberately. The connection policy and retrying error handlers maintain separate budgets, so their limits do not form one total limit for a request. The [retry guide](retries.md) explains strategy composition and budgets; the [error handling guide](error-handling.md) covers structured errors and status-specific recovery.
