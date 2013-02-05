using ServiceStack.ServiceInterface;
namespace ServiceStackGen.Tests.Examples
{
    public class ClassWithOneMethodExpected :Service
    {
        private ClassWithOneMethod _classWithOneMethod;
        public ClassWithOneMethodExpected(ClassWithOneMethod classWithOneMethod)
        {
            _classWithOneMethod = classWithOneMethod;
        }
        public virtual DoSomethingResult Any(DoSomething doSomething)
        {
            _classWithOneMethod.DoSomething();
            return null;
        }
    }

    public class DoSomething
    {

    }

    public class DoSomethingResult
    {
    }
}