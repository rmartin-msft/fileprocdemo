
namespace testpool;

using System.Net;
using System.Threading.Tasks;


public class FileJobStorage : IFileJobStorageRepository
{
  private Dictionary<Guid, JobStatus> _jobData = new Dictionary<Guid, JobStatus>();

	public Task<HttpStatusCode> WriteJobToStorageAsync(JobId jobId, int numberOfRecords, string metadata)
  {    
    {
      // Create a new job status entry
      var newJobStatus = new JobStatus
      {
        Id = jobId,
        RecordsProcessed = 0,
        TotalRecords = numberOfRecords,
        RecordErrors = 0,
        IsComplete = false // Initially not complete
      };
      _jobData[jobId.Value] = newJobStatus;
      return Task.FromResult(HttpStatusCode.Created);
    }    
  }

	public Task<HttpStatusCode> UpdateJobStatusAsync(JobId jobId, int status)
	{
    if (_jobData.TryGetValue(jobId.Value, out var jobStatus))
    {
      jobStatus.RecordsProcessed += status;
      jobStatus.IsComplete = (jobStatus.RecordErrors + jobStatus.RecordsProcessed == jobStatus.TotalRecords);
      return Task.FromResult(HttpStatusCode.OK);
    }
    else
    {
      return Task.FromResult(HttpStatusCode.NotFound);
    } 
	}

	public async Task<JobStatus> GetJobStatusAsync(JobId jobId)
	{
    if (_jobData.TryGetValue(jobId.Value, out var jobStatus))
    {      
      jobStatus.IsComplete = (jobStatus.RecordErrors + jobStatus.RecordsProcessed == jobStatus.TotalRecords);

      await Task.Delay(100); // Simulate some async operation
        
      return jobStatus;
    }
    else
    {
      throw new KeyNotFoundException($"Job with ID {jobId.Value} not found.");
    }
	}
}