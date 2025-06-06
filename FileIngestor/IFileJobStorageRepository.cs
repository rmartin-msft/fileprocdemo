
namespace testpool;

using System.Data.Common;
using System.Net;
using System.Threading.Tasks;

public class JobId 
{
  public static JobId Create(string? metadata)
  {
    return new JobId
    {
      Metadata = metadata ?? string.Empty,
      Value = Guid.NewGuid()
    };  
  }

  public string? Metadata { get; set; }
  public Guid Value { get; set; }
}

public class JobStatus
{
  public required JobId Id { get; set; }
  public int RecordsProcessed { get; set; }
  public int RecordErrors { get; set; }
  public int TotalRecords { get; set; }
  public bool IsComplete { get; set; }
}
public interface IFileJobStorageRepository
{
  /// <summary>
  /// Adds a record to the storage.
  /// </summary>
  /// <param name="record">The record to add.</param>
  Task<HttpStatusCode> WriteJobToStorageAsync(JobId Id, int numberOfRecords, string metadata);

  Task<HttpStatusCode> UpdateJobStatusAsync(JobId Id, int newRecordsProcessed);

  Task<JobStatus> GetJobStatusAsync(JobId Id);

}