using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace ServiceStackGen.Tests
{
    [TestFixture]
    public class ServiceStackWrapperGeneratorFixture
    {
        [Test, Ignore("Figure out whether this is needed")]
        public void ClassAndVariousMethodsMethodOmitted()
        {
            var gen = new ServiceStackWrapperGenerator();
            var result = gen.Generate(new Generate.GenerationOptions(typeof(Examples.ClassWith2Methods), typeof(Examples.ClassWith2MethodsExpectedOmittingBarFoo).Name, "ServiceStackGen.Tests.Examples"));
            AssertResultEqual(result, typeof(Examples.ClassWith2MethodsExpectedOmittingBarFoo));
        }

        private void AssertResultEqual(string result,Type expected)
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
