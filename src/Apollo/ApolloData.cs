using RickDotNet.Base;
using RickDotNet.Extensions.Base;

namespace Apollo;

public sealed class ApolloData
{
    public Memory<byte> Data { get; }
    public byte[] ToArray() => Data.ToArray();

    internal ApolloData(Memory<byte> data)
    {
        Data = data;
    }

    public static implicit operator ApolloData(byte[] data) => new(data);
    public static implicit operator byte[](ApolloData data) => data.ToArray();

    public override string ToString()
    {
        return System.Text.Encoding.UTF8.GetString(Data.Span);
    }

    /// <summary>
    /// Attempts to deserialize the data into the specified type.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the data into.</typeparam>
    /// <returns>The deserialized data or default if the deserialization failed.</returns>
    public T? As<T>()
    {
        var result = Result.Try(() => System.Text.Json.JsonSerializer.Deserialize<T>(Data.Span));
        return result.ValueOrDefault();
    }

    public object? As(Type type)
    {
        var result = Result.Try(() => System.Text.Json.JsonSerializer.Deserialize(Data.Span, type));
        return result.ValueOrDefault();
    }

    /// <summary>
    /// Creates an ApolloData instance from the specified data.
    /// </summary>
    /// <param name="data">The data to create the ApolloData instance from.</param>
    /// <typeparam name="T">The type of the data.</typeparam>
    /// <returns>An ApolloData instance created from the specified data.</returns>
    public static ApolloData From<T>(T data)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(data);
        return new ApolloData(System.Text.Encoding.UTF8.GetBytes(json));
    }
}