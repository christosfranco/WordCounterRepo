using NUnit.Framework.Internal;

namespace TestWordCounter
{
    public class UnitTests
    {
        [SetUp]
        public void Setup()
        {
        }


        [Test]
        public async Task ProcessFiles_CountsWordsCorrectly()
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
    }
}