# Kampute.HttpClient.DataContract

`Kampute.HttpClient.DataContract` is an extension for the [`Kampute.HttpClient`](https://www.nuget.org/packages/Kampute.HttpClient)
library, designed to enhance its functionality by providing support for handling `application/xml` content types. This package leverages
the `DataContractSerializer` for efficient serialization and deserialization of XML data, simplifying the process of sending and receiving
XML payloads in RESTful API communications.

## Installation

Install `Kampute.HttpClient.DataContract` via NuGet:

```shell
dotnet add package Kampute.HttpClient.DataContract
```

## Usage

To enable XML processing capabilities in your `HttpRestClient` instance, simply import the `Kampute.HttpClient.DataContract` namespace and
use the provided extension methods.

```csharp
using Kampute.HttpClient;
using Kampute.HttpClient.DataContract;

// Create a new instance of the HttpRestClient.
using var client = new HttpRestClient();

// Configure the client to accept XML responses.
client.AcceptXml();

// Sending an XML payload to an API endpoint.
var payload = new MyPayload();
var result = await client.PostAsXmlAsync<MyResult>("https://api.example.com/resource", payload);
```

## Contributing

Contributions are welcomed! Please feel free to fork the repository, make changes, and submit pull requests. For major changes or new features,
please open an issue first to discuss what you would like to change.

## License

`Kampute.HttpClient.DataContract` is licensed under the terms of the MIT license. See the [LICENSE](LICENSE) file for more details.
