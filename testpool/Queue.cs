namespace testpool;

using System.Threading.Tasks;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Azure.Messaging.ServiceBus;
using Azure.Identity;
using System.Reflection.Metadata.Ecma335;

public class Queue<T> : IQueue<T> where T : new()
{
    public sealed class QueueConfiguration
    {
        public static readonly QueueConfiguration DefaultConfiguration = new QueueConfiguration();
        public string Topic { get; set; } = "defaultTopic";
        public string SubscriptionName { get; set; } = "defaultSubscription";
        public string ServiceBusName { get; set; } = "defaultServiceBus";
    }

    private ConcurrentQueue<string> _queue = new ConcurrentQueue<string>();
    private readonly ILogger<Queue<T>> _logger;
    public Queue(ILogger<Queue<T>> logger, QueueConfiguration? configuration = null)
    {
        if (configuration == null) configuration = QueueConfiguration.DefaultConfiguration;
        
        Topic = configuration.Topic;
        _logger = logger;
        {
            logger.LogInformation($"Queue instance created Listening on {Topic} for {typeof(T)}.");
        }
    }

    public string Topic { get; set; }

    public void EnqueueRecord(T record)
    {        
        string recordData = System.Text.Json.JsonSerializer.Serialize(record);
        _logger.LogInformation($"Enqueuing record {recordData} to topic '{Topic}'");
        _queue.Enqueue(recordData);
    }

    public async Task EnqueueRecordAsync(T record)
    {
        
        await Task.Run(() => EnqueueRecord(record));
    }


    public Task<T> DequeueRecordAsync(Func<T, bool>? onRecordReceived = null, CancellationToken cancellationToken = default)
    {
        throw new System.NotImplementedException("DequeueRecordAsync with callback is not implemented in this queue.");
    }
    public async Task<T> DequeueRecordAsync()
    {
        T? record = new T();

        await Task<T>.Run(() =>
        {
            var recordData = string.Empty;
            _queue.TryDequeue(out recordData);

            if (recordData == null || recordData.Length == 0)
            {
                _logger.LogWarning($"DequeueRecordAsync: No record data available in queue for topic '{Topic}'.");
                return;
            }

            record = System.Text.Json.JsonSerializer.Deserialize<T>(recordData);
        });

        return record;
    }

    public void Flush()
    {
        return;
    }
}