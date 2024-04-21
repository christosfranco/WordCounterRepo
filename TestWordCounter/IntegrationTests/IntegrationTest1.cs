using NUnit.Framework.Internal;

namespace TestWordCounter
{
    public class IntegrationTests
    {
        [SetUp]
        public void Setup()
        {
        }


        [Test]
        public async Task ProcessFiles_CountsWordsCorrectly_IO1()
        {
            // Get the directory of the executing assembly (your project's executable)
            string assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;

            string? system_dir = Path.GetDirectoryName(assemblyLocation);
            Assert.That(IOHelpers.IsAbsolutePath(system_dir), Is.EqualTo(true));
            // Navigate up multiple levels to reach the desired parent directory
            string base_dir = IOHelpers.NavigateUpToTargetDirectory(system_dir, 3); // Navigate up 3 levels

            Console.WriteLine(base_dir);
            // Specify the relative file paths within the base directory
            string[] filePaths = { "file1.txt", "file2.txt", "file3.txt" };

            // Combine base directory with file names to get full file paths
            string[] fullFilePaths = new string[filePaths.Length];
            for (int i = 0; i < filePaths.Length; i++)
            {
                fullFilePaths[i] = System.IO.Path.Combine(base_dir, "TestFiles" , filePaths[i]);
            }

            var wordCounter = new WordCounter.WordCounter();

            // Act
            await wordCounter.ProcessFilesAsync(fullFilePaths);

            // Assert
            var wordCounts = wordCounter.GetWordCounts();
            Assert.Multiple(() =>
            {
                Assert.That(wordCounts["hello"], Is.EqualTo(3));
                Assert.That(wordCounts["world"], Is.EqualTo(2));
                Assert.That(wordCounts["goodbye"], Is.EqualTo(1));
                Assert.That(wordCounts["again"], Is.EqualTo(1));
            });
        }

        
        [Test]
        public async Task ProcessFiles_CountsWordsCorrectly_IO2()
        {
            // Get the directory of the executing assembly (your project's executable)
            string assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;

            string? system_dir = Path.GetDirectoryName(assemblyLocation);
            Assert.That(IOHelpers.IsAbsolutePath(system_dir), Is.EqualTo(true));
            // Navigate up multiple levels to reach the desired parent directory
            string base_dir = IOHelpers.NavigateUpToTargetDirectory(system_dir, 3); // Navigate up 3 levels

            Console.WriteLine(base_dir);
            // Specify the relative file paths within the base directory
            string[] filePaths = { "file1.txt", "file2.txt", "file3.txt" };

            // Combine base directory with file names to get full file paths
            string[] fullFilePaths = new string[filePaths.Length];
            for (int i = 0; i < filePaths.Length; i++)
            {
                fullFilePaths[i] = System.IO.Path.Combine(base_dir, "TestFiles" , filePaths[i]);
            }

            var wordCounter = new WordCounter.WordCounter();

            // Act
            await wordCounter.ProcessFilesAsync(fullFilePaths);

            // Assert
            var wordCounts = wordCounter.GetWordCounts();
            Assert.Multiple(() =>
            {
                Assert.That(wordCounts["hello"], Is.EqualTo(3));
                Assert.That(wordCounts["world"], Is.EqualTo(2));
                Assert.That(wordCounts["goodbye"], Is.EqualTo(1));
                Assert.That(wordCounts["again"], Is.EqualTo(1));
            });
        }

        [Test]
        public async Task ProcessFilesAsync_CountsWordsCorrectly_Generated()
        {
            // Arrange
            int numFiles = 2; // Number of large files

            var wordFrequencies = new Dictionary<string, int>
                {
                    { "Lorem", 3 },
                    { "ipsum", 5 },
                    { "dolor", 2 },
                    { "sit", 4 },
                    { "amet", 3 },
                    { "consectetur", 2 }
                };

            // Create large text files
            string[] filePaths = IOHelpers.CreateLargeTextFiles(numFiles, wordFrequencies);

            try
            {
                // Create an instance of WordCounter
                var wordCounter = new WordCounter.WordCounter();

                // Act: Process large files asynchronously
                await wordCounter.ProcessFilesAsync(filePaths);

                // Assert: Check word counts
                var wordCounts = wordCounter.GetWordCounts();
                Assert.Multiple(() =>
                {
                    Assert.That(wordCounts["Lorem"], Is.EqualTo(3* numFiles));
                    Assert.That(wordCounts["ipsum"], Is.EqualTo(5 * numFiles));
                    Assert.That(wordCounts["dolor"], Is.EqualTo(2 * numFiles));
                    Assert.That(wordCounts["sit"], Is.EqualTo(4 * numFiles));
                    Assert.That(wordCounts["amet"], Is.EqualTo(3 * numFiles));
                    Assert.That(wordCounts["consectetur"], Is.EqualTo(2 * numFiles));
            });
            }
            finally
            {
                // Cleanup: Delete temporary files after testing
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

        // TODO test with insanely large files. Seems that some memory is lost (maybe due to byte split when reading chunks)?
        [Test]
        public async Task ProcessFilesAsync_CountsWordsCorrectly_Bigger_Files()
        {
            // Arrange
            int numFiles = 200; // Number of large files

            var wordFrequencies = new Dictionary<string, int>
                {
                    { "Lorem", 3000 },
                    { "ipsum", 5000 },
                    { "dolor", 2000 },
                    { "sit", 4000 },
                    { "amet", 3000 },
                    { "consectetur", 2000 }
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
                    Assert.That(wordCounts["Lorem"], Is.EqualTo(3000 * numFiles));
                    Assert.That(wordCounts["ipsum"], Is.EqualTo(5000 * numFiles));
                    Assert.That(wordCounts["dolor"], Is.EqualTo(2000 * numFiles));
                    Assert.That(wordCounts["sit"], Is.EqualTo(4000 * numFiles));
                    Assert.That(wordCounts["amet"], Is.EqualTo(3000 * numFiles));
                    Assert.That(wordCounts["consectetur"], Is.EqualTo(2000 * numFiles));
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
        
        // TODO test with insanely large files. Seems that some memory is lost (maybe due to byte split when reading chunks)?
        [Test]
        public async Task ProcessFilesAsync_CountsWordsCorrectly_Bigger_Files2()
        {
            // Arrange
            int numFiles = 1; // Number of large files

            var wordFrequencies = new Dictionary<string, int>
                {
                    { "Lorem", 3000 },
                    { "ipsum", 5000 },
                    { "dolor", 2000 },
                    { "sit", 4000 },
                    { "amet", 3000 },
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
                    Assert.That(wordCounts["Lorem"], Is.EqualTo(3000 * numFiles));
                    Assert.That(wordCounts["ipsum"], Is.EqualTo(5000 * numFiles));
                    Assert.That(wordCounts["dolor"], Is.EqualTo(2000 * numFiles));
                    Assert.That(wordCounts["sit"], Is.EqualTo(4000 * numFiles));
                    Assert.That(wordCounts["amet"], Is.EqualTo(3000 * numFiles));
                    Assert.That(wordCounts["consectetur"], Is.EqualTo(20000000 * numFiles));
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

}