# Kampute.Retry

A .NET library for retrying operations that fail transiently. It provides composable delay strategies, retry sessions, and synchronous and asynchronous execution helpers, with no package dependencies.

[Retry guide](https://kampute.github.io/http-client/overview/retries.html) · [API reference](https://kampute.github.io/http-client/api/Kampute.Retry.html)

## Installation

```shell
dotnet add package Kampute.Retry
```

## Usage

In a modern .NET application, this example retries an asynchronous file read on `IOException`, up to five retries after the initial attempt. Replace `data.txt` with your file path.

```csharp
using System;
using System.IO;
using Kampute.Retry;

var retry = RetryStrategies
    .Exponential(TimeSpan.FromSeconds(1))
    .WithJitter(0.2)
    .WithMaxAttempts(5);

var text = await retry.ExecuteAsync(
    ct => File.ReadAllTextAsync("data.txt", ct),
    retryOn: ex => ex is IOException);
```

See the [retry guide](https://kampute.github.io/http-client/overview/retries.html) for strategy selection, timeouts, cancellation, manual sessions, and use with `HttpRestClient`.

## License

[MIT License](LICENSE).
