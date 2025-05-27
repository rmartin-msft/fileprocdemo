namespace testpool;

using System.Buffers;
using System.ComponentModel.DataAnnotations;

class Program
{
    static void Main(string[] args)
    {

        var records = new System.Collections.Generic.List<DataClass>();
        for (int i = 0; i < 102; i++)
        {
            var data = new DataClass();
            data.MyProperty = i;
            records.Add(data);
        }

        // work out how many groups of 10 we need

        int recordsLeft = records.Count;
        int batchSize = 10;

        while (recordsLeft > 0)
        {
            if (recordsLeft < batchSize)
            {
                batchSize = recordsLeft;
            }
            
            Console.WriteLine($"Processing batch starting at index {records.Count - recordsLeft} to {records.Count - recordsLeft + batchSize}");

            recordsLeft -= batchSize;
        }        

        // var memoryPool = MemoryPool<DataClass>.Shared;
        // var arrayPool = ArrayPool<DataClass>.Create(4, 10);
        // var batch = arrayPool.Rent(10);
        // for (int i = 0; i < batch.Length; i++)
        // {
        //     batch[i].MyProperty = i;
        // }        
    }
}
