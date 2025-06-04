namespace testpool;

using Microsoft.Extensions.Hosting;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

class FileIngestorService : IHostedService
{
    private readonly IFileJobStorageRepository _fileJobStorageRepository;
    private readonly ILogger<FileIngestorService> _logger;
    private readonly IQueue2<MyRecord> _queue;
    private readonly IConfiguration _configuration;

    public FileIngestorService(
        IFileJobStorageRepository fileJobStorageRepository,
        ILogger<FileIngestorService> logger,
        /* [FromKeyedServices("file")] */ IQueue2<MyRecord> queue,
        IConfiguration configuration)
    {
        _logger = logger;
        _queue = queue;
        _fileJobStorageRepository = fileJobStorageRepository;
        _configuration = configuration;
            

        _logger.LogInformation("FileIngestorService initialized.");
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        string fileName = "c:/scratch/TestData.csv";

        StreamReader? streamReader = null;
        try
        {
            streamReader = new StreamReader(fileName);
            string? line;

            line = await streamReader.ReadLineAsync();

            if (!await VerifyHeadersAsync(line))
            {
                Console.Error.WriteLine("Invalid file format.");
                return;
            }

            JobId job = JobId.Create("Test job metadata");
            await _fileJobStorageRepository.WriteJobToStorageAsync(job, -1, fileName);

            int recordsInJob = 0;
            while ((line = await streamReader.ReadLineAsync()) != null)
            {
                MyRecord? record = await ParseLineAsync(line);

                if (record != null)
                {
                    recordsInJob++;
                    await _queue.EnqueueRecordAsync(record); // Enqueue the record for processing                    
                }
            }

            _queue.Flush(); // Ensure all records are committed to the queue

            await _fileJobStorageRepository.WriteJobToStorageAsync(job, recordsInJob, fileName); // Update job status after processing all records

            var jobInfo = await _fileJobStorageRepository.GetJobStatusAsync(job);

            _logger.LogInformation($"Job Information: Job ID: {jobInfo.Id.Value} Records Processed: {jobInfo.RecordsProcessed} Total Records: {jobInfo.TotalRecords} Is Complete: {jobInfo.IsComplete} Record Errors: {jobInfo.RecordErrors}");
        }
        catch (FileNotFoundException ex)
        {
            Console.WriteLine($"File not found: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
        finally
        {
            streamReader?.Dispose();
        }

        await Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("FileIngestorService Stopped.");
        // Implementation for stopping the file ingestion service
        await Task.CompletedTask;
    }
    
    static bool VerifyHeaders(string? line)
    {
        if (line == null)
        {
            return false;
        }

        string[] headers = line.Split(',');

        if (headers.Length != 6)
        {
            return false;
        }

        return headers[0].Trim().ToLower() == "id" &&
               headers[1].Trim().ToLower() == "first name" &&
               headers[2].Trim().ToLower() == "last name" &&
               headers[3].Trim().ToLower() == "full name" &&
               headers[4].Trim().ToLower() == "language" &&
               headers[5].Trim().ToLower() == "gender";
    }
    static async Task<bool> VerifyHeadersAsync(string? line)
    {
        bool result = false;

        await Task.Run(() => result = VerifyHeaders(line));

        return result;
    }

    static async Task<MyRecord?> ParseLineAsync(string line)
    {
        MyRecord? record = null;

        await Task.Run(() =>
        {                     
            string[] parts = line.Split(',');

            if (parts.Length != 6)
            {
                throw new FormatException("Invalid number of fields in line.");
            }

            try
            {
                record = new MyRecord()
                {
                    Id = int.Parse(parts[0].Trim()),
                    FirstName = parts[1].Trim(),
                    LastName = parts[2].Trim(),
                    FullName = parts[3].Trim(),
                    Language = parts[4].Trim(),
                    Gender = parts[5].Trim()
                };
            }
            catch (FormatException)
            {
                throw;
            }
            catch (OverflowException)
            {
                throw;
            }
            catch (Exception)
            {
                throw;
            }        
        });

        return record;
        
    }
}

