namespace testpool;

using System.Threading.Tasks;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Azure.Messaging.ServiceBus;
using Azure.Identity;
using System.Reflection.Metadata.Ecma335;

public class Queue<T> : IQueue2<T> where T : new()
{
    private ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();
    private readonly ILogger<Queue<T>> _logger;
    public Queue(ILogger<Queue<T>> logger, string topic = "defaultTopic")
    {
        Topic = topic;
        _logger = logger;
        {
            logger.LogInformation("Queue instance created.");
        }
    }

    public string Topic { get; set; }

    public void EnqueueRecord(T record)
    {
        _logger.LogInformation($"Enqueuing record of type {record.ToString() ?? "<EMPTY>"} to topic '{Topic}'");
        _queue.Enqueue(record);
    }

    public async Task EnqueueRecordAsync(T record)
    {
        await Task.Run(() => EnqueueRecord(record));
    }

    public async Task<T> DequeueRecordAsync()
    {
        T? record = new T();

        await Task<T>.Run(() =>
        {
            _queue.TryDequeue(out record);
        });

        return record; 
    }

    public void Flush()
    {
        return;
    }
}