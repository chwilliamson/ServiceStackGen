using System;

namespace ServiceStackGen
{
    public class GenerationOptions
    {
        public string TypeName { get; set; }
        public Type Target { get; set; }
        public string MethodNameRegex { get; set; }
    }
}
