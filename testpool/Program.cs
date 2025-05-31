namespace testpool;

using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;


class Program
{
    static IFileJobStorageRepository _fileJobStorageRepository = new FileJobStorage(); 

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

        return headers[0].Trim() == "Id" &&
               headers[1].Trim() == "First name" &&
               headers[2].Trim() == "Last name" &&
               headers[3].Trim() == "Full name" &&
               headers[4].Trim() == "Language" &&
               headers[5].Trim() == "Gender";
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

    static async Task Main(string[] args)
    {
        string fileName = "c:/scratch/TestData.csv";

        StreamReader? streamReader = null;
        try
        {
            IEnqueue recordQueue = new QueueManager(); // Assuming you have an implementation of IEnqueueRecord

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
                    await recordQueue.EnqueueRecordAsync(record); // Enqueue the record for processing                    
                }
            }

            recordQueue.Flush(); // Ensure all records are committed to the queue

            await _fileJobStorageRepository.WriteJobToStorageAsync(job, recordsInJob, fileName); // Update job status after processing all records

            var jobInfo = await _fileJobStorageRepository.GetJobStatusAsync(job);

            Console.WriteLine($"Job ID: {jobInfo.Id.Value}");
            Console.WriteLine($"Records Processed: {jobInfo.RecordsProcessed}");
            Console.WriteLine($"Total Records: {jobInfo.TotalRecords}");
            Console.WriteLine($"Is Complete: {jobInfo.IsComplete}");    
            Console.WriteLine($"Record Errors: {jobInfo.RecordErrors}");
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
    }
}
