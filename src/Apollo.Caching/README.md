# Apollo.Caching

Apollo.Caching is an extension of the [Apollo](https://github.com/rickdotnet/Apollo) library that provides a distributed caching solution using NATS as the meat and potatoes. It leverages the high-performance and scalability of NATS to offer a robust caching mechanism suitable for microservices and distributed systems built with .NET.

## Features

- **Distributed Caching**: Share cache data across multiple instances of your application.
- **NATS Integration**: Built directly on top of the NATS messaging system for seamless communication.
- **MessagePack Serialization**: Utilizes MessagePack for efficient binary serialization of cache items.
- **Easy Setup**: Integrate distributed caching into your Apollo setup with minimal configuration.
- **Expiration Support**: Supports absolute and sliding expiration for cache entries.

## Getting Started

Before using Apollo.Caching, ensure that you have a running instance of NATS and the Apollo library set up in your project. Follow the instructions in the [Apollo](https://github.com/rickdotnet/Apollo) README.md to set up Apollo and NATS.

## Installation

To add Apollo.Caching to your project, you can install it via NuGet:

```
Install-Package RickDotNet.Apollo.Caching
```

Or via the .NET CLI:

```
dotnet add package RickDotNet.Apollo.Caching
```

For more information, visit the [NuGet package page](https://www.nuget.org/packages/RickDotNet.Apollo.Caching/).

## Usage

To use Apollo.Caching, you need to configure the service in your application's setup. Here's an example of how to register the distributed cache in your `Startup.cs` or program initialization:

```cs
using Apollo.Caching;
using Microsoft.Extensions.DependencyInjection;

var builder = Host.CreateApplicationBuilder(args);

// Add Apollo services
builder.Services
    .AddApollo(config)
    .AddCaching(); // This will register the NatsDistributedCache as IDistributedCache

var host = builder.Build();

// Now you can use IDistributedCache in your application
var cache = host.Services.GetRequiredService<IDistributedCache>();

await cache.SetAsync("my-key", "my-value"u8.ToArray(), new DistributedCacheEntryOptions
{
    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10)
});

var value = cache.Get("my-key");
Console.WriteLine($"[GET] {Encoding.UTF8.GetString(value!)}");
```

## Configuration

The `NatsDistributedCache` can be configured with a custom bucket name by passing it to the constructor. If no bucket name is provided, it defaults to "apollocache".

## Contributing

Contributions are welcome! Feel free to submit pull requests or create issues if you find bugs or have feature suggestions.

## License

Apollo.Caching is licensed under the [MIT License](LICENSE).