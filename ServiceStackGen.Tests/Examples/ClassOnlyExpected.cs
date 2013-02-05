using ServiceStack.ServiceInterface;
namespace ServiceStackGen.Tests.Examples
{
    public class ClassOnlyExpected: Service

{
    private ClassOnly _classOnly;

    public ClassOnlyExpected(ClassOnly classOnly)
    {
        _classOnly = classOnly;
    }
}
}