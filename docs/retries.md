---
title: Retries
summary: Configure HTTP retry policies and retry other operations with Kampute.Retry.
---

# Retries

[`Kampute.Retry`](~/api/Kampute.Retry.html) supplies the strategies used by [`Kampute.HttpClient`](~/api/Kampute.HttpClient.html). The core client includes it as a dependency; install it directly when you only need retries for other operations.

## Retry Connection Failures

The default [`HttpRetryPolicy.None`](~/api/Kampute.HttpClient.HttpRetryPolicy.html#Kampute_HttpClient_HttpRetryPolicy_None) does not retry. Set [`RetryPolicy`](~/api/Kampute.HttpClient.HttpRestClient.html#Kampute_HttpClient_HttpRestClient_RetryPolicy) to recover from transient connection failures:

```csharp
using System;
using Kampute.HttpClient;
using Kampute.Retry;

using var client = new HttpRestClient();

client.RetryPolicy = RetryStrategies.Fibonacci(TimeSpan.FromSeconds(1))
    .WithMaxAttempts(5)
    .ToHttpRetryPolicy();
```

[`RetryPolicy`](~/api/Kampute.HttpClient.HttpRestClient.html#Kampute_HttpClient_HttpRestClient_RetryPolicy) controls connection failures. To recover from HTTP error responses such as 429 or 503, register an [error handler](error-handling.md). The connection policy and each retrying error handler keep separate budgets for a request; their limits do not form a single total retry limit.

Use [`HttpRetryPolicy.Dynamic()`](~/api/Kampute.HttpClient.HttpRetryPolicy.html#Kampute_HttpClient_HttpRetryPolicy_Dynamic_System_Func{Kampute_HttpClient_HttpRequestErrorContext_Kampute_Retry_IRetryStrategy}_) when the policy must choose a strategy from the failure context.

## Choose a Strategy

[`RetryStrategies`](~/api/Kampute.Retry.RetryStrategies.html) creates these built-in strategies:

| Strategy | Delay |
| --- | --- |
| [`None`](~/api/Kampute.Retry.RetryStrategies.html#Kampute_Retry_RetryStrategies_None) | No retry. |
| [`Once()`](~/api/Kampute.Retry.RetryStrategies.html#Kampute_Retry_RetryStrategies_Once_System_TimeSpan_) | A single retry after a delay or at a specified time. |
| [`Uniform()`](~/api/Kampute.Retry.RetryStrategies.html#Kampute_Retry_RetryStrategies_Uniform_System_TimeSpan_) | The same delay before every retry. |
| [`Linear()`](~/api/Kampute.Retry.RetryStrategies.html#Kampute_Retry_RetryStrategies_Linear_System_TimeSpan_) | A delay that increases by a fixed step. |
| [`Fibonacci()`](~/api/Kampute.Retry.RetryStrategies.html#Kampute_Retry_RetryStrategies_Fibonacci_System_TimeSpan_) | A delay that grows with the Fibonacci sequence. |
| [`Exponential()`](~/api/Kampute.Retry.RetryStrategies.html#Kampute_Retry_RetryStrategies_Exponential_System_TimeSpan_System_Double_) | A delay multiplied by a fixed rate. |

Except for [`None`](~/api/Kampute.Retry.RetryStrategies.html#Kampute_Retry_RetryStrategies_None) and [`Once()`](~/api/Kampute.Retry.RetryStrategies.html#Kampute_Retry_RetryStrategies_Once_System_TimeSpan_), strategies retry without limit. Chain modifiers to bound retries or spread their delays:

- [`WithMaxAttempts(5)`](~/api/Kampute.Retry.RetryStrategyExtensions.html#Kampute_Retry_RetryStrategyExtensions_WithMaxAttempts_Kampute_Retry_IRetryStrategy_System_UInt32_) allows up to five retries after the initial attempt.
- [`WithTimeout(duration)`](~/api/Kampute.Retry.RetryStrategyExtensions.html#Kampute_Retry_RetryStrategyExtensions_WithTimeout_Kampute_Retry_IRetryStrategy_System_TimeSpan_) limits the time window in which the strategy allows retries. It does not cancel an operation already running.
- [`WithJitter(factor)`](~/api/Kampute.Retry.RetryStrategyExtensions.html#Kampute_Retry_RetryStrategyExtensions_WithJitter_Kampute_Retry_IRetryStrategy_System_Double_) adds randomness to retry delays; the factor must be between 0 and 1.

## Retry Other Operations

Install the standalone package if you are not using the HTTP client:

```shell
dotnet add package Kampute.Retry
```

In a modern .NET application, this example reads a local file and retries [`IOException`](https://learn.microsoft.com/dotnet/api/system.io.ioexception) failures. Replace `data.txt` with your file path.

```csharp
using System;
using System.IO;
using Kampute.Retry;

var retry = RetryStrategies.Exponential(TimeSpan.FromSeconds(1))
    .WithJitter(0.2)
    .WithMaxAttempts(5)
    .WithTimeout(TimeSpan.FromMinutes(2));

var text = await retry.ExecuteAsync(
    ct => File.ReadAllTextAsync("data.txt", ct),
    retryOn: ex => ex is IOException);
```

When the operation throws an exception accepted by [`retryOn`](~/api/Kampute.Retry.RetryStrategyExtensions.html#Kampute_Retry_RetryStrategyExtensions_ExecuteAsync_Kampute_Retry_IRetryStrategy_System_Func{System_Threading_CancellationToken_System_Threading_Tasks_Task}_System_Func{System_Exception_System_Boolean}_System_Threading_CancellationToken_-parameters), [`ExecuteAsync`](~/api/Kampute.Retry.RetryStrategyExtensions.html#Kampute_Retry_RetryStrategyExtensions_ExecuteAsync_Kampute_Retry_IRetryStrategy_System_Func{System_Threading_CancellationToken_System_Threading_Tasks_Task}_System_Func{System_Exception_System_Boolean}_System_Threading_CancellationToken_) waits as the strategy decides and tries again. If no retry remains, the last exception is rethrown with its original stack trace. [`ExecuteAsync<T>`](~/api/Kampute.Retry.RetryStrategyExtensions.html#Kampute_Retry_RetryStrategyExtensions_ExecuteAsync__1_Kampute_Retry_IRetryStrategy_System_Func{System_Threading_CancellationToken_System_Threading_Tasks_Task{__0}}_System_Func{System_Exception_System_Boolean}_System_Threading_CancellationToken_) returns the operation's result; [`Execute`](~/api/Kampute.Retry.RetryStrategyExtensions.html#Kampute_Retry_RetryStrategyExtensions_Execute_Kampute_Retry_IRetryStrategy_System_Action{System_Threading_CancellationToken}_System_Func{System_Exception_System_Boolean}_System_Threading_CancellationToken_) and [`Execute<T>`](~/api/Kampute.Retry.RetryStrategyExtensions.html#Kampute_Retry_RetryStrategyExtensions_Execute__1_Kampute_Retry_IRetryStrategy_System_Func{System_Threading_CancellationToken___0}_System_Func{System_Exception_System_Boolean}_System_Threading_CancellationToken_) do the same for synchronous operations and block while waiting.

Without [`retryOn`](~/api/Kampute.Retry.RetryStrategyExtensions.html#Kampute_Retry_RetryStrategyExtensions_ExecuteAsync_Kampute_Retry_IRetryStrategy_System_Func{System_Threading_CancellationToken_System_Threading_Tasks_Task}_System_Func{System_Exception_System_Boolean}_System_Threading_CancellationToken_-parameters), every exception is eligible for retry except an [`OperationCanceledException`](https://learn.microsoft.com/dotnet/api/system.operationcanceledexception) thrown after the caller's token has been canceled. Pass [`cancellationToken`](~/api/Kampute.Retry.RetryStrategyExtensions.html#Kampute_Retry_RetryStrategyExtensions_ExecuteAsync_Kampute_Retry_IRetryStrategy_System_Func{System_Threading_CancellationToken_System_Threading_Tasks_Task}_System_Func{System_Exception_System_Boolean}_System_Threading_CancellationToken_-parameters) to the execution helper and honor the token supplied to the operation.

## Manage a Session

For an operation with its own retry loop, create a session and call [`WaitAsync()`](~/api/Kampute.Retry.RetrySession.html#Kampute_Retry_RetrySession_WaitAsync_System_Threading_CancellationToken_) after a retryable failure. It waits before the next attempt and returns `false` when no retry remains. Start a new session for each operation.

This fragment assumes a configured `retry` strategy, a caller's `cancellationToken`, and your application's `SendAsync()` operation:

```csharp
var session = retry.StartSession();
while (true)
{
    try
    {
        await SendAsync(cancellationToken);
        break;
    }
    catch (IOException)
    {
        if (!await session.WaitAsync(cancellationToken))
            throw;
    }
}
```

See [`RetryStrategyExtensions`](~/api/Kampute.Retry.RetryStrategyExtensions.html) and [`RetrySession`](~/api/Kampute.Retry.RetrySession.html) for execution, modifier, and session contracts.
