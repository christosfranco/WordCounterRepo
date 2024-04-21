using System.Collections.Concurrent;
using System.Diagnostics;
using NUnit.Framework.Internal;

namespace TestWordCounter.StressTests;


[TestFixture]
public class StressTest1
{

        private Dictionary<string, int> _wordFrequencies;
        private string[] _filePaths;
        private int _numFiles;

        // public async Task<ConcurrentDictionary<string,int>> TimeTaker(int workers)
        // {
        //     return wordCounts;
        // }
        
        [SetUp]
        public void Setup()
        {
            
            // Arrange
            _numFiles = 4; // Number of large files

            _wordFrequencies = new Dictionary<string, int>
            {
                { "Lorem", 3_000_000 },
                { "ipsum", 5_000_000 },
                { "dolor", 2_000_000 },
                { "sit", 4_000_000 },
                { "amet", 3_000_000 },
                { "consectetur", 2_000_000 }
            };

            // Create large text files
            _filePaths = IOHelpers.CreateLargeTextFiles(_numFiles, _wordFrequencies);

        }
        
        // 1.600 GB
        [Test]
        [Ignore("Skipping stresstest, Memory overload ")]
        public async Task ProcessFilesAsync_CountsWordsCorrectly_200Mil_Words()
        {
            // Arrange
            int numFiles = 1; // Number of large files

            var wordFrequencies = new Dictionary<string, int>
                {
                    { "Lorem", 30_000_000 },
                    { "ipsum", 50_000_000 },
                    { "dolor", 20_000_000 },
                    { "sit", 40_000_000 },
                    { "amet", 30_000_000 },
                    { "consectetur", 20_000_000 }
                };

            // Create large text files
            string[] filePaths = IOHelpers.CreateLargeTextFiles(numFiles, wordFrequencies);

            try
            {
                // Create an instance of WordCounter
                var wordCounter = new WordCounter.WordCounter();

                // Actt Process large files asynchronously
                await wordCounter.ProcessFilesAsync(filePaths);

                // Assert Check word counts
                var wordCounts = wordCounter.GetWordCounts();
                Assert.Multiple(() =>
                {
                    Assert.That(wordCounts["Lorem"], Is.EqualTo(30_000_000 * numFiles));
                    Assert.That(wordCounts["ipsum"], Is.EqualTo(50_000_000 * numFiles));
                    Assert.That(wordCounts["dolor"], Is.EqualTo(20_000_000 * numFiles));
                    Assert.That(wordCounts["sit"], Is.EqualTo(40_000_000 * numFiles));
                    Assert.That(wordCounts["amet"], Is.EqualTo(30_000_000 * numFiles));
                    Assert.That(wordCounts["consectetur"], Is.EqualTo(20_000_000 * numFiles));
                });
            }
            finally
            {
                // cleanup dlete temporary files after testing
                foreach (var filePath in filePaths)
                {
                    try
                    {
                        File.Delete(filePath);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to delete file '{filePath}': {ex.Message}");
                    }
                }
            }
            
            
        }

        // ~0.160 GB
        [Test]
        // [Ignore("stresstest")]
        public async Task ProcessFilesAsync_CountsWordsCorrectly_1_File_20MilWords()
        {
            // Arrange
            int numFiles = 1; // Number of large files

            var wordFrequencies = new Dictionary<string, int>
            {
                { "Lorem", 3_000_000 },
                { "ipsum", 5_000_000 },
                { "dolor", 2_000_000 },
                { "sit", 4_000_000 },
                { "amet", 3_000_000 },
                { "consectetur", 2_000_000 }
            };

            // Create large text files
            string[] filePaths = IOHelpers.CreateLargeTextFiles(numFiles, wordFrequencies);

            try
            {
                // Create an instance of WordCounter
                var wordCounter = new WordCounter.WordCounter();

                // Actt Process large files asynchronously
                await wordCounter.ProcessFilesAsync(filePaths);

                // Assert Check word counts
                var wordCounts = wordCounter.GetWordCounts();
                Assert.Multiple(() =>
                {
                    Assert.That(wordCounts["Lorem"], Is.EqualTo(3_000_000 * numFiles));
                    Assert.That(wordCounts["ipsum"], Is.EqualTo(5_000_000 * numFiles));
                    Assert.That(wordCounts["dolor"], Is.EqualTo(2_000_000 * numFiles));
                    Assert.That(wordCounts["sit"], Is.EqualTo(4_000_000 * numFiles));
                    Assert.That(wordCounts["amet"], Is.EqualTo(3_000_000 * numFiles));
                    Assert.That(wordCounts["consectetur"], Is.EqualTo(2_000_000 * numFiles));
                });
            }
            finally
            {
                // cleanup dlete temporary files after testing
                foreach (var filePath in filePaths)
                {
                    try
                    {
                        File.Delete(filePath);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to delete file '{filePath}': {ex.Message}");
                    }
                }
            }
        }
        
        
        // ~0.160*4 GB
        [Test]
        // [Ignore("stresstest")]
        public async Task ProcessFilesAsync_CountsWordsCorrectly_4_File_20MilWords_1Worker()
        {
            
            // Start the timer
            Stopwatch stopwatch = Stopwatch.StartNew();
            // Create an instance of WordCounter
            var wordCounter = new WordCounter.WordCounter(numWorkers:1);
            // Actt Process large files asynchronously
            await wordCounter.ProcessFilesAsync(_filePaths);
            // Stop the timer
            stopwatch.Stop();
            // Get the elapsed time in milliseconds
            long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;

            Console.WriteLine($"ProcessFilesAsync execution time: {elapsedMilliseconds} ms");

            // Assert Check word counts
            var wordCounts = wordCounter.GetWordCounts();
            
            Assert.Multiple(() =>
            {
                Assert.That(wordCounts["Lorem"], Is.EqualTo(3_000_000 * _numFiles));
                Assert.That(wordCounts["ipsum"], Is.EqualTo(5_000_000 * _numFiles));
                Assert.That(wordCounts["dolor"], Is.EqualTo(2_000_000 * _numFiles));
                Assert.That(wordCounts["sit"], Is.EqualTo(4_000_000 * _numFiles));
                Assert.That(wordCounts["amet"], Is.EqualTo(3_000_000 * _numFiles));
                Assert.That(wordCounts["consectetur"], Is.EqualTo(2_000_000 * _numFiles));
            });
        }
        
        // ~0.160*4 GB
        [Test]
        // [Ignore("stresstest")]
        public async Task ProcessFilesAsync_CountsWordsCorrectly_4_File_20MilWords_8Worker()
        {
            // Start the timer
            Stopwatch stopwatch = Stopwatch.StartNew();
            // Create an instance of WordCounter
            var wordCounter = new WordCounter.WordCounter(numWorkers:8);
            // Actt Process large files asynchronously
            await wordCounter.ProcessFilesAsync(_filePaths);
            // Stop the timer
            stopwatch.Stop();
            // Get the elapsed time in milliseconds
            long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;

            Console.WriteLine($"ProcessFilesAsync execution time: {elapsedMilliseconds} ms");

            // Assert Check word counts
            var wordCounts = wordCounter.GetWordCounts();
            Assert.Multiple(() =>
            {
                Assert.That(wordCounts["Lorem"], Is.EqualTo(3_000_000 * _numFiles));
                Assert.That(wordCounts["ipsum"], Is.EqualTo(5_000_000 * _numFiles));
                Assert.That(wordCounts["dolor"], Is.EqualTo(2_000_000 * _numFiles));
                Assert.That(wordCounts["sit"], Is.EqualTo(4_000_000 * _numFiles));
                Assert.That(wordCounts["amet"], Is.EqualTo(3_000_000 * _numFiles));
                Assert.That(wordCounts["consectetur"], Is.EqualTo(2_000_000 * _numFiles));
            });
        
        }
        
        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
        // cleanup dlete temporary files after testing
            foreach (var filePath in _filePaths)
            {
                try
                {
                    File.Delete(filePath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to delete file '{filePath}': {ex.Message}");
                }
            }
        }
}