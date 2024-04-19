using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;


class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: WordCounter <file1> <file2> ...");
            return;
        }

        var fileNames = args;

        var wordCounter = new WordCounter.WordCounter();
        await wordCounter.ProcessFilesAsync(fileNames);
    }
}
