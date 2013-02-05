using ServiceStack.ServiceInterface;
namespace ServiceStackGen.Tests.Examples
{
    public class ClassWith2MethodsExpectedOmittingBarFoo : Service
    {
        private ClassWith2Methods _classWith2Methods;

        public ClassWith2MethodsExpectedOmittingBarFoo(ClassWith2Methods classWith2Methods)
        {
            _classWith2Methods = classWith2Methods;
        }

        public virtual FooBarResult Any(FooBar fooBar)
        {
            _classWith2Methods.FooBar();
            return null;
        }
    }

    public class FooBar
    {

    }

    public class FooBarResult
    {

    }
}