using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;

namespace ServiceStackGen.Tests
{
    [TestFixture]
    public class ServiceGeneratorTests
    {
        [Test]
        public void ShouldGenerateServiceWithSingleVoidMethod()
        {
            Assembly assembly = new ServiceStackWrapperGenerator().GenerateAssembly(typeof(Services.SingleVoidOperationService));
            Type[] types = assembly.GetTypes();

            Type requestType = types.Single(t => t.Name == "DoSomethingReally");
            Type responseType = types.Single(t => t.Name == "DoSomethingReallyResult");
            Type serviceType = types.Single(t => t.Name == "SingleVoidOperationServiceExpected");
        }
    }
}
