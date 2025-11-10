# Kampute.HttpClient - AI Coding Assistant Instructions

## Project Overview

Kampute.HttpClient is a .NET library that enhances the native `HttpClient` for simplified RESTful API communication. It provides a modular, extensible architecture with shared connection pooling, scoped request customization, automatic content deserialization, and built-in retry strategies.

## Architecture & Design Patterns

### Core Components
- **`HttpRestClient`**: Main client class wrapping `HttpClient` with enhanced features
- **Extension Packages**: Modular serialization support (`Json`, `Xml`, `DataContract`, `NewtonsoftJson`)
- **Shared HttpClient**: Connection pooling via `SharedHttpClient` for efficient resource management
- **Scoped Collections**: `ScopedCollection<T>` for temporary header/property overrides

### Key Design Patterns
- **Fluent API**: Extension methods for HTTP verbs (`GetAsync<T>`, `PostAsJsonAsync`, etc.)
- **Event-driven**: `BeforeSendingRequest`/`AfterReceivingResponse` events for interception
- **Strategy Pattern**: `IHttpBackoffProvider` for configurable retry logic
- **Factory Pattern**: `BackoffStrategies` for creating retry policies
- **Decorator Pattern**: `HttpRequestScope` for fluent request configuration

### Request Flow
1. **Request Creation**: `CreateHttpRequest()` builds `HttpRequestMessage` with headers/properties
2. **Pre-processing**: `BeforeSendingRequest` event allows modification
3. **Dispatch**: `DispatchAsync()` sends via underlying `HttpClient`
4. **Retry Logic**: `DispatchWithRetriesAsync()` handles failures with backoff strategies
5. **Response Processing**: `DeserializeContentAsync()` converts response to .NET objects
6. **Post-processing**: `AfterReceivingResponse` event for inspection/logging

## Critical Developer Workflows

### Building & Testing
```bash
# Build solution
dotnet build -c Release

# Run all tests
dotnet test --verbosity minimal

# Run specific test project
dotnet test tests/Kampute.HttpClient.Test/

# Generate documentation
kampose build
```

### Adding New Features
1. **Core Features**: Modify `HttpRestClient.cs` and add tests in corresponding test file
2. **Extensions**: Create new package in `src/Kampute.HttpClient.*` with matching test project
3. **Serialization**: Implement `IHttpContentDeserializer` and add to `ResponseDeserializers`

### Debugging Common Issues
- **Connection Pooling**: Use `SharedHttpClient` reference counting for proper disposal
- **Header Conflicts**: Scoped headers override defaults; avoid setting headers on underlying `HttpClient`
- **Serialization Failures**: Check `ResponseDeserializers` collection has appropriate deserializer
- **Retry Behavior**: Verify `BackoffStrategy` is set and `ErrorHandlers` are configured

## Project-Specific Conventions

### Code Style & Structure
- **Namespaces**: `Kampute.HttpClient` (core), `Kampute.HttpClient.*` (extensions)
- **Naming**: PascalCase for public APIs, consistent with .NET conventions
- **Documentation**: XML comments with `<summary>`, `<remarks>`, and `<example>` sections
- **Nullability**: `Nullable` enabled with proper `?` annotations
- **Async/Await**: Fully asynchronous APIs with `CancellationToken` support

### Testing Patterns
- **Framework**: NUnit with Moq for mocking
- **Structure**: Test classes mirror source structure (`HttpRestClientTests.cs`)
- **Mocking**: `Mock<HttpMessageHandler>` for HTTP interactions
- **Helpers**: `TestHelpers` namespace for shared test utilities
- **Coverage**: Comprehensive unit tests for all public APIs

### Extension Package Pattern
```csharp
// Extension method pattern
public static class HttpRestClientJsonExtensions
{
    public static void AcceptJson(this HttpRestClient client)
    {
        client.ResponseDeserializers.Add(new JsonContentDeserializer());
    }

    public static Task<T> PostAsJsonAsync<T>(this HttpRestClient client, string uri, object payload)
    {
        return client.SendAsync<T>(HttpVerb.Post, uri, CreateJsonContent(payload));
    }
}
```

### Error Handling
- **Exceptions**: `HttpResponseException` for HTTP errors, `HttpContentException` for deserialization failures
- **Custom Errors**: Implement `IHttpErrorResponse` for structured error responses
- **Retry Logic**: `IHttpErrorHandler` implementations for status-code specific handling
- **Logging**: Use request/response events for comprehensive logging

### Configuration Management
- **Base Address**: Trailing slash handling in `BaseAddress` setter
- **Headers**: `DefaultRequestHeaders` vs scoped headers precedence
- **Accept Headers**: Auto-generated from `ResponseDeserializers` if not specified
- **Properties**: Request-scoped properties via `HttpRequestMessagePropertyKeys`

## Integration Points

### External Dependencies
- **Core**: `System.Net.Http` (native .NET)
- **JSON**: `System.Text.Json` or `Newtonsoft.Json`
- **XML**: `System.Runtime.Serialization` or `System.Xml.Serialization`
- **Testing**: NUnit, Moq, Microsoft.NET.Test.Sdk

### Cross-Component Communication
- **Events**: `BeforeSendingRequest`/`AfterReceivingResponse` for observability
- **Scopes**: `BeginHeaderScope()`/`BeginPropertyScope()` for request customization
- **Extensions**: Fluent chaining via `HttpRequestScope.WithScope().SetHeader()...PerformAsync()`
- **Handlers**: `ErrorHandlers` collection for pluggable error handling

## Key Files & Directories

### Core Implementation
- `src/Kampute.HttpClient/HttpRestClient.cs` - Main client implementation
- `src/Kampute.HttpClient/HttpRestClientExtensions.cs` - HTTP verb extensions
- `src/Kampute.HttpClient/BackoffStrategies.cs` - Retry strategy factories
- `src/Kampute.HttpClient/Utilities/ScopedCollection.cs` - Scoped state management

### Extension Packages
- `src/Kampute.HttpClient.Json/` - System.Text.Json integration
- `src/Kampute.HttpClient.Xml/` - XML serialization support
- `src/Kampute.HttpClient.DataContract/` - DataContractSerializer integration
- `src/Kampute.HttpClient.NewtonsoftJson/` - Newtonsoft.Json support

### Testing
- `tests/Kampute.HttpClient.Test/` - Core functionality tests
- `tests/Kampute.HttpClient.Json.Test/` - JSON extension tests
- `TestHelpers/` - Shared testing utilities

### Build & CI/CD
- `Kampute.HttpClient.sln` - Solution file
- `.github/workflows/main.yml` - GitHub Actions CI/CD
- `kampose.json` - Documentation generation config

## Common Patterns & Examples

### Basic Usage
```csharp
using var client = new HttpRestClient();
client.AcceptJson(); // Add JSON deserializer

var data = await client.GetAsync<MyModel>("https://api.example.com/data");
```

### Scoped Configuration
```csharp
using var client = new HttpRestClient();

var result = await client
    .WithScope()
    .SetHeader("Authorization", $"Bearer {token}")
    .SetProperty("CustomData", context)
    .PerformAsync(scoped => scoped.GetAsync<Data>("endpoint"));
```

### Error Handling & Retry
```csharp
client.BackoffStrategy = BackoffStrategies.Fibonacci(maxAttempts: 5, initialDelay: TimeSpan.FromSeconds(1));

client.ErrorHandlers.Add(new HttpError401Handler(async (client, challenges, token) => {
    var auth = await client.PostAsFormAsync<AuthToken>("auth/refresh", new { refreshToken });
    return new AuthenticationHeaderValue("Bearer", auth.AccessToken);
}));
```

### Custom Serialization
```csharp
public class CustomDeserializer : IHttpContentDeserializer
{
    public bool CanDeserialize(string mediaType, Type objectType) =>
        mediaType == "application/custom" && objectType == typeof(CustomType);

    public Task<object> DeserializeAsync(HttpContent content, Type objectType, CancellationToken token) =>
        // Custom deserialization logic
}

client.ResponseDeserializers.Add(new CustomDeserializer());
```

## Quality Assurance

### Code Quality Checks
- **Build**: `dotnet build -c Release` ensures compilation
- **Tests**: `dotnet test` runs full test suite
- **Documentation**: `kampose build` generates API docs
- **Analysis**: Nullable reference types enabled for null safety

### Performance Considerations
- **Connection Pooling**: Use `SharedHttpClient` for multiple client instances
- **Async Operations**: All I/O operations are fully asynchronous
- **Memory Management**: Proper `IDisposable` implementation with reference counting
- **Serialization**: Efficient deserialization with content-type matching

### Security Best Practices
- **Header Injection**: Scoped headers prevent accidental global state changes
- **Authentication**: Built-in support for various auth schemes via error handlers
- **Cancellation**: `CancellationToken` support throughout async APIs
- **Error Handling**: Structured error responses prevent information leakage

## Development Guidelines
- **SOLID principles**: Prioritize Single Responsibility and Open/Closed principles
- **Simplicity over complexity**: Avoid excessive helper methods and unnecessary abstractions
- **Self-documenting code**: Code should be readable without redundant inline comments
- **Interface pragmatism**: Use interfaces judiciously - avoid "Ravioli" (too many small interfaces) and "Lasagna" (too many layers)
- **Performance & clarity**: Optimize for both execution speed and code understanding
- **Problem-solving approach**: Question existing solutions, propose improvements, document limitations when needed

## Documentation Guidelines
- Avoid promotional, flowery, or overly embellished language, and use adjectives and adverbs only when strictly necessary.
- Emphasize purpose and usage; do not document implementation details or obvious information.
- Provide meaningful context and call out important behavioral nuances or edge cases.
- Keep content concise and focused by using short sentences and brief paragraphs to convey information clearly.
- Organize content using bullet points, numbered lists, or tables when appropriate, and explain the context or purpose of the list or table in at least one paragraph.
- Numbered lists should be used for steps in a process or sequence only.
- When writing XML documentation comments, use appropriate XML tags for references, language keywords, and for organizing information in lists and tables. Ensure tags are used to clarify context, structure information, and improve readability for consumers of the documentation.
