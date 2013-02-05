using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace ServiceStackGen.Tests
{
    [TestFixture]
    public class ServiceStackWrapperGeneratorFixture
    {
        /// <summary>
        /// Generate a class only
        /// </summary>
        [Test]
        public void ClassOnly()
        {
            var gen = new ServiceStackWrapperGenerator();
            var result = gen.Generate(typeof(Examples.ClassOnly));

            AssertIs(result,typeof(Examples.ClassOnlyExpected));
            
        }

        /// <summary>
        /// Generate a class only
        /// </summary>
        [Test]
        public void GenerateVoidMethodHasClassRepresentingInput()
        {
            var gen = new ServiceStackWrapperGenerator();
            var result = gen.Generate(typeof(Examples.MyServiceWithOneMethod));

            AssertIs(result, typeof(Examples.MyServiceWithOneMethodExpected));

        }

        private void AssertIs(string result,Type expected)
        {
            using (var s = File.OpenRead(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Examples", expected.Name + ".cs")))
            using(var sr = new StreamReader(s))
            {
                //don't compare whitespace
                var expectedStr = Regex.Replace(sr.ReadToEnd(), @"\s", "");
                var actual = Regex.Replace(result, @"\s", "");

                Assert.AreEqual(expectedStr, actual);

            }
        }
    }
}
