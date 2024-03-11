# Kampute.HttpClient.Json

`Kampute.HttpClient.Json` is an extension for the [`Kampute.HttpClient`](https://www.nuget.org/packages/Kampute.HttpClient) library,
designed to enhance its functionality by providing support for handling `application/json` content types. This package leverages the
`System.Text.Json` for efficient serialization and deserialization of JSON data, simplifying the process of sending and receiving
JSON payloads in RESTful API communications.

## Installation

Install `Kampute.HttpClient.Json` via NuGet:

```shell
dotnet add package Kampute.HttpClient.Json
```

## Usage

To enable JSON processing capabilities in your `HttpRestClient` instance, simply import the `Kampute.HttpClient.Json` namespace and
use the provided extension methods.

```csharp
using Kampute.HttpClient;
using Kampute.HttpClient.Json;

// Create a new instance of the HttpRestClient.
using var client = new HttpRestClient();

// Configure the client to accept JSON responses.
client.AcceptJson();

// Sending a JSON payload to an API endpoint.
var payload = new MyPayload();
var result = await client.PostAsJsonAsync<MyResult>("https://api.example.com/resource", payload);
```

## Documentation

For details on how to utilize the `Kampute.HttpClient.Json` extension, including class references, method signatures, and property
descriptions, please refer to its [API Documentation](https://kampute.github.io/http-client/api/Kampute.HttpClient.Json.html).

## Contributing

Contributions are welcomed! Please feel free to fork the repository, make changes, and submit pull requests. For major changes or new
features, please open an issue first to discuss what you would like to change.

## License

`Kampute.HttpClient.Json` is licensed under the terms of the [MIT](LICENSE) license.
