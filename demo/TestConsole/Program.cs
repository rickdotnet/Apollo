using Apollo;
using Apollo.Abstractions.Messaging.Events;
using Apollo.Abstractions.Messaging.Requests;
using Apollo.Configuration;
using Apollo.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TestConsole;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddEnvironmentVariables("APOLLO_");
builder.Configuration.AddJsonFile("apolloConfig.json", optional: true);

var config = ApolloConfig.Default;
builder.Configuration.Bind(config);

builder.Services
    .AddApollo(config, x => x.WithRemotePublishing());

var host = builder.Build();
var publisherFactory = host.Services.GetRequiredService<IRemotePublisherFactory>();

//var remoteDispatcher = publisherFactory.CreatePublisher("DockerEndpoint");
//var response = await remoteDispatcher.SendRequestAsync<ListServicesRequest,ListServicesResponse>(new ListServicesRequest(), default);

var remoteDispatcher = publisherFactory.CreatePublisher("MyEndpoint");
await remoteDispatcher.BroadcastAsync(new TestEvent("My Test Event"), default);

var remoteDispatcher2 = publisherFactory.CreatePublisher("MyReplyEndpoint");
await remoteDispatcher2.SendRequestAsync<MyRequest, bool>(new MyRequest("My Request"), default);



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

namespace TestConsole
{
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

    public record ListServicesRequest : IRequest<ListServicesResponse>;

    public record ListServicesResponse(string[] Services);
}

public record MyRequest(string Message): IRequest<bool>;