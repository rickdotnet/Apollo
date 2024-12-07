# Apollo Concepts

## Overview

Apollo is a flexible library designed for NATS, with support for a generic in-memory provider. To extend Apollo, developers can implement their own custom providers.

### Endpoints

Endpoints are the central concept in Apollo and listen for incoming messages. They are configured by the consumer via an [EndpointConfig](/source/Apollo.Configuration.EndpointConfig.md) and transformed into a [SubscriptionConfig](/source/Apollo.Configuration.SubscriptionConfig.md) for the [ISubscriptionProvider](/source/Apollo.Abstractions.ISubscriptionProvider.md). Endpoints can be concrete classes that process messages systematically or ad-hoc handlers that receive a raw [ApolloContext](/source/Apollo.ApolloContext.md). This flexibility makes it easy to build dynamic handlers for specific tasks.

**Example**

[!code-csharp[](../../../../demo/ConsoleDemo/TestEndpoint.cs#docs-snippet)]

### Publishers

Publishers are responsible for routing messages to appropriate endpoints. Apollo has different logical concepts:
- **Events**: Use `Broadcast` for publishing events to multiple subscribers.
- **Commands**: Use `Send` for sending commands to a single subscriber.
- **Request/Reply**: Use `Request` for making requests and awaiting replies.

At the moment, `Events` and `Commands` only differ by type and intent.

**Example**

[!code-csharp[](../../../../demo/ConsoleDemo/Demo/HostDemo.cs#docs-snippet-publish)]

### Subscription Config

Subscription configurations contain more properties for providers to use when mapping routes. Providers use these configurations to manage how and where they route incoming messages. This allows the system to adapt dynamically to different communication channels, ensuring seamless message routing between local and remote calls.

### Providers

Providers are the backbone of the routing mechanism within Apollo. They control the route mapping based on the subscription configuration, making it possible to listen on multiple providers and proxy them to others.

### Visual

This is roughly the in-memory implementation, with the `InMemoryProvider` pulling double provider duty. `InMemorySubscription` is the other half of the equation. `DefaultEndpointProvider` also comes free with the `Apollo.Extensions.Microsoft.Hosting` package.

![Apollo In-Memory](/images/apollo-in-memory.png)



