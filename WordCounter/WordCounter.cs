using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;

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
        // TODO: preanalyze hardware / system / file size distribution
        readonly int CHUNKSIZE = 1024 * 1024;
        private readonly ConcurrentDictionary<string, int> wordCount = new ConcurrentDictionary<string, int>();
        private readonly ConcurrentQueue<Job> jobQueue = new ConcurrentQueue<Job>();
        private readonly int numWorkers = 8;

        // Lock for filesProcessed
        private readonly object filesProcessedLock = new object();
        private int totalFiles = 0; 
        private int filesProcessed = 0;

        /// <summary>
        /// Asynchronously processes files by reading them in chunks and enqueuing the content into a job queue.
        /// Workers then read the enqueued content chunks from the queue and process them further.
        /// </summary>
        /// <param name="fileNames">An array of file names to be processed.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ProcessFilesAsync(IEnumerable<string> fileNames)
        {
            this.totalFiles = fileNames.Count();
            // Console.WriteLine(this.totalFiles);

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

        /// <summary>
        /// Asynchronously dequeues jobs from the job queue and processes them based on the job type.
        /// </summary>
        /// <remarks>
        /// This method continuously dequeues jobs from the job queue and executes specific actions based on the job type.
        /// Supported job types include reading files (JobType.FileRead) and processing file content (JobType.WordProcess).
        /// </remarks>
        /// <returns>A task representing the asynchronous operation of the worker.</returns>
        private async Task WorkerAsync()
        {
            while (true)
            {
                // Dequeue a job from the job queue
                // DONE: make sure that workers continue even while queue is empty if all files havent been read yet. Simple count and decrement when done?
                Job? job;
                lock (this.filesProcessedLock)
                {
                    // Console.WriteLine($"Processed {this.filesProcessed}" );
                    // Console.WriteLine(this.totalFiles);
                    if (!jobQueue.TryDequeue(out job) && this.filesProcessed >= this.totalFiles)
                    {
                        break;
                    }
                }

                switch (job?.Type)
                {
                    case JobType.FileRead:
                        if (job.FileName == null) {
                            // TODO log error and continue to next file
                            break;
                        } else {
                            await ProcessFileAsync(job.FileName);
                            // update the filesProcessed class var 
                            lock (this.filesProcessedLock) {
                                this.filesProcessed++;
                            }
                            break;
                        }
                    case JobType.WordProcess:
                        if (job.Content == null) {
                            // TODO log error and continue to next file
                            break;
                        } else {
                            await ProcessWords(job.Content);
                            break;
                        }
                }
            }
        }

        /// <summary>
        /// Asynchronously reads a file in chunks and enqueues the file content for word processing.
        /// </summary>
        /// <param name="fileName">The name of the file to be processed.</param>
        /// <returns>A task representing the asynchronous operation of processing the file.</returns>
        private async Task ProcessFileAsync(string fileName)
        {
            try
            {
                // Open the file for reading
                // DONE enqueue chunks of file instaed of the whole file. So it needs to read x amount of bytes, enqueue that content, then continue to next chunk of the file
                using FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
                byte[] buffer = new byte[CHUNKSIZE];
                int bytesRead;

                // Read the file in chunks until the end is reached
                // TODO check that it splits on space such that words dont get split
                while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    // Convert the read bytes to string (assuming UTF-8 encoding)
                    string contentChunk = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    // Enqueue word processing job with file content chunk
                    jobQueue.Enqueue(new Job { Type = JobType.WordProcess, Content = contentChunk });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading file '{fileName}': {ex.Message}");
            }
        }

        /// <summary>
        /// Processes a string of text by splitting it into words, removing non-alphanumeric characters,
        /// converting to lowercase, and updating the word count in a global concurrent dictionary.
        /// </summary>
        /// <param name="content">The input text content to be processed.</param>
        /// <returns>void</returns>
        private async Task ProcessWords(string content)
        {
            string[] words = Regex.Split(content, @"\W+")
                                  .Where(word => !string.IsNullOrEmpty(word))
                                //   .Select(word => word.ToLower())
                                  .ToArray();

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
            // TODO: lock for the whole loop to only do 1 lock/unlock, then do an async task to allow thread to pool if blocked
            foreach (var entry in localWordCount)
            {
                wordCount.AddOrUpdate(entry.Key, entry.Value, (key, oldValue) => oldValue + entry.Value);
            }
        }

        /// <summary>
        /// Prints the word counts stored in the global dictionary to the console, sorted by word (key).
        /// Format: "Count: Word", eg. "2: hello"
        /// </summary>
        public void PrintWordCounts()
        {
            // TODO: dont orderby?
            //foreach (var pair in wordCount.OrderBy(pair => pair.Key))
            
            foreach (var pair in wordCount.OrderByDescending(pair => pair.Value))
            {
                Console.WriteLine($"{pair.Value}: {pair.Key}");
            }
        }
        /// <summary>
        /// Gets the concurrent dictionary for purpose of testing or further processing.
        /// </summary>
        public ConcurrentDictionary<string, int> GetWordCounts()
        {
            return this.wordCount; // Return the wordCount dictionary
        }
    }
}
