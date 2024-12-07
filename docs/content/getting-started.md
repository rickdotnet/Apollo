## Getting Started

Install Apollo via NuGet:

```sh
# quickest way to get started
dotnet add package RickDotNet.Apollo.Extensions.Microsoft.Hosting 

# or, if you want to use Apollo without hosting extension methods
dotnet add package RickDotNet.Apollo
```

### Quickstart

The following example demonstrates how to set up Apollo with an In-Memory provider. This basic example will help you understand the core concepts and get up and running quickly.

**Hosted Example**

[!code-csharp[](../../demo/ConsoleDemo/Demo/HostDemo.cs#docs-snippet-host)]

**Publishing**

[!code-csharp[](../../demo/ConsoleDemo/Demo/HostDemo.cs#docs-snippet-publish)]

**Direct Usage**

[!code-csharp[](../../demo/ConsoleDemo/Demo/Direct.cs#docs-snippet)]

**Test Endpoint**

[!code-csharp[](../../demo/ConsoleDemo/TestEndpoint.cs#docs-snippet)]


