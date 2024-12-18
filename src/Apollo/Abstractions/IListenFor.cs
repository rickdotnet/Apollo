﻿namespace Apollo.Abstractions;

public interface IListenFor
{
    
}
public interface IListenFor<in TEvent> : IListenFor where TEvent : IEvent
{
    public Task Handle(TEvent message, ApolloContext context, CancellationToken cancellationToken = default);
}
