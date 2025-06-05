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
  private readonly IQueue2<MyRecord> _queue;
  private int _targetRate =  -1; // default to infinite rate
  private readonly TimeSpan _processingRate = TimeSpan.FromSeconds(5);

  public FileProcessorService(
      ILogger<FileProcessorService> logger,
      IQueue2<MyRecord> queue,
      IConfiguration configuration, 
      int targetRate = -1)
  {
    _logger = logger;
    _queue = queue;
    _configuration = configuration;    
    _targetRate = targetRate;

    _logger.LogInformation($"FileProcessorService initialized and running target througput {(targetRate == -1 ? "Infinite" : targetRate)} / sec.");
  }

  public async Task StartAsync(CancellationToken cancellationToken)
  {    
    int newWaitTime = 0;
    double targetTimePerOperation = double.Max(0, 1000 / _targetRate);

    // we will use a stopwatch to measure the time taken to process the messages
    System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();

    while (cancellationToken.IsCancellationRequested == false)
    {
      try
      {
        // wait for the processing rate         
        if (newWaitTime > 0) await Task.Delay(newWaitTime, cancellationToken);
        // receive a message from the queue
        var receivedMessage = await _queue.DequeueRecordAsync((receivedMessage) =>
          {
            // process the message
            // get the message body as a string

            _logger.LogInformation($"Received message: {receivedMessage?.ToString() ?? "<EMPTY>"}");

            // Simulate Calling the API which will process the the message

            return true; // return true to indicate that the message was passed to the API for processing          

          }
        , cancellationToken);

        newWaitTime = int.Max(0, (int)(targetTimePerOperation - stopwatch.ElapsedMilliseconds));
        _logger.LogInformation($"Operation Delay now {newWaitTime}");
        
        stopwatch.Restart();
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