namespace testpool;

using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Azure.Messaging.ServiceBus;
using Azure.Identity;
using Microsoft.Extensions.Options;

public class ServiceBusQueue<T> : IQueue<T> where T : new()
{
  private readonly ILogger<ServiceBusQueue<T>> _logger;    
  private Lazy<ServiceBusClient> _serviceBusClient;
  private Lazy<ServiceBusSender> _serviceBusSender;
  private Lazy<ServiceBusReceiver> _serviceBusReceiver;
  public sealed class QueueConfiguration
  {    
    public string Topic { get; set; } = "defaultTopic";
    public string SubscriptionName { get; set; } = "defaultSubscription";
    public string ServiceBusName { get; set; } = "defaultServiceBus";
    public string? TenantId { get; set; } = null;
  }


  public ServiceBusQueue(ILogger<ServiceBusQueue<T>> logger, IOptions<ServiceBusQueue<T>.QueueConfiguration> options)
  {

    Topic = options.Value.Topic;
    _logger = logger;
    {
      logger.LogInformation($"Queue instance created Listening on {Topic} for {typeof(T)}.");
    }

    _serviceBusClient = new Lazy<ServiceBusClient>(() =>
    {
      ServiceBusClient client = new(options.Value.ServiceBusName,
        new DefaultAzureCredential(new DefaultAzureCredentialOptions()
        {
          TenantId = options.Value.TenantId ?? string.Empty
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
      return _serviceBusClient.Value.CreateReceiver(options.Value.Topic, options.Value.SubscriptionName);
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
        string recordData = System.Text.Json.JsonSerializer.Serialize(record);
        
        _logger.LogInformation($"Enqueuing '{recordData}' to topic '{Topic}'");

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