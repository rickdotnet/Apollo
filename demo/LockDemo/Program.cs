using Apollo.Lock;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.KeyValueStore;

var owner = Guid.NewGuid().ToString();
Console.WriteLine($"Owner: {owner}");

var opts = new NatsOpts { Url = "nats://nats.rhinostack.com:4222" };
await using var nats = new NatsConnection(opts);

var js = new NatsJSContext(nats);
var kv = new NatsKVContext(js);

var lockFactory = new DistributedLockStoreFactory(kv);
var store = await lockFactory.CreateAsync(owner);

var t1 = TestLock();
var t2 = TestLock2();
await Task.WhenAll(t1,t2);

async Task TestLock()
{
    var myLock = store.CreateLock("my-lock");
    await using (await myLock.LockAsync())
    {
        Console.WriteLine("Acquired first lock");
        await Task.Delay(TimeSpan.FromSeconds(5));
        Console.WriteLine("Releasing first lock");
    }
}
async Task TestLock2()
{
    var myLock = store.CreateLock("my-lock");
    await using (await myLock.LockAsync())
    {
        Console.WriteLine("Acquired second lock");
        await Task.Delay(TimeSpan.FromSeconds(5));
        Console.WriteLine("Releasing second lock");
    }
}