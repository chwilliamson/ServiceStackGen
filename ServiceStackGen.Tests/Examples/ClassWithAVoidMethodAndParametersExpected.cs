using ServiceStack.ServiceInterface;
namespace ServiceStackGen.Tests.Examples
{
    public class ClassWithAVoidMethodAndParametersExpected : Service
    {
        private ClassWithAVoidMethodAndParameters _classWithAVoidMethodAndParameters;

        public ClassWithAVoidMethodAndParametersExpected(
            ClassWithAVoidMethodAndParameters classWithAVoidMethodAndParameters)
        {
            _classWithAVoidMethodAndParameters = classWithAVoidMethodAndParameters;
        }

        public virtual FooResult Any(Foo foo)
        {
            _classWithAVoidMethodAndParameters.Foo(foo.Name);
            return null;
        }
    }

    public class Foo
    {
        public virtual string Name { get; set; }
    }
    public class FooResult
    {
        
    }
}
