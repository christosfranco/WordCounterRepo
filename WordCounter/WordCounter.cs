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

        int CHUNKSIZE = 1024 * 1024;
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
                if (!jobQueue.TryDequeue(out Job? job))
                {
                    break; // Exit worker loop if no more jobs
                }

                switch (job.Type)
                {
                    case JobType.FileRead:
                        if (job.FileName == null) {
                            break;
                        } else {
                            await ProcessFileAsync(job.FileName);
                            break;
                        }
                    case JobType.WordProcess:
                        if (job.Content == null) {
                            break;
                        } else {
                            ProcessWords(job.Content);
                            break;
                        }
                }
            }
        }

        // DONE enqueue chunks of file instaed of the whole file. So it needs to read x amount of bytes, enqueue that content, then continue to next chunk of the file
        private async Task ProcessFileAsync(string fileName)
        {
            try
            {
                // Open the file for reading
                using (FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                {
                    byte[] buffer = new byte[CHUNKSIZE];
                    int bytesRead;

                    // Read the file in chunks until the end is reached
                    while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        // Convert the read bytes to string (assuming UTF-8 encoding)
                        // TODO: maybe directly parse the bytes instead of converting to string first?
                        string contentChunk = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                        // Enqueue word processing job with file content chunk
                        jobQueue.Enqueue(new Job { Type = JobType.WordProcess, Content = contentChunk });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading file '{fileName}': {ex.Message}");
            }
        }

        private void ProcessWords(string content)
        {
            // Split text into words by removing non-alphanumeric characters
            // converting to lowercase
            string[] words = Regex.Split(content, @"\W+")
                                  .Where(word => !string.IsNullOrEmpty(word))
                                  .Select(word => word.ToLower())
                                  .ToArray();

            // DONE: can this be optimized by making a local dictionary, then doing 1 update to global dictionary?
            // did save a bit of time
            // Update word count dictionary
            // Create a local dictionary to accumulate word counts
            Dictionary<string, int> localWordCount = new Dictionary<string, int>();

            // Update word count in the local dictionary
            foreach (string word in words)
            {
                if (localWordCount.ContainsKey(word))
                {
                    localWordCount[word]++;
                }
                else
                {
                    localWordCount[word] = 1;
                }
            }

            // Merge local word counts into the global word count dictionary
            foreach (var entry in localWordCount)
            {
                wordCount.AddOrUpdate(entry.Key, entry.Value, (key, oldValue) => oldValue + entry.Value);
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
