using NUnit.Framework;
using FizzBuzz;
using System.Collections.Generic;
using System;

namespace TestProject1
{
    [TestFixture]
    public class FizzBuzzTester
    {
        private FizzBuzzer _fizzBuzzer;

        [SetUp]
        public void SetUp()
        {
            _fizzBuzzer = new FizzBuzzer();
        }
        
        [Test]
        public void FizzBuzzLst_String_Tests()
        {
            List<string> Expected = new List<string>() { "Fizz", "Buzz", "FizzBuzz" };

            int[] inputLst = new int[] { 3, 5, 15 };


            List<string> result = _fizzBuzzer.FizzBuzzLst(inputLst);

            Assert.AreEqual(result, Expected);
        }

        [TestCase(3, ExpectedResult = "Fizz")]
        [TestCase(5, ExpectedResult = "Buzz")]
        [TestCase(15, ExpectedResult = "FizzBuzz")]

        public string FizzBuzz_String_Tests(int input)
        {
            string result = _fizzBuzzer.FizzBuzz(input);
            return result;
        }

        // Return input START
        // Assuming that the function shoudl return the input as typeof(string), similar to the fizzbuzz
        [Test]
        public void FizzBuzzLst_Int_Tests()
        {
            List<string> Expected = new List<string>() { "7", "13", "22"};

            int[] inputLst = new int[] { 7, 13, 22 };


            List<string> result = _fizzBuzzer.FizzBuzzLst(inputLst);

            Assert.AreEqual(result, Expected);
        }

        [TestCase(7, ExpectedResult = "7")]
        [TestCase(13, ExpectedResult = "13")]
        [TestCase(22, ExpectedResult = "22")]

        public string FizzBuzz_Int_Tests(int input)
        {
            string result = _fizzBuzzer.FizzBuzz(input);
            return result;
        }

        // Return input END

        // Exceptions START
        [TestCase(new int[]  { 1, 0, -2 } )]
        public void FizzBuzzLst_ArgumentOutOfRangeException(int[] inputLst)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _fizzBuzzer.FizzBuzzLst(inputLst));
        }

        [TestCase(-1)]
        [TestCase(0)]
        public void FizzBuzz_ArgumentOutOfRangeException(int input)
        {

            Assert.Throws<ArgumentOutOfRangeException>(() => _fizzBuzzer.FizzBuzz(input));
        }

        // Exceptions END
    }
}