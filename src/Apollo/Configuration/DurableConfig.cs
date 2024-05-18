namespace Apollo.Configuration;

public class DurableConfig
{
    public bool IsDurableConsumer { get; set; }
    
    public static DurableConfig Default 
        => new();
}