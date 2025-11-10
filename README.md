# Welcome to HttpClient

`Kampute.HttpClient` is a .NET library designed to simplify HTTP communication with RESTful APIs by enhancing the native `HttpClient` capabilities. Tailored for
developers seeking a potent yet flexible HTTP client for API integration within .NET applications, it combines ease of use with a wide array of functionalities
to address the complexities of web service consumption.

[Explore the API documentation](https://kampute.github.io/http-client/api/) for detailed insights.

## Key Features

- **Shared HttpClient Instances:**
  Facilitates the reuse of a single `HttpClient` instance across multiple `HttpRestClient` instances, promoting efficient resource and connection
  management. This approach significantly boosts performance in scenarios involving concurrent access to multiple services or API endpoints.

- **Flexible HttpClient Configuration:**
  Allows the integration of custom or shared `HttpClient` instances, complete with configurations for message handlers, timeouts, and advanced authentication
  mechanisms to fit specific application needs.

- **Dynamic Request Customization:**
  Offers the capability to define request headers and properties scoped to specific request blocks, allowing for temporary changes that do not affect the global
  configuration. Scoped headers and properties ensure that modifications are contextually isolated, enhancing maintainability and reducing the risk of configuration
  errors during runtime.

- **Custom Error Handling and Exception Management:**
  Converts HTTP response errors into detailed, meaningful exceptions, streamlining the process of interpreting API-specific errors with the aid of a customizable
  error response type set through the `ResponseErrorType` property. Furthermore, it enhances flexibility in error management with the `ErrorHandlers` collection,
  allowing for response status code-specific handling. Developers can craft and utilize custom `IHttpErrorHandler` implementations to address distinct HTTP errors
  directly, facilitating the development of refined retry strategies and precise error responses tailored to specific needs.

- **Retry Strategies with Backoff Mechanisms:**
  Implements backoff strategies to handle transient failures and network interruptions effectively. These strategies, configurable via the `BackoffStrategy`
  property, ensure resilient communication by dictating the logic for retrying requests, thereby preventing server overload and optimizing resource use.

- **Modular Content Processing:**
  Supports extendable serialization/deserialization modules for seamless integration with common and custom content types. It uses a collection of response
  deserializers that automatically convert HTTP response content into .NET objects based on the response's `Content-Type`, and proactively informs the service
  of the content types it is configured to accept by setting the appropriate `Accept` headers. This dual-functionality simplifies the process of working with
  API responses and ensures seamless data integration by aligning expected response formats with the client’s capabilities.

- **Streamlined Authentication and Authorization:**
  Simplifies the process of integrating various authentication schemes and dynamic reauthorization, facilitating straightforward implementation of authentication
  strategies.

- **Request and Response Interception:**
  Provides events such as `BeforeSendingRequest` and `AfterReceivingResponse` for executing custom logic before sending a request or after receiving a response.
  This feature enables detailed request modification, response inspection, and logging, offering developers full control over the HTTP communication process.

- **Asynchronous API for Enhanced Performance:**
  Promotes fully asynchronous network operations with support for cancellation tokens, ensuring efficient management of long-running requests in line with modern
  asynchronous programming practices in .NET.

## Serialization Support

By default, `Kampute.HttpClient` does not include any content deserializer. To accommodate popular content types, the following extension packages are available:

- **[Kampute.HttpClient.Json](https://kampute.github.io/http-client/api/Kampute.HttpClient.Json)**:
  Utilizes the `System.Text.Json` library for handling JSON content types, offering high-performance serialization and deserialization that integrates tightly
  with the .NET ecosystem.

- **[Kampute.HttpClient.NewtonsoftJson](https://kampute.github.io/http-client/api/Kampute.HttpClient.NewtonsoftJson)**:
  Leverages the `Newtonsoft.Json` library for handling JSON content types, providing extensive customization options and compatibility with a vast number of JSON
  features and formats.

- **[Kampute.HttpClient.Xml](https://kampute.github.io/http-client/api/Kampute.HttpClient.Xml)**:
  Employs the `XmlSerializer` for handling XML content types, enabling straightforward serialization and deserialization of XML into .NET objects using custom
  class structures.

- **[Kampute.HttpClient.DataContract](https://kampute.github.io/http-client/api/Kampute.HttpClient.DataContract)**:
  Utilizes the `DataContractSerializer` for handling XML content types, focusing on serialization and deserialization of .NET objects into XML based on data contract
  attributes for fine-grained control over the XML output.

For scenarios where the provided serialization packages do not meet specific requirements, `Kampute.HttpClient` allows the implementation of custom deserializers.
Developers can create their own serialization modules by implementing interfaces for deserialization, thus enabling support for custom content types or proprietary
data formats.

## Installation

Install `Kampute.HttpClient` via NuGet:

```shell
dotnet add package Kampute.HttpClient
```

## Usage Examples

The examples below demonstrate how to use the library for common tasks.

### Basic Usage

To get started with `HttpRestClient`, simply instantiate it and use it to perform HTTP requests.

```csharp
using Kampute.HttpClient;
using Kampute.HttpClient.Json;

// Create a new instance of the HttpRestClient
using var client = new HttpRestClient();

// Configure the client to accept JSON responses, using System.Text.Json library.
// This is an extension method provided by the Kampute.HttpClient.Json package.
client.AcceptJson();

// Perform a GET request.
// The GetAsync<TResponse> method will automatically deserialize the JSON response
// into the specified MyModel type.
var data = await client.GetAsync<MyModel>("https://api.example.com/resource");
```

### Scoped Request Headers

In addition to setting default request headers that apply to all requests, you can define headers for a specific set of requests using a scoped approach. This feature
allows for temporary modifications to headers that override default settings within a defined context. This is particularly useful for handling varying endpoint requirements
or for testing scenarios.

Below is an example that demonstrates how to temporarily override the `Accept` header for a series of requests, ensuring that all requests within the scope explicitly request
a specific media type.

```csharp
using Kampute.HttpClient;

// Create a new instance of the HttpRestClient.
using var client = new HttpRestClient();

string csv;

// Begin a scoped block where the 'Accept' header is set to 'text/csv'.
// All HTTP requests within this using block will include this 'Accept' header.
using (client.BeginHeaderScope(new Dictionary<string, string> { ["Accept"] = MediaTypeNames.Text.Csv }))
{
    // Perform a GET request to retrieve data as CSV. The 'Accept' header for this request
    // will be 'text/csv', as specified by the scoped header.
    csv = await client.GetAsStringAsync("https://api.example.com/resource/csv");
}
```

Alternatively, you can use the `WithScope` extension method to simplify the code as follows:

```csharp
using Kampute.HttpClient;

using var client = new HttpRestClient();

var csv = await client
    .WithScope()
    .SetHeader("Accept", MediaTypeNames.Text.Csv)
    .PerformAsync(scopedClient => scopedClient.GetAsStringAsync("https://api.example.com/resource/csv"));
```

### Scoped Request Properties

Similar to headers, you can also scope request properties. This capability is invaluable in scenarios where you need to maintain state or context-specific information temporarily
during a series of HTTP operations. Scoped properties work similarly to scoped headers, allowing developers to define temporary data attached to requests that are automatically
cleared once the scope is exited. This feature enhances the adaptability of your HTTP interactions, especially in complex or state-dependent communication scenarios.

### Custom Retry Strategies

The library offers various retry strategies to manage transient failures, ensuring your application remains resilient during network instability or temporary
service unavailability. The example below demonstrates how to apply a Fibonacci backoff strategy, which gradually increases the delay between retries, balancing
the need to retry soon against the need to wait longer as the number of attempts increases.

```csharp
using Kampute.HttpClient;

// Create a new instance of the HttpRestClient
using var client = new HttpRestClient();

// Configure the client's retry mechanism.
// The Fibonacci strategy will retry up to 5 times
// with an initial delay of 1 second between retries
// and delay increases following the Fibonacci sequence for subsequent retries.
client.BackoffStrategy = BackoffStrategies.Fibonacci(maxAttempts: 5, initialDelay: TimeSpan.FromSeconds(1));
```

### Handling HTTP Errors

The library includes built-in handlers for managing common HTTP errors, streamlining the implementation of custom logic for error responses.
Here's how to utilize the built-in '401 Unauthorized' error handler:

```csharp
using Kampute.HttpClient;
using Kampute.HttpClient.ErrorHandlers;

// Create an instance of the built-in '401 Unauthorized' error handler.
// This handler defines the logic to handle unauthorized responses.
using var unauthorizedErrorHandler = new HttpError401Handler(async (client, challenges, cancellationToken) =>
{
    // In this example, we're handling the unauthorized error by making a POST request to an
    // authentication endpoint to obtain a new authentication token.
    var auth = await client.PostAsFormAsync<AuthToken>("https://api.example.com/auth",
    [
        KeyValuePair.Create("client_id", MY_APP_ID),
        KeyValuePair.Create("client_secret", MY_APP_SECRET)
    ]);

    // Return a new AuthenticationHeaderValue with the obtained token.
    // This will be used to include the authentication header in subsequent requests.
    return new AuthenticationHeaderValue(AuthSchemes.Bearer, auth.Token);
});

// Create a new instance of the HttpRestClient
using var client = new HttpRestClient();

// Register the unauthorized error handler with the client.
// This allows the client to handle '401 Unauthorized' responses automatically.
client.ErrorHandlers.Add(unauthorizedErrorHandler);
```

Additionally, handling '503 Service Unavailable' and '429 Too Many Requests' errors is simplified with the built-in handler, ensuring your application can gracefully
retry requests during service outages and rate limit encounters.

### Handling Content Types

For handling specific content types like JSON or XML, consider using the available extension packages.

In the example below, we assume that both the `Kampute.HttpClient.NewtonsoftJson` package, which facilitates JSON content handling through the `Newtonsoft.Json`
library, and the `Kampute.HttpClient.DataContract` package, enabling XML content management via `DataContractSerializer`, have been installed.

```csharp
using Kampute.HttpClient;
using Kampute.HttpClient.NewtonsoftJson;
using Kampute.HttpClient.DataContract;

// Create a new instance of the HttpRestClient.
using var client = new HttpRestClient();

// Configure the client to accept JSON responses, using the Newtonsoft.Json library.
// This is an extension method provided by the Kampute.HttpClient.NewtonsoftJson package
client.AcceptJson();

// Configure the client to accept XML responses, using DataContractSerializer.
// This is an extension method provided by the Kampute.HttpClient.DataContract package
client.AcceptXml();

// Execute a GET request. The server may respond in either JSON or XML format.
// The GetAsync<TResponse> method will automatically deserialize the response
// into the specified MyResource type, based on the response content type (JSON or XML).
var result = await client.GetAsync<MyResource>("https://api.example.com/resource");

// Send a PATCH request with a payload in JSON format.
// The PatchAsJsonAsync method is provided by the Kampute.HttpClient.NewtonsoftJson package.
await client.PatchAsJsonAsync("https://api.example.com/resource", new { name = "new name" });

// Send a POST request with a payload in XML format.
// The PostAsXmlAsync method is provided by the Kampute.HttpClient.DataContract package.
var newResource = new MyResource();
await client.PostAsXmlAsync("https://api.example.com/resource", newResource);
```

## Documentation

Explore the `Kampute.HttpClient` library's [API Documentation](https://kampute.github.io/http-client/api/) for an in-depth understanding of its
functionalities. You'll find detailed class references, method signatures, and descriptions of properties to guide your implementation and leverage the library's full
potential.

## Contributing

Contributions welcome! Please follow the existing coding and documentation conventions to maintain consistency across the codebase.

1. Fork the repository
2. Create a feature branch: `git checkout -b feature-name`
3. Commit changes: `git commit -m 'Add feature'`
4. Push branch: `git push origin feature-name`
5. Open a pull request

## License

Licensed under the [MIT License](LICENSE).
