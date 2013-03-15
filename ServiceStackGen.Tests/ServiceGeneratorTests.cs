using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using NUnit.Framework;
using Rhino.Mocks;
using ServiceStack.ServiceHost;
using ServiceStackGen;
using ServiceStackGen.Tests.Services;

namespace ServiceStackGen.Tests
{
    [TestFixture]
    public class ServiceGeneratorTests
    {
        [Test]
        public void ShouldGenerateServiceWithNoMethods()
        {
            Assembly assembly = new ServiceStackWrapperGenerator().GenerateAssembly(typeof(IServiceWithNoMethods));
            Type[] types = assembly.GetTypes();

            IServiceWithNoMethods mockService = MockRepository.GenerateStub<IServiceWithNoMethods>();
            object service = CreateService<IServiceWithNoMethods>(types, mockService);

            Assert.IsNotNull(service);
        }

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

        [Test]
        public void ShouldGenerateServiceWithSingleMethodWithParametersAndReturnValue()
        {
            Assembly assembly = new ServiceStackWrapperGenerator().GenerateAssembly(typeof(IServiceWithSingleMethodWithParametersAndReturnValue));
            Type[] types = assembly.GetTypes();

            var mockService = MockRepository.GenerateStub<IServiceWithSingleMethodWithParametersAndReturnValue>();
            int result = 4;
            object arg1 = new object();
            int arg2 = 7;
            string arg3 = "arg3";

            mockService.Stub(s => s.GetCount(arg1, arg2, arg3)).Return(result);

            object service = CreateService<IServiceWithSingleMethodWithParametersAndReturnValue>(types, mockService);
            var rrTypes = GetRequestResponseTypes<IServiceWithSingleMethodWithParametersAndReturnValue>(s => s.GetCount(arg1, arg2, arg3), types);

            dynamic request = Activator.CreateInstance(rrTypes.RequestType);
            request.Arg1 = arg1;
            request.Arg2 = arg2;
            request.Arg3 = arg3;

            dynamic response = service.Any((object)request);
            Assert.AreEqual(result, response.Result);
        }

        [Test]
        public void ShouldGenerateServiceWithMultipleMethods()
        {
            Assembly assembly = new ServiceStackWrapperGenerator().GenerateAssembly(typeof(IServiceWithMultipleMethods));
            Type[] types = assembly.GetTypes();

            var mockService = MockRepository.GenerateMock<IServiceWithMultipleMethods>();

            object service = CreateService<IServiceWithMultipleMethods>(types, mockService);

            //method1
            var rrTypes1 = GetRequestResponseTypes<IServiceWithMultipleMethods>(s => s.Method1(), types);
            object request1 = Activator.CreateInstance(rrTypes1.RequestType);
            object response1 = service.Any(request1);

            mockService.AssertWasCalled(s => s.Method1());
            Assert.IsInstanceOf(rrTypes1.ResponseType, response1);

            //method 2
            string arg1 = "arg1";
            int result = 1;
            var rrTypes2 = GetRequestResponseTypes<IServiceWithMultipleMethods>(s => s.Method2(arg1), types);

            mockService.Expect(s => s.Method2(arg1)).Return(result);

            dynamic request2 = Activator.CreateInstance(rrTypes2.RequestType);
            request2.Arg1 = arg1;

            dynamic response2 = service.Any((object)request2);
            Assert.AreEqual(result, response2.Result);
        }

        [Test]
        public void RequestTypeShouldImplementIReturn()
        {
            Assembly assembly = new ServiceStackWrapperGenerator().GenerateAssembly(typeof(IServiceWithSingleMethodWithReturnValue));
            Type[] types = assembly.GetTypes();

            var rrTypes = GetRequestResponseTypes<IServiceWithSingleMethodWithReturnValue>(s => s.GetString(), types);
            Type iReturnInterfaceType = typeof(IReturn<>).MakeGenericType(rrTypes.ResponseType);
            Assert.IsTrue(iReturnInterfaceType.IsAssignableFrom(rrTypes.RequestType), "Request type does not implement IReturn<Response>");
        }

        [Test]
        public void ShouldDecorateRequestAndResponseTypesWithSerialisationAttributes()
        {
            Assembly assembly = new ServiceStackWrapperGenerator().GenerateAssembly(typeof(IServiceWithSingleMethodWithParametersAndReturnValue));
            Type[] types = assembly.GetTypes();

            var rrTypes = GetRequestResponseTypes<IServiceWithSingleMethodWithParametersAndReturnValue>(s => s.GetCount(1,2, "sdfs"), types);

            AssertHasAttribute<DataContractAttribute>(rrTypes.RequestType);
            AssertHasAttribute<DataContractAttribute>(rrTypes.ResponseType);

            var allProperties = rrTypes.RequestType.GetProperties().Concat(rrTypes.ResponseType.GetProperties());
            foreach (PropertyInfo prop in allProperties)
            {
                AssertHasAttribute<DataMemberAttribute>(prop);
            }
        }

        private static void AssertHasAttribute<TAttribute>(MemberInfo member) where TAttribute : Attribute
        {
            int attrCount = member.GetCustomAttributes(typeof(TAttribute), false).Length;
            Assert.GreaterOrEqual(attrCount, 1, "Member {0} does is not decorated with attribute {1}", member.Name, typeof(TAttribute).Name);
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
        public static object Any(this object service, object request)
        {
            MethodInfo method = service.GetType().GetMethod("Any", new Type[] { request.GetType() });
            return method.Invoke(service, new object[] { request });
        }
    }
}
