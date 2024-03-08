﻿using Apollo;
using Apollo.Configuration;
using Apollo.Messaging;
using Apollo.Abstractions.Messaging.Events;
using Apollo.Abstractions.Messaging.Requests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var config = new ApolloConfig("nats://nats.rhinostack.com:4222");

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddApollo(config, x => x.WithRemotePublishing());

var host = builder.Build();
var publisherFactory = host.Services.GetRequiredService<IRemotePublisherFactory>();

var remoteDispatcher = publisherFactory.CreatePublisher("MyEndpoint");
await remoteDispatcher.BroadcastAsync(new TestEvent("My Event"), default);

//var remoteDispatcher = publisherFactory.CreatePublisher("DashboardEndpoint");

return;
Console.WriteLine("Sending remote events...");

var systems = new List<HeartbeatEvent>
{
    new() { Id = "System1", DisplayName = "Main System" },
    new() { Id = "System2", DisplayName = "Test System" },
    new() { Id = "System3", DisplayName = "Some Other System" },
    // Add more systems as needed
};

var systemTasks = new List<Task>
{
    SimulateHeartbeatAsync(remoteDispatcher, new HeartbeatEvent { Id = "System1", DisplayName = "Main System" }, 5),
    SimulateHeartbeatAsync(remoteDispatcher, new HeartbeatEvent { Id = "System2", DisplayName = "Backup System" }, 5),
    SimulateHeartbeatAsync(remoteDispatcher, new HeartbeatEvent { Id = "System3", DisplayName = "Analytics System" },
        15)
};

Console.WriteLine("Sent remote event");
//await Task.Delay(5000);

Console.WriteLine("Closing");

static async Task SimulateHeartbeatAsync(IRemotePublisher remoteDispatcher, HeartbeatEvent system, int delayInSeconds)
{
    while (true)
    {
        system.UtcTimestamp = DateTime.UtcNow;
        Console.WriteLine($"Sending heartbeat for {system.DisplayName}...");
        await remoteDispatcher.BroadcastAsync(system, CancellationToken.None);
        await Task.Delay(TimeSpan.FromSeconds(delayInSeconds));
    }
}

public record TestEvent(string Message) : IEvent;

//await host.RunAsync();
public record TestMessage : IEvent
{
    public TestMessage(string Message)
    {
        this.Message = Message;
    }

    public string Message { get; set; }

    public void Deconstruct(out string Message)
    {
        Message = this.Message;
    }
}

public record MyRequest : IRequest<bool>
{
    public MyRequest(string Message)
    {
        this.Message = Message;
    }

    public string Message { get; init; }

    public void Deconstruct(out string Message)
    {
        Message = this.Message;
    }
}

public record HeartbeatEvent : IEvent
{
    public string Id { get; set; } // Unique identifier for the system
    public string DisplayName { get; set; } // Human-readable name of the system
    public DateTime UtcTimestamp { get; set; } // Timestamp of when the heartbeat was received
}