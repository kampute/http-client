---
title: Retries
summary: Configure HTTP retry policies with strategies from Kampute.Resilience.
---

# Retries

The retry strategies come from the [`Kampute.Resilience`](https://kampute.github.io/resilience/) package, which the core client installs as a dependency. Its [user guide](https://kampute.github.io/resilience/overview/index.html) also covers retrying operations other than HTTP requests.

## Retry Connection Failures

The default [`HttpRetryPolicy.None`](~/api/Kampute.HttpClient.HttpRetryPolicy.html#Kampute_HttpClient_HttpRetryPolicy_None) does not retry. Set [`RetryPolicy`](~/api/Kampute.HttpClient.HttpRestClient.html#Kampute_HttpClient_HttpRestClient_RetryPolicy) to recover from transient connection failures:

```csharp
using System;
using Kampute.HttpClient;
using Kampute.Resilience;

using var client = new HttpRestClient();

client.RetryPolicy = RetryStrategies.Fibonacci(TimeSpan.FromSeconds(1))
    .WithMaxRetries(5)
    .ToHttpRetryPolicy();
```

[`RetryPolicy`](~/api/Kampute.HttpClient.HttpRestClient.html#Kampute_HttpClient_HttpRestClient_RetryPolicy) controls connection failures. To recover from HTTP error responses such as 429 or 503, register an [error handler](error-handling.md). The connection policy and each retrying error handler keep separate budgets for a request; their limits do not form a single total retry limit.

Use [`HttpRetryPolicy.Dynamic()`](~/api/Kampute.HttpClient.HttpRetryPolicy.html#Kampute_HttpClient_HttpRetryPolicy_Dynamic_System_Func{Kampute_HttpClient_HttpRequestErrorContext_Kampute_Resilience_IRetryStrategy}_) when the policy must choose a strategy from the failure context.

## Choose a Strategy

[`RetryStrategies`](https://kampute.github.io/resilience/api/Kampute.Resilience.RetryStrategies.html) creates these built-in strategies:

| Strategy | Delay |
| --- | --- |
| [`None`](https://kampute.github.io/resilience/api/Kampute.Resilience.RetryStrategies.html#Kampute_Resilience_RetryStrategies_None) | No retry. |
| [`Once()`](https://kampute.github.io/resilience/api/Kampute.Resilience.RetryStrategies.html#Kampute_Resilience_RetryStrategies_Once_System_TimeSpan_) | A single retry after a delay or at a specified time. |
| [`Constant()`](https://kampute.github.io/resilience/api/Kampute.Resilience.RetryStrategies.html#Kampute_Resilience_RetryStrategies_Constant_System_TimeSpan_) | The same delay before every retry. |
| [`Linear()`](https://kampute.github.io/resilience/api/Kampute.Resilience.RetryStrategies.html#Kampute_Resilience_RetryStrategies_Linear_System_TimeSpan_) | A delay that increases by a fixed step. |
| [`Fibonacci()`](https://kampute.github.io/resilience/api/Kampute.Resilience.RetryStrategies.html#Kampute_Resilience_RetryStrategies_Fibonacci_System_TimeSpan_) | A delay that grows with the Fibonacci sequence. |
| [`Exponential()`](https://kampute.github.io/resilience/api/Kampute.Resilience.RetryStrategies.html#Kampute_Resilience_RetryStrategies_Exponential_System_TimeSpan_System_Double_) | A delay multiplied by a fixed factor. |

Each factory that takes a `TimeSpan` also accepts the duration as a number of milliseconds, like [`Task.Delay()`](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task.delay); for example, `RetryStrategies.Exponential(500)` starts with a half-second delay. A negative number of milliseconds throws [`ArgumentOutOfRangeException`](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception) rather than meaning an infinite wait.

Except for [`None`](https://kampute.github.io/resilience/api/Kampute.Resilience.RetryStrategies.html#Kampute_Resilience_RetryStrategies_None) and [`Once()`](https://kampute.github.io/resilience/api/Kampute.Resilience.RetryStrategies.html#Kampute_Resilience_RetryStrategies_Once_System_TimeSpan_), strategies retry without limit. Chain modifiers to bound retries, cap delays, or spread them:

- [`WithMaxRetries(5)`](https://kampute.github.io/resilience/api/Kampute.Resilience.RetryStrategyExtensions.html#Kampute_Resilience_RetryStrategyExtensions_WithMaxRetries_Kampute_Resilience_IRetryStrategy_System_UInt32_) allows up to five retries after the initial attempt.
- [`WithMaxElapsedTime(duration)`](https://kampute.github.io/resilience/api/Kampute.Resilience.RetryStrategyExtensions.html#Kampute_Resilience_RetryStrategyExtensions_WithMaxElapsedTime_Kampute_Resilience_IRetryStrategy_System_TimeSpan_) allows retries only while less than `duration` has passed since the first failure it handles for the request. It does not cancel an operation already running.
- [`WithMaxDelay(limit)`](https://kampute.github.io/resilience/api/Kampute.Resilience.RetryStrategyExtensions.html#Kampute_Resilience_RetryStrategyExtensions_WithMaxDelay_Kampute_Resilience_IRetryStrategy_System_TimeSpan_) shortens any delay longer than `limit` to `limit`, which keeps growing strategies from waiting too long. It does not limit the number of retries.
- [`WithJitter(factor)`](https://kampute.github.io/resilience/api/Kampute.Resilience.RetryStrategyExtensions.html#Kampute_Resilience_RetryStrategyExtensions_WithJitter_Kampute_Resilience_IRetryStrategy_System_Double_) adds randomness to retry delays; the factor must be between 0 and 1. Jitter chained after `WithMaxDelay()` can take a delay past the cap by up to that factor; chain it before to keep every delay within the cap.

See [Retry Strategies](https://kampute.github.io/resilience/overview/strategies.html) in the Kampute.Resilience guide for the full contracts and for writing your own strategy.
