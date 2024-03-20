namespace Apollo.Messaging.Endpoints;

public class EndpointBase
{
    private MessageContext context;
    protected MessageContext Context => context;

    internal void SetContext(MessageContext context)
    {
        this.context = context;
    }
}