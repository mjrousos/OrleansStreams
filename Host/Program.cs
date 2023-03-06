using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sender;

var builder = new HostBuilder()
    .ConfigureAppConfiguration(configBuilder =>
    {
        configBuilder.SetBasePath(Directory.GetCurrentDirectory());
        configBuilder.AddJsonFile("appsettings.json");
        configBuilder.AddUserSecrets<Program>();
        configBuilder.AddEnvironmentVariables();
    })
    .ConfigureLogging(ConfigureLogging)
    .UseOrleans((context, silo) =>
    {
        silo.UseLocalhostClustering()
            .AddMemoryGrainStorageAsDefault()
            .AddMemoryGrainStorage("PubSubStore"); // Required storage for subscription information; doesn't need to be memory, but needs this name.

        switch (context.Configuration["StreamProvider"])
        {
            case "Memory":
                Console.WriteLine("Using in-memory stream provider");
                silo.AddMemoryStreams("StreamProvider");
                break;
            case "EventHub":
                Console.WriteLine("Using event hub stream provider");
                silo.AddEventHubStreams("StreamProvider", c =>
                {
                    c.ConfigureEventHub(b => b.Configure(options =>
                    {
                        options.ConfigureEventHubConnection(
                            context.Configuration["EventHubConnectionString"],
                            context.Configuration["EventHubName"],
                            context.Configuration["ConsumerGroup"]);
                    }));
                    c.UseAzureTableCheckpointer(
                        b => b.Configure(options =>
                        {
                            options.ConfigureTableServiceClient(context.Configuration["TableServiceConnectionString"]);
                            options.PersistInterval = TimeSpan.FromSeconds(10);
                        }));
                });
                break;
            case "StorageQueue":
                Console.WriteLine("Using storage queue stream provider");
                silo.AddAzureQueueStreams("StreamProvider", o => o.Configure(c =>
                {
                    c.ConfigureQueueServiceClient(context.Configuration["QueueConnectionString"]);
                }));
                break;
            default:
                throw new InvalidOperationException("Invalid or missing stream provider setting");
        }
    });

var host = builder.Build();
await host.StartAsync();

var client = host.Services.GetRequiredService<IClusterClient>();
var sender = client.GetGrain<ISender>("SenderA");
await Task.Delay(15 * 1000);
await sender.StartSendingAsync(60 * 10 * 1000, 4, 2500);

Console.WriteLine("Orleans host started.");
Console.WriteLine("Press enter to exit");
await Task.Delay(-1);
//Console.ReadLine();

await host.StopAsync();

void ConfigureLogging(ILoggingBuilder builder)
{
    builder.AddConsole();
}