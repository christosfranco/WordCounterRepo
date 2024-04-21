using CommandDotNet;

namespace WordCounter;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        return await new AppRunner<WordCounterCommands>().RunAsync(args);

    }
}