namespace testpool;

using System.Threading.Tasks;
using System.Collections.Concurrent;

public class QueueManager : IEnqueue
{
  private ConcurrentBag<MyRecord> _buffer = new ConcurrentBag<MyRecord>();

  protected static async Task FlushBufferToQueueAsync(ConcurrentBag<MyRecord> buffer)
  {
    // This method can be used to flush the buffer to a queue or database.
    // For simplicity, we are not implementing it here.
    // Simulate processing the buffer
    System.Console.WriteLine("Buffer limit reached, pushing records onto queue...");

    foreach (var rec in buffer)
    {
      // Simulate processing each record
      System.Console.WriteLine($"Adding record: {rec.Id} {rec.FullName}");
    }

    await Task.Delay(100); // Simulate some processing delay

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
    System.Console.WriteLine($"Enqueued record: {record.Id} {record.FullName}");
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

      System.Console.WriteLine("Flushed all records to the queue.");
    }
  }
}