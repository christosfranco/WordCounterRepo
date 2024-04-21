using NUnit.Framework.Internal;

namespace TestWordCounter.StressTests;

public class StressTest1
{

        [SetUp]
        public void Setup()
        {
        }
        [Test]
        [Ignore("Skipping stresstest, Memory overload ")]
        public async Task ProcessFilesAsync_CountsWordsCorrectly_2Mil_Words()
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
}