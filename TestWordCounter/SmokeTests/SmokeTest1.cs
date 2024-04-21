using NUnit.Framework.Internal;

namespace TestWordCounter.SmokeTests
{
    
    internal class SmokeTest1
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
            string[] filePaths = { "file1.txt", "file2.txt" };

            // Combine base directory with file names to get full file paths
            string[] fullFilePaths = new string[filePaths.Length];
            for (int i = 0; i < filePaths.Length; i++)
            {
                fullFilePaths[i] = System.IO.Path.Combine(base_dir, "SmokeTests", filePaths[i]);
            }

            var wordCounter = new WordCounter.WordCounter();

            // Act
            await wordCounter.ProcessFilesAsync(fullFilePaths);

            // Assert
            var wordCounts = wordCounter.GetWordCounts();
            Assert.Multiple(() =>
            {
                Assert.That(wordCounts["Go"], Is.EqualTo(1));
                Assert.That(wordCounts["do"], Is.EqualTo(2));
                Assert.That(wordCounts["that"], Is.EqualTo(2));
                Assert.That(wordCounts["thing"], Is.EqualTo(1));
                Assert.That(wordCounts["you"], Is.EqualTo(1));
                Assert.That(wordCounts["so"], Is.EqualTo(1));
                Assert.That(wordCounts["well"], Is.EqualTo(2));
                Assert.That(wordCounts["I"], Is.EqualTo(1));
                Assert.That(wordCounts["play"], Is.EqualTo(1));
                Assert.That(wordCounts["football"], Is.EqualTo(1));
            });
        }
    }
}
