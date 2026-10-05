---
title: Home
summary: A .NET client layer for REST integrations, with typed content, scoped requests, and configurable recovery.
---

# Welcome to Kampute.HttpClient

REST integrations need more than an HTTP call: requests carry authentication and context, responses need to become application models, and failures need a recovery policy. [`Kampute.HttpClient`](~/api/Kampute.HttpClient.html) brings these concerns together around the native .NET [`HttpClient`](https://learn.microsoft.com/dotnet/api/system.net.http.httpclient).

The library is useful when you want to write an API client around your application's own models and operations. You choose the endpoints, payloads, and recovery rules; [`HttpRestClient`](~/api/Kampute.HttpClient.HttpRestClient.html) provides the request helpers and configuration behind them.

## A Familiar HTTP Foundation

[`HttpRestClient`](~/api/Kampute.HttpClient.HttpRestClient.html) wraps [`HttpClient`](https://learn.microsoft.com/dotnet/api/system.net.http.httpclient), so your integration can use the .NET HTTP stack's message handlers, proxies, and timeouts. You can supply a client managed by your application or use the library's shared client, which reuses connections across wrappers.

This lets an API wrapper keep its own base address, headers, content formatters, and error handlers while sharing the underlying connection infrastructure. Application code can expose operations such as loading an account or updating a resource without repeating HTTP setup in each method.

## Application Models and Content Formats

Use helpers such as [`GetAsync<T>()`](~/api/Kampute.HttpClient.HttpRestClientExtensions.html#Kampute_HttpClient_HttpRestClientExtensions_GetAsync__1_Kampute_HttpClient_HttpRestClient_System_String_System_Threading_CancellationToken_) and [`PostAsJsonAsync<T>()`](~/api/Kampute.HttpClient.Json.HttpRestClientJsonExtensions.html#Kampute_HttpClient_Json_HttpRestClientJsonExtensions_PostAsJsonAsync__1_Kampute_HttpClient_HttpRestClient_System_String_System_Object_System_Threading_CancellationToken_) to work with typed responses and request payloads. Registered content formatters read a response according to its `Content-Type` and the model you request. When you need the body directly, helpers also return strings, byte arrays, or streams.

XML support is included in the core package. JSON extensions let you choose [`System.Text.Json`](https://learn.microsoft.com/dotnet/api/system.text.json) or [`Newtonsoft.Json`](https://www.newtonsoft.com/json/help/html/N_Newtonsoft_Json.htm), and custom formatters can support application-specific media types. A client can register more than one format when an API returns different kinds of content.

Formatters are explicit: a new client starts with none registered. Configure the formats your integration needs before requesting typed responses.

## Request Configuration Where It Belongs

Set default headers for values shared by an API client's requests. Use a request scope when an operation needs a temporary tenant identifier, authorization header, or different accepted media type. Disposing the scope restores the prior configuration, so each operation does not need to undo its overrides manually.

Request properties carry context for message handlers and hooks. The [`BeforeSendingRequest`](~/api/Kampute.HttpClient.HttpRestClient.html#Kampute_HttpClient_HttpRestClient_BeforeSendingRequest) and [`AfterReceivingResponse`](~/api/Kampute.HttpClient.HttpRestClient.html#Kampute_HttpClient_HttpRestClient_AfterReceivingResponse) events let you enrich outgoing messages or inspect status and headers around the HTTP operation.

## Recovery That Matches Your API

Choose a retry policy for transient connection failures, with delays and limits supplied by [`Kampute.Resilience`](https://kampute.github.io/resilience/). Retries are opt-in: the default connection policy does not retry.

HTTP error responses have a separate recovery path. Register error handlers to refresh authorization after a 401 response or schedule retries for selected status codes. Structured error bodies can be deserialized into your API's error model; an unrecovered error response raises [`HttpResponseException`](~/api/Kampute.HttpClient.HttpResponseException.html).

The same retry strategies can also be used independently of HTTP, through the standalone [`Kampute.Resilience`](https://kampute.github.io/resilience/) package.

## Get Started

For a JSON API, install the JSON extension for the serializer your application uses. For XML or raw response bodies, start with the core package. The [getting-started guide](getting-started.md) walks through package selection, formatter registration, and a first typed request.

