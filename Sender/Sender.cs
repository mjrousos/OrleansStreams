using Microsoft.Extensions.Logging;
using Orleans.Streams;
using System.IO;

namespace Sender;

internal class Sender: Grain, ISender
{
    private readonly ILogger<Sender> _logger;
    private int _count = 0;

    public Sender(ILogger<Sender> logger)
{
        _logger = logger;
    }

    public Task StartSendingAsync(int periodMs, int workerCount, int queueItemsPerWorker)
    {
        _logger.LogInformation("Sender {Id} sending event {Period} ms", this.GetPrimaryKeyString(), periodMs);
        var streamProvider = this.GetStreamProvider("StreamProvider");
        var stream = streamProvider.GetStream<long>("StreamNamespace", "StreamA");
        RegisterTimer(_ =>
        {
            var workerTasks = new List<Task>();
            _count = 0;
            for(var i = 0; i < workerCount; i++)
            {
                workerTasks.Add(QueueItemsAsync(stream, queueItemsPerWorker));
            }
            return Task.WhenAll(workerTasks);
        }, null, TimeSpan.FromMilliseconds(1000), TimeSpan.FromMilliseconds(periodMs));

        return Task.CompletedTask;
    }

    private async Task QueueItemsAsync(IAsyncStream<long> stream, int queueItemsPerWorker)
    {
        for (var i = 0; i < queueItemsPerWorker; i++)
        {
            _logger.LogInformation("Queueing stream item {Count}", Interlocked.Increment(ref _count));
            await stream.OnNextAsync(DateTimeOffset.UtcNow.Ticks);
        }
    }
}
