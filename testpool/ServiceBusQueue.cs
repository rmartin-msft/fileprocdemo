namespace testpool;

using System.Threading.Tasks;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Azure.Messaging.ServiceBus;
using Azure.Identity;
using System.Reflection.Metadata.Ecma335;

public class ServiceBusQueue<T> : IQueue2<T> where T : new()
{
  private readonly ILogger<Queue<T>> _logger;    
  private Lazy<ServiceBusClient> _serviceBusClient;
  private Lazy<ServiceBusSender> _serviceBusSender;
  private Lazy<ServiceBusReceiver> _serviceBusReceiver;
  public class QueueConfiguration
  {
    public static readonly QueueConfiguration DefaultConfiguration = new QueueConfiguration();
    public string Topic { get; set; } = "defaultTopic";
    public string SubscriptionName { get; set; } = "defaultSubscription";
    public string ServiceBusName { get; set; } = "defaultServiceBus";
    public string? TenantId { get; set; } = null;
  }


  public ServiceBusQueue(ILogger<Queue<T>> logger, QueueConfiguration? configuration = null)
  {
    if (configuration == null) configuration = QueueConfiguration.DefaultConfiguration;

    Topic = configuration.Topic;
    _logger = logger;
    {
      logger.LogInformation($"Queue instance created Listening on {Topic} for {typeof(T)}.");
    }

    _serviceBusClient = new Lazy<ServiceBusClient>(() =>
    {
      ServiceBusClient client = new(configuration.ServiceBusName,
        new DefaultAzureCredential(new DefaultAzureCredentialOptions()
        {
          TenantId = configuration.TenantId ?? String.Empty
        }
      ));

      return client;
    });

    _serviceBusSender = new Lazy<ServiceBusSender>(() =>
    {
      var sender = _serviceBusClient.Value.CreateSender(Topic);

      return sender;
    });

    _serviceBusReceiver = new Lazy<ServiceBusReceiver>(() =>
    {
      return _serviceBusClient.Value.CreateReceiver(configuration.Topic, configuration.SubscriptionName);
    });
  }

    public string Topic { get; set; }

   

    public async Task EnqueueRecordAsync(T record)
    {
      if (record == null)
      {
        _logger.LogWarning("Attempted to enqueue a null record.");
        return;
      }
      else
      {
        _logger.LogInformation($"Enqueuing record of type {record.ToString() ?? "<EMPTY>"} to topic '{Topic}'");

        string recordData = System.Text.Json.JsonSerializer.Serialize(record);

        await _serviceBusSender.Value.SendMessageAsync(
          new ServiceBusMessage(recordData)
        );
      }
    }

  public async Task<T> DequeueRecordAsync()
  {
    T? record = new T();

    await _serviceBusReceiver.Value.ReceiveMessageAsync().ContinueWith(task =>
    {
      if (task.IsCompletedSuccessfully && task.Result != null)
      {
        record = System.Text.Json.JsonSerializer.Deserialize<T>(task.Result.Body.ToString());        
      }
    });

    return record;           
  }

  public async Task<T> DequeueRecordAsync(Func<T, bool>? onRecordReceived = null, CancellationToken cancellationToken = default)
  {
    T? record = new T();

    await _serviceBusReceiver.Value.ReceiveMessageAsync(null, cancellationToken).ContinueWith(task =>
    {
      if (task.IsCompletedSuccessfully && task.Result != null)
      {
        var record = System.Text.Json.JsonSerializer.Deserialize<T>(task.Result.Body.ToString());

        if (record == null)
        {
          _logger.LogWarning($"DequeueRecordAsync: No record data available in queue for topic '{Topic}'.");
          return;
        }

        if (onRecordReceived?.Invoke(record) == true)
        {
          // If the record was processed successfully, complete the message
          _serviceBusReceiver.Value.CompleteMessageAsync(task.Result);
          _logger.LogInformation($"Record {record.ToString() ?? "<EMPTY>"} processed and completed.");
        }
        else
        {
          // If not processed, abandon the message to make it available again
          _serviceBusReceiver.Value.AbandonMessageAsync(task.Result);
          _logger.LogWarning($"Record {record.ToString() ?? "<EMPTY>"} not processed, message abandoned.");
        }
      }
    });      
    
    return record;
  }

    public void Flush()
  {
    return;
  }
}