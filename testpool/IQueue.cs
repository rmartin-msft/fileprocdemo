
namespace testpool;

using System.Security.AccessControl;
using System.Threading.Tasks;

public interface IQueue2<T>
{
    /// <summary>
    /// Enqueues a record for processing.
    /// </summary>
    /// <param name="record">The record to enqueue.</param>
    void EnqueueRecord(T record);

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
    /// Ensures that all queued records are commited to the queue
    /// </summary>
    void Flush();
  
    /// <summary>
    /// The topic to use for send messageds to the queue
    /// </summary>
    public string Topic { get; set; }
}