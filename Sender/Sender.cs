using Microsoft.Extensions.Logging;
using Orleans.Streams;

namespace Sender;

internal class Sender: Grain, ISender
{
    private readonly ILogger<Sender> _logger;

    public Sender(ILogger<Sender> logger)
{
        _logger = logger;
    }

    public Task StartSendingAsync(int periodMs)
    {
        _logger.LogInformation("Sender {Id} sending event {Period} ms", this.GetPrimaryKeyString(), periodMs);
        var streamProvider = this.GetStreamProvider("StreamProvider");
        var stream = streamProvider.GetStream<long>("StreamNamespace", "StreamA");
        RegisterTimer(_ =>
        {
            _logger.LogInformation("Queueing stream item");
            return stream.OnNextAsync(DateTimeOffset.UtcNow.Ticks);
        }, null, TimeSpan.FromMilliseconds(periodMs), TimeSpan.FromMilliseconds(periodMs));

        return Task.CompletedTask;
    }
}
