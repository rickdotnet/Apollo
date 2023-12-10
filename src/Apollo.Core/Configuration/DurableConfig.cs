namespace Apollo.Core.Configuration;

public class DurableConfig
{
    public bool IsDurableConsumer { get; set; }
    
    // jetstream options
    
    
    public static DurableConfig Default 
        => new();
}