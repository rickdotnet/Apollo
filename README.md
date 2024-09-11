# Apollo

Apollo aims to be a lightweight, high-performance messaging client designed to provide developers with a simple yet powerful way to incorporate flexible architectures into their .NET applications. 

## Documentation

Apollo is under rapid development. Please refer to the [Apollo Docs](https://apollo.rickdot.net) for the most up-to-date information.

## Example Usage

```csharp
var apolloConfig = new ApolloConfig { InstanceId = "my-instance", DefaultConsumerName = "my-consumer" };
var client = new ApolloClient(apolloConfig, endpointProvider: new EndpointProvider());

// configure and add an endpoint
var endpointConfig = new EndpointConfig
{
    Namespace = "dev.myapp",
    EndpointName = "My Endpoint"
};

var endpoint = client.AddEndpoint<TestEndpoint>(endpointConfig);
_ = endpoint.StartEndpoint(cancellationToken);

await Task.Delay(-1, cancellationToken);

// clean up
await endpoint.DisposeAsync();
```