namespace Apollo.Messaging.Endpoints;

public class EndpointRegistration<T>(EndpointConfig config)
    : EndpointRegistration(typeof(T), config);

public class EndpointRegistration
{
    public Type EndpointType { get; }
    public IEnumerable<Type> HandlerTypes { get; }
    public IEnumerable<string> Subjects { get; }
    public EndpointConfig Config { get; }
    public string EndpointRoute { get; }

    public EndpointRegistration(Type endpointType, EndpointConfig config)
    {
        EndpointType = endpointType ?? throw new ArgumentNullException(nameof(endpointType));
        HandlerTypes = EndpointType.MessageHandlerTypes();
        Config = config ?? throw new ArgumentNullException(nameof(config));
        EndpointRoute = Config.Namespace;

        if (config.UseEndpointNameInRoute)
            EndpointRoute += $".{EndpointType.Name}";

        var subjects =
            HandlerTypes.Select(
                handlerType => $"{EndpointRoute}.{handlerType.GetMessageType().Name}".ToLower()
            ).ToList();

        Subjects = subjects.AsReadOnly();
    }
}