# Kampute.HttpClient.Xml

`Kampute.HttpClient.Xml` is an extension for the [`Kampute.HttpClient`](https://www.nuget.org/packages/Kampute.HttpClient) library,
designed to enhance its functionality by providing support for handling `application/xml` content types. This package leverages
the `XmlSerializer` for efficient serialization and deserialization of XML data, simplifying the process of sending and receiving
XML payloads in RESTful API communications.

## Installation

Install `Kampute.HttpClient.Xml` via NuGet:

```shell
dotnet add package Kampute.HttpClient.Xml
```

## Usage

To enable XML processing capabilities in your `HttpRestClient` instance, simply import the `Kampute.HttpClient.Xml` namespace and
use the provided extension methods.

```csharp
using Kampute.HttpClient;
using Kampute.HttpClient.Xml;

// Create a new instance of the HttpRestClient.
using var client = new HttpRestClient();

// Configure the client to accept XML responses.
client.AcceptXml();

// Sending an XML payload to an API endpoint.
var payload = new MyPayload();
var result = await client.PostAsXmlAsync<MyResult>("https://api.example.com/resource", payload);
```

## Documentation

For details on how to utilize the `Kampute.HttpClient.Xml` extension, including class references, method signatures, and property
descriptions, please refer to its [API Documentation](https://kampute.github.io/http-client/api/Kampute.HttpClient.Xml.html).

## Contributing

Contributions are welcomed! Please feel free to fork the repository, make changes, and submit pull requests. For major changes or new
features, please open an issue first to discuss what you would like to change.

## License

`Kampute.HttpClient.Xml` is licensed under the terms of the [MIT](LICENSE) license.
