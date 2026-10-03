# Kampute.HttpClient - AI Coding Assistant Instructions

## Project Overview

Kampute.HttpClient is a .NET library that enhances the native `HttpClient` for simplified RESTful API communication. It provides a modular, extensible architecture with shared connection pooling, scoped request customization, automatic content deserialization, and built-in retry strategies.

## Architecture & Design Patterns

### Core Components
- **`HttpRestClient`**: Main client class wrapping `HttpClient` with enhanced features
- **Content Formats**: XML support in the core (`Kampute.HttpClient.Xml` namespace) and extension packages for JSON (`Json`, `NewtonsoftJson`)
- **Retry Library**: `Kampute.Retry` package (no dependencies) with retry strategies, sessions and `Execute`/`ExecuteAsync` helpers; the core package depends on it
- **Shared HttpClient**: Connection pooling via `SharedHttpClient` for efficient resource management
- **Scoped Collections**: `ScopedCollection<T>` for temporary header/property overrides

### Key Design Patterns
- **Fluent API**: Extension methods for HTTP verbs (`GetAsync<T>`, `PostAsJsonAsync`, etc.)
- **Event-driven**: `BeforeSendingRequest`/`AfterReceivingResponse` events for interception
- **Strategy Pattern**: `IHttpRetryPolicy` for configurable retry logic, built on `IRetryStrategy` from the `Kampute.Retry` package
- **Factory Pattern**: `RetryStrategies` (in `Kampute.Retry`) for creating retry strategies, and `ToHttpRetryPolicy()` to use one for HTTP requests
- **Decorator Pattern**: `HttpRequestScope` for fluent request configuration

### Request Flow
1. **Request Creation**: `CreateHttpRequest()` builds `HttpRequestMessage` with headers/properties
2. **Pre-processing**: `BeforeSendingRequest` event allows modification
3. **Dispatch**: `DispatchAsync()` sends via underlying `HttpClient`
4. **Retry Logic**: `DispatchWithRetriesAsync()` handles failures with retry policies
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
3. **Serialization**: Derive from `HttpContentFormatter` (or implement `IHttpContentFormatter`) and add to `ContentFormatters`

### Debugging Common Issues
- **Connection Pooling**: Use `SharedHttpClient` reference counting for proper disposal
- **Header Conflicts**: Scoped headers override defaults; avoid setting headers on underlying `HttpClient`
- **Serialization Failures**: Check that the `ContentFormatters` collection has a formatter that reads (for responses) or writes (for `SendObjectAsync` payloads) the media type
- **Retry Behavior**: Verify `RetryPolicy` is set and `ErrorHandlers` are configured
