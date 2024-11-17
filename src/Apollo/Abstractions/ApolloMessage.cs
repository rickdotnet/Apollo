using Microsoft.Extensions.Primitives;

namespace Apollo.Abstractions;

public sealed record ApolloMessage
{
    public string Subject { get; set; } = string.Empty;
    public IDictionary<string, StringValues> Headers { get; set; } = new Dictionary<string, StringValues>();
    public Type? MessageType { get; set; }
    public ApolloData? Data { get; set; }
    public override string ToString() => "Apollo Message!!";
}