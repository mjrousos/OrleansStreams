using Microsoft.Extensions.Logging;
using Orleans.Streams;

namespace Receiver;

[ImplicitStreamSubscription("StreamNamespace")]
internal class Receiver : Grain, IReceiver
{
    internal readonly ILogger<Receiver> logger;
    private StreamSubscriptionHandle<long>? _subscriptionHandle;
    private DemoEventHandler? _eventHandler;

    public Receiver(ILogger<Receiver> logger)
    {
        this.logger = logger;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Receiver {Id} activated", this.GetPrimaryKeyString());
        _eventHandler = new DemoEventHandler(this);
        var streamProvider = this.GetStreamProvider("StreamProvider");
        var stream = streamProvider.GetStream<long>("StreamNamespace", this.GetPrimaryKeyString());
        _subscriptionHandle = await stream.SubscribeAsync(_eventHandler);
    }
}

class DemoEventHandler : IAsyncObserver<long>
{
    private readonly Receiver _hostingReceiver;
    private int _count = 0;

    public DemoEventHandler(Receiver hostingReceiver)
    {
        _hostingReceiver = hostingReceiver;
    }

    public Task OnCompletedAsync()
    {
        _hostingReceiver.logger.LogInformation("OnCompletedAsync()");
        return Task.CompletedTask;
    }

    public Task OnErrorAsync(Exception ex)
    {
        _hostingReceiver.logger.LogError(ex, "OnErrorAsync()", ex);
        return Task.CompletedTask;
    }

    public Task OnNextAsync(long item, StreamSequenceToken? token = null)
    {
        var latency = DateTimeOffset.UtcNow.Ticks - item;
        _hostingReceiver.logger.LogInformation("Stream {Id} received item #{Count}: {Item}", _hostingReceiver.GetPrimaryKeyString(), Interlocked.Increment(ref _count), item);
        _hostingReceiver.logger.LogInformation("Streaming message latency: {Latency}", TimeSpan.FromTicks(latency));
        return Task.CompletedTask;
    }
}
