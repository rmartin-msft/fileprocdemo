
// using Microsoft.Identity.Client;
// using testpool;

// public interface IQueueFactory
// {
//   /// <summary>
//   /// Creates a new queue of the specified type.
//   /// </summary>
//   /// <typeparam name="T">The type of the queue to create.</typeparam>
//   /// <returns>A new instance of the specified queue type.</returns>
//   IQueue Create(Action<IQueue> configure);
// }

// public class QueueFactory : IQueueFactory
// {
//   private readonly IServiceProvider _serviceProvider;
//   public QueueFactory(IServiceProvider serviceProvider)
//   {
//       _serviceProvider = serviceProvider;
//   }
//   public IQueue Create(Action<IQueue> configure)
//   {    
//     IQueue queue = _serviceProvider.Resolve<IQueue>();
//     configure?.Invoke(queue);
        
//     return queue;
//   }
// }