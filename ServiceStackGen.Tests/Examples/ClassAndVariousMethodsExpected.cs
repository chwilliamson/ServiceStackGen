using ServiceStack.ServiceInterface;
namespace ServiceStackGen.Tests.Examples
{
    public class ClassAndVariousMethodsExpected : Service
    {
        private ClassAndVariousMethods _classAndVariousMethods;

        public ClassAndVariousMethodsExpected(ClassAndVariousMethods classAndVariousMethods)
        {
            _classAndVariousMethods = classAndVariousMethods;
        }

        public virtual BMethodResult Any(BMethod bMethod)
        {
            _classAndVariousMethods.BMethod();
            return null;
        }

        public virtual EMethodResult Any(EMethod eMethod)
        {
            _classAndVariousMethods.EMethod();
            return null;
        }
    }

    public class BMethod
    {
        
    }
    public class BMethodResult
    {
        
    }

    public class EMethod
    {

    }
    public class EMethodResult
    {

    }
}
