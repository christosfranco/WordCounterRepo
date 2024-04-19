namespace TestWordCounter
{
    public static class IOHelpers
    {

        public static string[] CreateLargeTextFiles(int numFiles, Dictionary<string, int> wordFrequencies)
        {
            string[] filePaths = new string[numFiles];

            for (int i = 0; i < numFiles; i++)
            {
                string filePath = Path.Combine(Path.GetTempPath(), $"file{i + 1}.txt");
                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    // Generate large random text content (for demonstration purposes)
                    string content = GenerateLoremIpsum(wordFrequencies); 
                    writer.Write(content);
                    writer.AutoFlush = true;
                    writer.Close();
                }
                filePaths[i] = filePath;
            }

            return filePaths;
        }



        public static string? NavigateUpToTargetDirectory(string baseDir, int levels)
        {
            string? currentDir = baseDir;

            // Navigate up multiple levels to reach the desired parent directory
            for (int i = 0; i < levels; i++)
            {
                currentDir = Directory.GetParent(currentDir)?.FullName;

                if (currentDir == null)
                {
                    Console.WriteLine($"Failed to navigate up {levels} levels.");
                    return null;
                }
            }

            return currentDir;
        }
        public static bool IsAbsolutePath(string? path)
        {
            // Check if the path is rooted (absolute path)
            // Path.IsPathRooted returns true if the path is an absolute path
            return !string.IsNullOrEmpty(path) && Path.IsPathRooted(path);
        }

        public static string GenerateLoremIpsum(Dictionary<string, int> wordFrequencies)
        {
            // Get the list of words from the Lorem Ipsum text
            List<string> words = new List<string> { };

            // Adjust the frequencies of specific words based on input dictionary
            foreach (var kvp in wordFrequencies)
            {
                for (int i = 0; i < kvp.Value; i++)
                {
                    words.Add(kvp.Key);
                }
            }

            // Shuffle the words list to randomize the order
            var rnd = new Random();
            words = words.OrderBy(_ => rnd.Next()).ToList();

            // Take the specified number of words from the shuffled list
            //var selectedWords = words.Take(numWords);

            // Join the selected words into a single string
            string loremIpsum = string.Join(" ", words);

            return loremIpsum;
        }
        
    }
}
