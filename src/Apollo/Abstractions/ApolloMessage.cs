using Microsoft.Extensions.Primitives;

namespace Apollo.Abstractions;

//public record ApolloMessage : ApolloMessage<object>;
//public record ApolloMessage<T>
public record ApolloMessage
{
    public string Subject { get; set; } = string.Empty;
    public IDictionary<string, StringValues>? Headers { get; set; } = new Dictionary<string, StringValues>();
    public Type? MessageType { get; set; }
    public byte[]? Data { get; set; }
    public override string ToString() => "Apollo Message!!";
}