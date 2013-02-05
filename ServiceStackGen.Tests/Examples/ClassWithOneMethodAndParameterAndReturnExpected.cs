using ServiceStack.ServiceInterface;
namespace ServiceStackGen.Tests.Examples
{
    public class ClassWithOneMethodAndParameterAndReturnExpected : Service
    {
        private ClassWithOneMethodAndParameterAndReturn _classWithOneMethodAndParameterAndReturn;

        public ClassWithOneMethodAndParameterAndReturnExpected(
            ClassWithOneMethodAndParameterAndReturn classWithOneMethodAndParameterAndReturn)
        {
            _classWithOneMethodAndParameterAndReturn = classWithOneMethodAndParameterAndReturn;
        }

        public virtual BarResult Any(Bar bar)
        {
            var barResult = new BarResult();
            barResult.Result = _classWithOneMethodAndParameterAndReturn.Bar(bar.Foo);
            return barResult;
        }
    }

    public class Bar
    {
        public virtual string Foo { get; set; }
    }

    public class BarResult
    {
        public virtual string Result { get; set; }
    }
}
