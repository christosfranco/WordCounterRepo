public class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: WordCounter <file1> <file2> ...");
            return;
        }

        var fileNames = args;
        // DONE: log.txt default, add optional for multiple log file specification
        // DONE give input which queue type, workers, chunksize
        var wordCounter = new WordCounter.WordCounter(numWorkers: 8, longestWord: 50);
        await wordCounter.ProcessFilesAsync(fileNames);
    }
}
