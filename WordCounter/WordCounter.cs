using System.Collections.Concurrent;
using System.Reflection.Metadata.Ecma335;
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

    public enum QueueType
    {
        ConcurrentQueue,
    }

    public class WordCounter(QueueType queueType = QueueType.ConcurrentQueue, int numWorkers = 8, int chunkSize = 4096, int longestWord = 100, string logFile = "log.txt")
    {
        // TODO: preanalyze hardware / system / file size distribution
        // todo: fixed length jobqueue?, the maxsize of the queue should be limited to prevent memory overload
        // TODO: check whether performance for TPLDataflow and Channels are greater. (TPL should have higher throughput, but Channels faster init) they also both have capacity
        // DONE: make sure that enqueue (both filename and content) are pointers to those values. capacity of jobQueue should hence be Job (JobType, *,*)
        
        private readonly ConcurrentDictionary<string, int> _wordCount = new ConcurrentDictionary<string, int>();
        private readonly ConcurrentQueue<Job> _jobQueue = queueType switch
        {
            QueueType.ConcurrentQueue => new ConcurrentQueue<Job>(),
            // _ => new TPLDataflow(), // Implement other queue 
            _ => throw new ArgumentException("Unsupported queue type")
        };

        // Lock for filesProcessed
        private readonly object _filesProcessedLock = new object();
        private int _totalFiles = 0; 
        private int _filesProcessed = 0;
        // Error variable returning that there was an error instead of crashing the program.
        private bool _errorOccured = false;

        /// <summary>
        /// Asynchronously processes files by reading them in chunks and enqueuing the content into a job queue.
        /// Workers then read the enqueued content chunks from the queue and process them further.
        /// </summary>
        /// <param name="fileNames">An array of file names to be processed.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ProcessFilesAsync(IEnumerable<string> fileNames)
        {
            var enumerable = fileNames as string[] ?? fileNames.ToArray();
            this._totalFiles = enumerable.Count();
            // Enqueue file read jobs into the job queue
            foreach (var fileName in enumerable)
            {
                if (File.Exists(fileName))
                {
                    _jobQueue.Enqueue(new Job { Type = JobType.FileRead, FileName = fileName });
                }
                else
                {
                    _errorOccured = true;
                    LogError($"File '{fileName}' does not exist.");
                }
            }

            if (!_errorOccured)
            {
                // Spawn worker tasks
                List<Task> workerTasks = new List<Task>();
                for (int i = 0; i < numWorkers; i++)
                {
                    workerTasks.Add(WorkerAsync());
                }

                // Wait for all worker tasks to complete
                await Task.WhenAll(workerTasks);
            }

            // All workers have completed processing
            Console.WriteLine(_errorOccured
                ? $"Word counting completed with errors, see {logFile} for specifics."
                : "Word counting completed without errors:");
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
                lock (this._filesProcessedLock)
                {
                    // Console.WriteLine($"Processed {this.filesProcessed}" );
                    // Console.WriteLine(this.totalFiles);
                    if (!_jobQueue.TryDequeue(out job) && this._filesProcessed >= this._totalFiles)
                    {
                        break;
                    }
                }
                
                var buffer = new Char[chunkSize+longestWord];

                switch (job?.Type)
                {
                    case JobType.FileRead:
                        if (job.FileName == null) {
                            // DONE log error and continue to next file
                            LogError("File name is null");
                            // treat a failed filename as a processed file
                            lock (this._filesProcessedLock)
                            {
                                this._filesProcessed++;
                            }
                            break;
                        } else {
                            
                            using StreamReader sr = new StreamReader(job.FileName);
                            await ProcessFileAsync(job.FileName,buffer,sr);
                            // update the filesProcessed class var 
                            lock (this._filesProcessedLock) {
                                this._filesProcessed++;
                            }
                            break;
                        }
                    case JobType.WordProcess:
                        if (job.Content == null) {
                            // DONE log error and continue to next file
                            LogError("Content is null");
                            break;
                        } else {
                            ProcessWords(job.Content);
                            break;
                        }
                }
            }
        }

        /// <summary>
        /// Asynchronously reads a file in chunks and enqueues the file content for word processing.
        /// </summary>
        /// <param name="fileName">The name of the file to be processed.</param>
        /// <param name="buffer">Char[] buffer to store bytes read from StreamReader sr</param>
        /// <param name="sr">StreamReader, reads _contentSize into buffer</param>
        /// <returns>A task representing the asynchronous operation of processing the file.</returns>
        private async Task ProcessFileAsync(string fileName,Char[] buffer,StreamReader sr)
        {
            try
            {
                // Console.WriteLine($"Basestream {sr.BaseStream.Length}");
                int bytesRead;

                string trailingWord = "";
                while (true )
                {
                    bytesRead = await sr.ReadBlockAsync(buffer, 0, chunkSize);
                    if (bytesRead == 0)
                    {
                        _jobQueue.Enqueue(new Job { Type = JobType.WordProcess, Content = trailingWord });
                        break;
                    }
                    // Convert the read bytes to string (assuming UTF-8 encoding)
                    string contentChunk = new string(buffer, 0, bytesRead);

                    // Append the trailing word from the previous iteration
                    contentChunk = trailingWord + contentChunk;

                    (trailingWord,contentChunk) = CutWord(contentChunk);
                    // Enqueue word processing job with processed content
                    _jobQueue.Enqueue(new Job { Type = JobType.WordProcess, Content = contentChunk });
                }
            }
            catch (Exception ex)
            {
                LogError($"Error reading file '{fileName}': {ex.Message}");
                // Console.WriteLine($"Error reading file '{fileName}': {ex.Message}");
            }
        }

        /// <summary>
        /// Processes a string of text by splitting it into words, removing non-alphanumeric characters,
        /// converting to lowercase, and updating the word count in a global concurrent dictionary.
        /// </summary>
        /// <param name="content">The input text content to be processed.</param>
        /// <returns>void</returns>
        private void ProcessWords(string content)
        {
            string[] words = Regex.Split(content, @"[\W_]+")
                .Where(word => !string.IsNullOrEmpty(word))
                .ToArray();

            // Create a local dictionary to accumulate word counts
            Dictionary<string, int> localWordCount = new Dictionary<string, int>();

            // Update word count in the local dictionary
            foreach (string word in words)
            {
                if (!localWordCount.TryAdd(word, 1))
                {
                    localWordCount[word]++;
                }
            }

            // Merge local word counts into the global word count dictionary
            // DONE: lock for the whole loop to only do 1 lock/unlock, then do an async task to allow thread to pool if blocked
            lock (_wordCount)
            {
                // Update the global word count dictionary
                foreach (var entry in localWordCount)
                {
                    _wordCount.AddOrUpdate(entry.Key, entry.Value, (key, oldValue) => oldValue + entry.Value);
                }
            }
        }

        /// <summary>
        /// Prints the word counts stored in the global dictionary to the console, sorted by word (key).
        /// Format: "Count: Word", eg. "2: hello"
        /// </summary>
        private void PrintWordCounts()
        {
            //foreach (var pair in wordCount.OrderBy(pair => pair.Key))
            
            foreach (var pair in _wordCount.OrderByDescending(pair => pair.Value))
            {
                Console.WriteLine($"{pair.Value}: {pair.Key}");
            }
        }
        /// <summary>
        /// Gets the concurrent dictionary for purpose of testing or further processing.
        /// </summary>
        public ConcurrentDictionary<string, int> GetWordCounts()
        {
            return this._wordCount; // Return the wordCount dictionary
        }

        public (string,string) CutWord(string contentChunk) {
            string trailingWord = "";
            // Find the last space in the current chunk
            int lastSpaceIndex = contentChunk.LastIndexOf(' ');
            // space is the last byte/char
            if (lastSpaceIndex+1 == contentChunk.Length)
            {
            }
            else if (lastSpaceIndex != -1)
            {
                // Extract the trailing word
                trailingWord = contentChunk.Substring(lastSpaceIndex + 1);
                // Truncate the content chunk at the last space (exclusive)
                contentChunk = contentChunk.Substring(0, lastSpaceIndex);
            }
            else
            {
                // No spaces found, the whole chunk is a single word
                trailingWord = contentChunk;
                contentChunk = "";
            }
            return (trailingWord , contentChunk) ;
        }
        private void LogError(string message)
        {
            this._errorOccured = true;
            try
            {
                using (StreamWriter sw = File.AppendText(logFile))
                {
                    sw.WriteLine($"[{DateTime.Now}] ERROR: {message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing to log file: {ex.Message}");
            }
        }
    }
}
