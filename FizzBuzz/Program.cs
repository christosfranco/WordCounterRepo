using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace FizzBuzz
{
    public class FizzBuzzer
    {
        public string FizzBuzz(int input)
        {
            if (input < 1)
            {
                Console.WriteLine("Must be natural non-zero number");
                throw new ArgumentOutOfRangeException($"{nameof(input)} must be natural non-zero number.");
            }

            bool divisible3 = input % 3 == 0;
            bool divisible5 = input % 5 == 0;

            //● “FizzBuzz” if the number is divisible by both 3 and 5 
            if (divisible3 && divisible5) { return "FizzBuzz"; }
            //● “Fizz” if the number is divisible by 3 
            else if (divisible3) { return "Fizz"; }
            //● “Buzz” if the number is divisible by 5 
            else if (divisible5) { return "Buzz"; }
            //● The number itself if it is not divisible by both 3 or 5
            else { return input.ToString(); }
        }

        public List<string> FizzBuzzLst(int[] intlst)
        {
            List<string> strLst = new List<string>();

            for (int i = 0; i < intlst.Count(); i++)
            {
                strLst.Add(FizzBuzz(intlst[i]));
            }

            return strLst;
        }
    }

    class Program
    {
        static void Main()
        {
            var _fizzbuzzer = new FizzBuzzer();

            Console.WriteLine("Write array of natural non-zero numbers, on format eg.: 1 , 3 , 5");

            // Run until cancel program
            while (true)
            {
                // For any given natural number greater than zero print the following:
                try
                {
                    // Convert input to int[]
                    int[] input = (Console.ReadLine().Split(',').Select(int.Parse).ToArray());
                    // Calculate ? FizzBuzz for all inputs
                    List<string> list = _fizzbuzzer.FizzBuzzLst(input);
                    // Print output to console
                    for (int i = 0; i < list.Count(); i++)
                    {
                        Console.WriteLine(list[i]);
                    }
                }
                catch (System.FormatException ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine("Make parameter a list of natural non-zero number(s)");

                    throw new FormatException("Input of wrong format", ex);
                }
            }
        }
    }
}
