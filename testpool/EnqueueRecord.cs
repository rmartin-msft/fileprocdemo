namespace testpool;

using System.Threading.Tasks;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Azure.Messaging.ServiceBus;
using Azure.Identity;


public class QueueManager : IQueue, IDisposable 
{
  private ConcurrentBag<MyRecord> _buffer = new ConcurrentBag<MyRecord>();
  private readonly ILogger<QueueManager> _logger;
  private readonly IConfiguration? _configuration;
  
  private Lazy<ServiceBusClient> _serviceBusClient;
  private Lazy<ServiceBusSender> _serviceBusSender;
  public QueueManager(ILogger<QueueManager> logger, IConfiguration configuration)
  {
    _logger = logger;
    _configuration = configuration;

    string serviceBusName = _configuration?["ServiceBus:Name"] ?? "defaultServiceBus";
    string recordTopicName = _configuration?["ServiceBus:RecordTopicName"] ?? "defaultRecordTopic";
    string tenantId = _configuration?["ServiceBus:TenantId"] ?? "defaultTenantId";

    _serviceBusClient = new Lazy<ServiceBusClient>(() =>
    {
      ServiceBusClient client = new(serviceBusName,
        new DefaultAzureCredential(new DefaultAzureCredentialOptions()
        {
          TenantId = tenantId
        }
      ));

      return client;
    });
    
    _serviceBusSender = new Lazy<ServiceBusSender>(() =>
    {
      var sender = _serviceBusClient.Value.CreateSender(recordTopicName);

      return sender;    
    });

  }

  public void Dispose()
  {
    if (_serviceBusSender.IsValueCreated)
    {
      _serviceBusSender.Value.DisposeAsync().GetAwaiter().GetResult();
    }

    if (_serviceBusClient.IsValueCreated)
    {
      _serviceBusClient.Value.DisposeAsync().GetAwaiter().GetResult();
    }
  }

  protected async Task FlushBufferToQueueAsync(ConcurrentBag<MyRecord> buffer)
  {
    // This method can be used to flush the buffer to a queue or database.
    // For simplicity, we are not implementing it here.
    // Simulate processing the buffer
    _logger.LogInformation("Buffer limit reached, pushing records onto queue...");

    try
    {
      foreach (var rec in buffer)
      {
        ServiceBusSender? sender = _serviceBusSender.Value;
        if (sender == null)
        {
          _logger.LogError("ServiceBusSender is not initialized.");
          return;
        }

        await sender.SendMessageAsync(
          new ServiceBusMessage(rec.ToString())
        );

        // Simulate processing each record
        _logger.LogDebug($"Adding record: {rec.Id} {rec.FullName}");
      }
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error while flushing buffer to queue.");
    }
  }

  /// <summary>
  /// Enqueues a record for processing.
  /// </summary>
  /// <param name="record">The record to enqueue.</param>
  public void EnqueueRecord(MyRecord record)
  {
    _buffer.Add(record);

    lock (_buffer)
    {
      if (_buffer.Count > 10)
      {
        // If the buffer exceeds a certain limit, flush it to the queue
        Task t = FlushBufferToQueueAsync(_buffer);
        _buffer = new ConcurrentBag<MyRecord>(); // Reset the buffer after flushing        
      }
    }

    // Simulate enqueueing the record
    // _logger.LogDebug($"Enqueued record: {record.Id} {record.FullName}");
  }

  /// <summary>
  /// Asynchronously enqueues a record for processing.
  /// </summary>
  /// <param name="record">The record to enqueue.</param>
  public async Task EnqueueRecordAsync(MyRecord record)
  {
    await Task.Run(() => EnqueueRecord(record));
  }
    
  public void Flush()
  {
    // Ensure that all records in the buffer are processed
    lock (_buffer)
    {

      if (_buffer.Count > 0)
      {
        Task t = FlushBufferToQueueAsync(_buffer);        
        t.Wait(); // Wait for the flush to complete
        _buffer = new ConcurrentBag<MyRecord>(); // Reset the buffer after flushing
      }

      _logger.LogInformation("Flushed all records to the queue.");
    }
  }
}