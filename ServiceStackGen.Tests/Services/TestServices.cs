using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceStackGen.Tests.Services
{
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
}
