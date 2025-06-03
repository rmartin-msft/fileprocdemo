namespace testpool;

using Microsoft.Extensions.Hosting;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Azure.Messaging.ServiceBus;
using Azure.Identity;
using System.Threading;

class FileProcessorService : IHostedService
{
  private readonly IConfiguration _configuration;
  private readonly ILogger<FileProcessorService> _logger;

  public FileProcessorService(
      ILogger<FileProcessorService> logger,
      IConfiguration configuration)
  {
    _logger = logger;
    _configuration = configuration;

    _logger.LogInformation("FileProcessorService initialized.");
  }

  public async Task StartAsync(CancellationToken cancellationToken)
  {
    string serviceBusName = _configuration["ServiceBus:Name"] ?? "defaultServiceBus";
    string topicName = _configuration["ServiceBus:RecordTopicName"] ?? "defaultRecordTopic";
    string subscriptionName = _configuration["ServiceBus:SubscriptionName"] ?? "defaultSubscription";
    string tenantId = _configuration["ServiceBus:TenantId"] ?? "defaultTenantId";

    await using ServiceBusClient client = new(serviceBusName,
        new DefaultAzureCredential(new DefaultAzureCredentialOptions()
        {
          TenantId = tenantId
        }
      ));

    // create a receiver for our subscription that we can use to receive the message
    ServiceBusReceiver receiver = client.CreateReceiver(topicName, subscriptionName);

    // the received message is a different type as it contains some service set properties
    while (cancellationToken.IsCancellationRequested == false)
    {
      try
      {
        // receive a message from the queue
        ServiceBusReceivedMessage receivedMessage = await receiver.ReceiveMessageAsync(null, cancellationToken);

        if (receivedMessage != null)
        {
          // process the message

          // get the message body as a string
          string body = receivedMessage.Body.ToString();
          Console.WriteLine(body);

          // ProcessMessageAsync(receivedMessage).GetAwaiter().GetResult();

          // complete the message so that it is not received again
          receiver.CompleteMessageAsync(receivedMessage, cancellationToken).GetAwaiter().GetResult();
        }
      }
      catch (TaskCanceledException)
      { 
        
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error processing message.");
      }
    }

    _logger.LogInformation("Cancellation requested, stopping FileProcessorService.");
     
    await Task.CompletedTask;
  }

  public async Task StopAsync(CancellationToken cancellationToken)
  {
      await Task.CompletedTask;
  }
}