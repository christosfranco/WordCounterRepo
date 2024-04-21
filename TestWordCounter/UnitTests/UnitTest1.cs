namespace TestWordCounter.UnitTests
{
    [TestFixture]
    public class UnitTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public Task CutChunkCorrectly()
        {
            var wordCounter = new WordCounter.WordCounter();

            // Act
            // await wordCounter.ProcessFilesAsync(fullFilePaths);
            string chunk = "hello world again";
            string chunk2 = "hello world again ";
            // Assert
            var lastword = wordCounter.CutWord(chunk);
            var lastword2 = wordCounter.CutWord(chunk2);

            Assert.Multiple(() =>
            {
                Assert.That(lastword, Is.EqualTo(("again","hello world")));
                Assert.That(lastword2, Is.EqualTo(("","hello world again ")));
            });
            return Task.CompletedTask;
        }
    
    }
}