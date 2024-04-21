using CommandDotNet;

namespace WordCounter;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var app = new AppRunner<WordCounterCommands>();
        return await app.RunAsync(args);
    }
}