namespace testpool;

using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Threading;
using Microsoft.Extensions.Options;

public sealed class FileProcessorServiceOptions
{
  public int TargetRate { get; set; } = -1; // default to infinite rate
  public bool IsEnabled { get; set; } = true; // default to enabled
}

class FileProcessorService : IHostedService
{
  private readonly ILogger<FileProcessorService> _logger;
  private readonly IQueue<MyRecord> _queue;
  private readonly IQueue<ApiEvent> _apiEventQueue;
  private int _targetRate = -1; // default to infinite rate
  private readonly TimeSpan _processingRate = TimeSpan.FromSeconds(5);
  private readonly bool _isEnabled;



  public FileProcessorService(
      ILogger<FileProcessorService> logger,
      IQueue<MyRecord> queue,
      IQueue<ApiEvent> apiEventQueue,
      IOptions<FileProcessorServiceOptions> configuration)
  {
    _logger = logger;
    _queue = queue;
    _apiEventQueue = apiEventQueue;
    _targetRate = configuration.Value.TargetRate;
    _isEnabled = configuration.Value.IsEnabled;

    if (_isEnabled)
      _logger.LogInformation($"FileProcessorService initialized and running target througput {(_targetRate == -1 ? "Infinite" : _targetRate)} / sec.");
  }

  public async Task StartAsync(CancellationToken cancellationToken)
  {
    if (_isEnabled)
    {
      int newWaitTime = 0;
      int lastWaitTime = 0;
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


              _logger.LogInformation($"Received message: {receivedMessage.ToString() ?? "<EMPTY>"}");

              // Simulate Calling the API which will process the the message

              ApiEvent apiEvent = new ApiEvent
              {
                Id = receivedMessage.Id,
                Metadata = receivedMessage.Metadata,
                IsSuccess = true // Simulate a successful API call
              };

              // Simulate the API recording the successful processing of the message
              _apiEventQueue.EnqueueRecordAsync(apiEvent).GetAwaiter().GetResult();

              return true; // return true to indicate that the message was passed to the API for processing          

            }
          , cancellationToken);

          lastWaitTime = newWaitTime;
          newWaitTime = int.Max(0, (int)(targetTimePerOperation - stopwatch.ElapsedMilliseconds));

          if (lastWaitTime != newWaitTime) _logger.LogInformation($"Operation Delay now {newWaitTime}");

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
    }
    
    await Task.CompletedTask;
  }



  public async Task StopAsync(CancellationToken cancellationToken)
  {
    await Task.CompletedTask;
  }
}