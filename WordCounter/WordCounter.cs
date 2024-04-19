using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WordCounter
{

    public enum JobType
    {
        FileRead,
        WordProcess
    }

    public class Job
    {
        public JobType Type { get; set; }
        public string? FileName { get; set; }
        public string? Content { get; set; }
    }

    public class WordCounter
    {
        private readonly ConcurrentDictionary<string, int> wordCount = new ConcurrentDictionary<string, int>();
        private readonly ConcurrentQueue<Job> jobQueue = new ConcurrentQueue<Job>();
        private readonly int numWorkers = 8;

        public async Task ProcessFilesAsync(IEnumerable<string> fileNames)
        {
            // Enqueue file read jobs into the job queue
            foreach (var fileName in fileNames)
            {
                jobQueue.Enqueue(new Job { Type = JobType.FileRead, FileName = fileName });
            }

            // Spawn worker tasks
            List<Task> workerTasks = new List<Task>();
            for (int i = 0; i < numWorkers; i++)
            {
                workerTasks.Add(WorkerAsync());
            }

            // Wait for all worker tasks to complete
            await Task.WhenAll(workerTasks);

            // All workers have completed processing
            Console.WriteLine("Word counting completed:");
            PrintWordCounts();
        }

        private async Task WorkerAsync()
        {
            while (true)
            {
                // Dequeue a job from the job queue
                if (!jobQueue.TryDequeue(out Job job))
                {
                    break; // Exit worker loop if no more jobs
                }

                switch (job.Type)
                {
                    case JobType.FileRead:
                        await ProcessFileAsync(job.FileName);
                        break;
                    case JobType.WordProcess:
                        ProcessWords(job.Content);
                        break;
                }
            }
        }

        // TODO enqueue chunks of file instaed of the whole file
        private async Task ProcessFileAsync(string fileName)
        {
            try
            {
                // Read file content asynchronously
                string content = await ReadFileContentAsync(fileName);

                // Enqueue word processing job with file content
                jobQueue.Enqueue(new Job { Type = JobType.WordProcess, Content = content });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading file '{fileName}': {ex.Message}");
            }
        }

        static private async Task<string> ReadFileContentAsync(string fileName)
        {
            using var reader = File.OpenText(fileName);
            return await reader.ReadToEndAsync();
        }

        private void ProcessWords(string content)
        {
            // Split text into words by removing non-alphanumeric characters and converting to lowercase
            string[] words = Regex.Split(content, @"\W+")
                                  .Where(word => !string.IsNullOrEmpty(word))
                                  .Select(word => word.ToLower())
                                  .ToArray();

            // Update word count dictionary
            foreach (string word in words)
            {
                wordCount.AddOrUpdate(word, 1, (key, oldValue) => oldValue + 1);
            }
        }

        public void PrintWordCounts()
        {
            foreach (var pair in wordCount.OrderBy(pair => pair.Key))
            {
                Console.WriteLine($"{pair.Value}: {pair.Key}");
            }
        }

        public ConcurrentDictionary<string, int> GetWordCounts()
        {
            return this.wordCount; // Return the wordCount dictionary
        }
    }
}
