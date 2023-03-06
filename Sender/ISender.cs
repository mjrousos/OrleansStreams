namespace Sender;
public interface ISender : IGrainWithStringKey 
{
    Task StartSendingAsync(int periodMs, int workerCount, int queueItemsPerWorker);
}
