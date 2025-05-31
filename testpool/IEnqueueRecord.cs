
namespace testpool;
using System.Threading.Tasks;

public interface IEnqueue
{
  /// <summary>
  /// Enqueues a record for processing.
  /// </summary>
  /// <param name="record">The record to enqueue.</param>
  void EnqueueRecord(MyRecord record);

  /// <summary>
  /// Asynchronously enqueues a record for processing.
  /// </summary>
  /// <param name="record">The record to enqueue.</param>
  Task EnqueueRecordAsync(MyRecord record);

  /// <summary>
  /// Ensures that all queued records are commited to the queue
  /// </summary>
  void Flush();
}