using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using NUnit.Framework;
using Rhino.Mocks;

using ServiceStackGen;
using ServiceStackGen.Tests.Services;

namespace ServiceStackGen.Tests
{
    [TestFixture]
    public class ServiceGeneratorTests
    {
        [Test]
        public void ShouldGenerateServiceWithSingleVoidMethod()
        {
            Assembly assembly = new ServiceStackWrapperGenerator().GenerateAssembly(typeof(ISingleVoidOperationService));
            Type[] types = assembly.GetTypes();

            var mockService = MockRepository.GenerateMock<Services.ISingleVoidOperationService>();

            object service = CreateService<ISingleVoidOperationService>(types, mockService);
            var rrTypes = GetRequestResponseTypes<ISingleVoidOperationService>(s => s.DoSomethingReally(), types);

            object request = Activator.CreateInstance(rrTypes.RequestType);
            object response = service.Any(request);

            mockService.AssertWasCalled(s => s.DoSomethingReally());
            Assert.IsInstanceOf(rrTypes.ResponseType, response);
        }

        [Test]
        public void ShouldGenerateServiceWithVoidMethodWithArguments()
        {
            Assembly assembly = new ServiceStackWrapperGenerator().GenerateAssembly(typeof(IServiceWithSingleVoidMethodWithParameters));
            Type[] types = assembly.GetTypes();

            var mockService = MockRepository.GenerateStub<IServiceWithSingleVoidMethodWithParameters>();
            string arg1 = "Hello";
            int arg2 = 4;

            object service = CreateService<IServiceWithSingleVoidMethodWithParameters>(types, mockService);
            var rrTypes = GetRequestResponseTypes<IServiceWithSingleVoidMethodWithParameters>(s => s.DoTheNeedful(arg1, arg2), types);

            dynamic request = Activator.CreateInstance(rrTypes.RequestType);
            request.Arg1 = arg1;
            request.Arg2 = arg2;

            object response = service.Any((object)request);

            mockService.AssertWasCalled(s => s.DoTheNeedful(arg1, arg2));
            Assert.IsInstanceOf(rrTypes.ResponseType, response);
        }

        [Test]
        public void ShouldGenerateServiceWithSingleMethodWithReturnValue()
        {
            Assembly assembly = new ServiceStackWrapperGenerator().GenerateAssembly(typeof(IServiceWithSingleMethodWithReturnValue));
            Type[] types = assembly.GetTypes();

            var mockService = MockRepository.GenerateStub<IServiceWithSingleMethodWithReturnValue>();
            string responseString = "Hello world!";
            mockService.Stub(s => s.GetString()).Return(responseString);

            object service = CreateService<IServiceWithSingleMethodWithReturnValue>(types, mockService);
            var rrTypes = GetRequestResponseTypes<IServiceWithSingleMethodWithReturnValue>(s => s.GetString(), types);

            object request = Activator.CreateInstance(rrTypes.RequestType);
            dynamic response = service.Any(request);

            Assert.AreEqual(responseString, response.Result);
        }

        public dynamic CreateService<T>(Type[] types, T service)
        {
            Type serviceWrapperType = GetServiceWrapperType<T>(types);
            return Activator.CreateInstance(serviceWrapperType, service);
        }

        public Type GetServiceWrapperType<T>(Type[] types)
        {
            string wrapperTypeName = typeof(T).Name + "Expected";
            return types.Single(t => t.Name == wrapperTypeName);
        }

        public Generate.RequestResponseType GetRequestResponseTypes<T>(Expression<Action<T>> invokeExpr, Type[] types)
        {
            MethodInfo method = ((MethodCallExpression)invokeExpr.Body).Method;
            string methodName = method.Name;
            string pascalCase = Char.ToUpperInvariant(methodName[0]) + methodName.Substring(1);
            Type requestType = types.Single(t => t.Name == pascalCase);
            Type responseType = types.Single(t => t.Name == pascalCase + "Result");

            return new Generate.RequestResponseType(requestType, responseType);
        }
    }

    public static class ServiceExtensions
    {
        public static object Any(this object service, dynamic request)
        {
            MethodInfo method = service.GetType().GetMethod("Any", new Type[] { request.GetType() });
            return method.Invoke(service, new object[] { request });
        }
    }
}
