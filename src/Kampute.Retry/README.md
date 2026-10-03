# Kampute.Retry

`Kampute.Retry` is a lightweight .NET library for retrying operations that can fail transiently, such as a network call, a file copy, or access
to a shared resource. It has no dependencies, and it is the retry engine of [`Kampute.HttpClient`](https://www.nuget.org/packages/Kampute.HttpClient).

## Installation

Install `Kampute.Retry` via NuGet:

```shell
dotnet add package Kampute.Retry
```

## Usage

Create a strategy with `RetryStrategies`, chain limits and jitter in any combination, and run the operation with `ExecuteAsync`:

```csharp
using Kampute.Retry;

var retry = RetryStrategies.Exponential(TimeSpan.FromSeconds(1))
    .WithJitter(0.2)
    .WithMaxAttempts(5)
    .WithTimeout(TimeSpan.FromMinutes(2));

await retry.ExecuteAsync(ct => CopyFileAsync(source, target, ct),
    retryOn: ex => ex is IOException, cancellationToken);
```

When the operation throws an exception that `retryOn` accepts, `ExecuteAsync` waits as the strategy decides and tries again. When the strategy
allows no more retries, the last exception is rethrown with its original stack trace. Without `retryOn`, every exception is retried except an
`OperationCanceledException` raised for the caller's token. `ExecuteAsync<T>` returns the value of the operation, and `Execute` and `Execute<T>`
run blocking operations the same way.

The built-in strategies are `None`, `Once`, `Uniform`, `Linear`, `Fibonacci` and `Exponential`. Apart from `None` and `Once`, they retry without
limit until you add `WithMaxAttempts` or `WithTimeout`.

To drive the retries yourself, start a session for the operation and call `WaitAsync` after each failure. It waits for the next delay and returns
`false` when no retry is left:

```csharp
var session = retry.StartSession();
while (true)
{
    try
    {
        await SendAsync();
        break;
    }
    catch (IOException)
    {
        if (!await session.WaitAsync(cancellationToken))
            throw;
    }
}
```

## Documentation

For details, including class references, method signatures, and property descriptions, please refer to the
[API Documentation](https://kampute.github.io/http-client/api/Kampute.Retry.html).

## License

`Kampute.Retry` is licensed under the terms of the [MIT](LICENSE) license.
