namespace Apollo.Messaging.Endpoints;

internal class EndpointRegistration<T>(EndpointConfig config, EndpointBuilder builder)
    : EndpointRegistration(typeof(T), config, builder);

public interface IEndpointRegistration
{
    Type EndpointType { get; }
    IEnumerable<Type> HandlerTypes { get; }
    IDictionary<string, Type> SubjectMapping { get; }
    EndpointConfig Config { get; }
    string EndpointRoute { get; }
    IReadOnlyCollection<Type> WiretapTypes { get; }
}

internal class EndpointRegistration : IEndpointRegistration
{
    private readonly EndpointBuilder builder;
    public Type EndpointType { get; }
    public IEnumerable<Type> HandlerTypes { get; }
    public IDictionary<string, Type> SubjectMapping { get; }
    public EndpointConfig Config { get; }
    public string EndpointRoute { get; }
    
    private readonly List<Type> wiretapTypes = [];
    
    public IReadOnlyCollection<Type> WiretapTypes => wiretapTypes;

    public EndpointRegistration(Type endpointType, EndpointConfig config, EndpointBuilder builder)
    {
        this.builder = builder;
        EndpointType = endpointType ?? throw new ArgumentNullException(nameof(endpointType));
        HandlerTypes = EndpointType.MessageHandlerTypes();
        Config = config ?? throw new ArgumentNullException(nameof(config));
        EndpointRoute = Config.Namespace;

        if (config.UseEndpointNameInRoute)
            EndpointRoute += $".{EndpointType.Name}";

        SubjectMapping =
            HandlerTypes
                .ToDictionary(
                    x=>$"{EndpointRoute}.{x.GetMessageType().Name}".ToLower(), // subject 
                    x => x.GetMessageType());// message type
    }
    
    internal void AddWiretap<T>() => AddWiretap(typeof(T));

    private void AddWiretap(Type type)
    {
        wiretapTypes.Add(type);
        builder.AddService(type);
    }
    
}