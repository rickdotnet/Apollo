// See https://aka.ms/new-console-template for more information

using Apollo.Core;
using Apollo.Core.Configuration;
using Apollo.Core.Hosting;
using Apollo.Core.Messaging;
using Apollo.Core.Messaging.Events;
using Apollo.Core.Messaging.Requests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var config = new ApolloConfig("nats://nats.rhinostack.com:4222");

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddApollo(config)
    .WithRemotePublishing();
    //.WithRemotePublisher("MyEndpoint");

var host = builder.Build();
var publisherFactory = host.Services.GetRequiredService<IRemotePublisherFactory>();

//var remoteDispatcher = publisherFactory.CreatePublisher("MyEndpoint");
var remoteDispatcher = publisherFactory.CreatePublisher("DashboardEndpoint");

Console.WriteLine("Sending remote events...");

var systems = new List<HeartbeatEvent>
{
    new() { Id = "System1", DisplayName = "Main System" },
    new() { Id = "System2", DisplayName = "Test System" },
    new() { Id = "System3", DisplayName = "Some Other System" },
    // Add more systems as needed
};

var tests = new List<AutomatedTestResultEvent>
{
    new() { Id = "Test1", DisplayName = "Integration Test" },
    new() { Id = "Test2", DisplayName = "Unit Test" },
    new() { Id = "Test3", DisplayName = "End-to-End Test" },
};

var systemTasks = new List<Task>
{
    SimulateHeartbeatAsync(remoteDispatcher, new HeartbeatEvent { Id = "System1", DisplayName = "Main System" }, 5),
    SimulateHeartbeatAsync(remoteDispatcher, new HeartbeatEvent { Id = "System2", DisplayName = "Backup System" }, 5),
    SimulateHeartbeatAsync(remoteDispatcher, new HeartbeatEvent { Id = "System3", DisplayName = "Analytics System" }, 15)
};

var testTasks = new List<Task>
{
    // SimulateAutomatedTestAsync(remoteDispatcher, new AutomatedTestResultEvent { Id = "Test1", DisplayName = "Integration Test" }, 60),
    // SimulateAutomatedTestAsync(remoteDispatcher, new AutomatedTestResultEvent { Id = "Test2", DisplayName = "Unit Test" } ,10),
    // SimulateAutomatedTestAsync(remoteDispatcher, new AutomatedTestResultEvent { Id = "Test3", DisplayName = "End-to-End Test" },60),
};

// Run all system and test simulations
await Task.WhenAll(systemTasks.Concat(testTasks));

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

static async Task SimulateAutomatedTestAsync(IRemotePublisher remoteDispatcher, AutomatedTestResultEvent test, int delayInSeconds)
{
    while (true)
    {
        test.Status = GetRandomStatus();
        test.UtcTimestamp = DateTime.UtcNow;
        Console.WriteLine($"Sending test result for {test.DisplayName}...");
        await remoteDispatcher.BroadcastAsync(test, CancellationToken.None);
        await Task.Delay(TimeSpan.FromSeconds(delayInSeconds)); // Delay for each test update
    }
}

// Helper method to return a random status
static string GetRandomStatus()
{
    var statuses = new[] { "Pass", "Fail", "Running", "Skipped" };
    var random = new Random();
    return statuses[random.Next(statuses.Length)];
}

//await host.RunAsync();
public record TestMessage(string Message) : IEvent
{
    public string Message { get; set; } = Message;
}
public record MyRequest(string Message) : IRequest<bool>;

public record HeartbeatEvent : IEvent
{
    public string Id { get; set; } // Unique identifier for the system
    public string DisplayName { get; set; } // Human-readable name of the system
    public DateTime UtcTimestamp { get; set; } // Timestamp of when the heartbeat was received
}

public record AutomatedTestResultEvent : IEvent
{
    public string Id { get; set; } // Unique identifier for the test
    public string DisplayName { get; set; } // Human-readable name of the test
    public string Status { get; set; } // Status of the test (e.g., "Pass", "Fail")
    public string Description { get; set; } // Arbitrary description of the test run
    public DateTime UtcTimestamp { get; set; } // Timestamp of when the test result was received
}