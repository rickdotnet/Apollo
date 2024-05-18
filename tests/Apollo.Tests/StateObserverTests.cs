using Castle.Core.Logging;
using FakeItEasy;
using Microsoft.Extensions.Logging;

namespace Apollo.Tests;

public class StateObserverTests
{
    private readonly StateObserver stateObserver;

    public StateObserverTests()
    {
        var fakeLogger = A.Fake<ILogger<StateObserver>>();
        stateObserver = new StateObserver(fakeLogger);
    }
    [Fact]
    public async Task Register_ShouldInvokeCallback_WhenNotified()
    {
        var wasCalled = false;
        Func<TestStateChange, Task> callback = _ =>
        {
            wasCalled = true;
            return Task.CompletedTask;
        };

        using (var _ = stateObserver.Register(callback))
        {
            await stateObserver.NotifyAsync(new TestStateChange());
        }
        
        Assert.True(wasCalled);
    }

    [Fact]
    public async Task Register_ShouldNotInvokeCallback_WhenDisposed()
    {
        var wasCalled = false;
        Func<TestStateChange, Task> callback = _ =>
        {
            wasCalled = true;
            return Task.CompletedTask;
        };

        var subscription = stateObserver.Register(callback);
        subscription.Dispose();

        await stateObserver.NotifyAsync(new TestStateChange());

        Assert.False(wasCalled);
    }

    private class TestStateChange { }
}