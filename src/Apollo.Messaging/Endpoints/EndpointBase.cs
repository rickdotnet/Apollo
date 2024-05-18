namespace Apollo.Messaging.Endpoints;

public class EndpointBase
{
    private MessageContext context = null!;
    protected MessageContext Context => context;

    internal void SetContext(MessageContext messageContext)
    {
        context = messageContext;
    }
}