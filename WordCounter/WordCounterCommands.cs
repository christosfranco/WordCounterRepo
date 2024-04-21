namespace WordCounter;
using CommandDotNet;

public class WordCounterCommands
{

    [DefaultCommand]
    public async Task<int> ProcessFiles(
        [Operand(Description = "File names to be processed")] List<string> fileNames,
        [Option(Description = "Type of job queue (ConcurrentQueue or OtherQueueType)")] QueueType queueType = QueueType.ConcurrentQueue,
        [Option(Description = "Number of worker tasks")] int numWorkers = 8,
        [Option(Description = "Chunk size for file processing")] int chunkSize = 4096,
        [Option(Description = "Maximum length of a word")] int longestWord = 50,
        [Option(Description = "Log file path")] string logFile = "log.txt"
    )
    {
        if (fileNames == null || fileNames.Count == 0)
        {
            Console.WriteLine("No file names specified.");
            return 1; // Return error code
        }

        var wordCounter = new WordCounter(queueType, numWorkers, chunkSize, longestWord, logFile);
        await wordCounter.ProcessFilesAsync(fileNames);

        return 0; // Success
    }
}
