using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.Threading.Channels;

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
        Channels,
    }

    public interface IJobQueue<T>
    {
        void Enqueue(T item);
        bool TryDequeue(out T item);
        Task<T> DequeueAsync();
    }
    
    public class ChannelJobQueue<T> : IJobQueue<T>
    {
        private readonly Channel<T> _channel;

        public ChannelJobQueue(BoundedChannelOptions options)
        {
            _channel = Channel.CreateBounded<T>(options);
            
        }

        public void Enqueue(T item)
        {
            _channel.Writer.TryWrite(item);
        }
        public bool TryDequeue(out T job)
        {
            if (_channel.Reader.TryRead(out job))
            {
                return true; // Return the successfully read job
            }
            else
            {
                return false; // Return failure to read job
            }
        }
        public async Task<T> DequeueAsync()
        {
            while (await _channel.Reader.WaitToReadAsync())
            {
                if (_channel.Reader.TryRead(out T item))
                    return item;
            }
            throw new InvalidOperationException("No items to dequeue.");
        }
    }

    public class ConcurrentQueueJobQueue<T> : IJobQueue<T>
    {
        private readonly ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();

        public void Enqueue(T item)
        {
            _queue.Enqueue(item);
        }
        public bool TryDequeue(out T job)
        {
            // T job;
            if (_queue.TryDequeue(out job))
            {
                return true; // Return the dequeued job
            }
            else
            {
                return false;
            }
        }
        public Task<T> DequeueAsync()
        {
            if (_queue.TryDequeue(out T item))
                return Task.FromResult(item);
            throw new InvalidOperationException("No items to dequeue.");
        }
    }

    public class WordCounter(QueueType queueType = QueueType.Channels, int numWorkers = 8, int chunkSize = 4096, int longestWord = 100, string logFile = "log.txt")
    {
        private readonly ConcurrentDictionary<string, int> _wordCount = new ConcurrentDictionary<string, int>();
        
        private static readonly BoundedChannelOptions Options = new BoundedChannelOptions(capacity:976562)
        {
            FullMode = BoundedChannelFullMode.Wait 
        };

        private readonly IJobQueue<Job> _jobQueue = queueType switch
        {
            QueueType.Channels => new ChannelJobQueue<Job>(Options),
            QueueType.ConcurrentQueue => new ConcurrentQueueJobQueue<Job>(),
            _ => throw new ArgumentException("Unsupported queue type")
        } ;

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
                            
            var buffer = new Char[chunkSize+longestWord];
            // Create a local dictionary to accumulate word counts
            Dictionary<string, int> localWordCount = new Dictionary<string, int>();

            while (true)
            {
                // workers continue even while queue is empty if all files havent been read yet.
                Job? job;
                lock (this._filesProcessedLock)
                {
                    if (!_jobQueue.TryDequeue(out job) && this._filesProcessed >= this._totalFiles)
                    {
                        break;
                    }
                }

                switch (job?.Type)
                {
                    case JobType.FileRead:
                        if (job.FileName == null) {
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
                            LogError("Content is null");
                            break;
                        } else {
                            ProcessWords(job.Content,localWordCount);
                            break;
                        }
                }
            }
            // Merge local word counts into the global word count dictionary
            lock (_wordCount)
            {
                foreach (var entry in localWordCount)
                {
                    _wordCount.AddOrUpdate(entry.Key, entry.Value, (key, oldValue) => oldValue + entry.Value);
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
        private void ProcessWords(string content,Dictionary<string, int> localWordCount )
        {
            string[] words = Regex.Split(content, @"[\W_]+")
                .Where(word => !string.IsNullOrEmpty(word))
                .ToArray();


            // Update word count in the local dictionary
            foreach (string word in words)
            {
                if (!localWordCount.TryAdd(word, 1))
                {
                    localWordCount[word]++;
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
            //
            // foreach (var pair in _wordCount.OrderByDescending(pair => pair.Value))
            // {
            //     Console.WriteLine($"{pair.Value}: {pair.Key}");
            // }
            
            foreach (var pair in _wordCount)
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
