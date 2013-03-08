using System;
using System.Collections.Generic;
using System.Linq;

namespace ServiceStackGen.Tests.Services
{
    public interface IServiceWithNoMethods
    {
    }

    public interface ISingleVoidOperationService
    {
        void DoSomethingReally();
    }

    public interface IServiceWithSingleMethodWithReturnValue
    {
        string GetString();
    }

    public interface IServiceWithSingleVoidMethodWithParameters
    {
        void DoTheNeedful(string arg1, int arg2);
    }

    public interface IServiceWithSingleMethodWithParametersAndReturnValue
    {
        int GetCount(object arg1, int arg2, string arg3);
    }

    public interface IServiceWithMultipleMethods
    {
        void Method1();
        int Method2(string arg1);
    }
}
