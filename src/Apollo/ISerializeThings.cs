namespace Apollo;

// junk interface to use until we decide on how we want to serialize things
public interface ISerializeThings
{
    byte[] Serialize<T>(T obj);
    T Deserialize<T>(byte[] obj);
}
