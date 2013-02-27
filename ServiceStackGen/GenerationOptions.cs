using System;

namespace ServiceStackGen
{
    public class GenerationOptions
    {
        public GenerationOptions()
        {
        }

        public GenerationOptions(Type targetType)
        {
            this.Target = targetType;
            this.TypeName = targetType.Name + "Expected";
        }

        public string TypeName { get; set; }
        public Type Target { get; set; }
        public string MethodNameRegex { get; set; }
    }
}
