
namespace testpool;

using System.Security.AccessControl;
using System.Threading.Tasks;

public interface IQueue2<T>
{
    /// <summary>
    /// Asynchronously enqueues a record for processing.
    /// </summary>
    /// <param name="record">The record to enqueue.</param>
    Task EnqueueRecordAsync(T record);

    /// <summary>
    /// Asynchronously Dequeues a record for processing.
    /// </summary>
    /// <returns></returns>
    Task<T> DequeueRecordAsync();

    /// <summary>
    /// Asynchronously Dequeues a record for processing with an optional callback to process the record when received.
    /// If the callback returns false, the record will not be processed and will be returned to the queue.
    /// </summary>
    /// <param name="onRecordReceived"></param>
    /// <returns></returns>
    Task<T> DequeueRecordAsync(Func<T, bool>? onRecordReceived = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures that all queued records are commited to the queue
    /// </summary>
    void Flush();
  
    /// <summary>
    /// The topic to use for send messageds to the queue
    /// </summary>
    public string Topic { get; set; }
}