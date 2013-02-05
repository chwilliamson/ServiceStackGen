using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CSharp;

namespace ServiceStackGen
{
    public class ServiceStackWrapperGenerator
    {
        public string Generate(Type type)
        {
            var codeDomNamespace = new CodeNamespace("ServiceStackGen.Tests.Examples");
            var compileUnit = new CodeCompileUnit();
            compileUnit.Namespaces.Add(codeDomNamespace);

            var @class = new CodeTypeDeclaration(type.Name + "Expected");
            codeDomNamespace.Types.Add(@class);

            //generate methods which are mapped to their own wrapping types
            type.GetMethods(BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance)
                .ToList()
                .ForEach(m =>
                {
                    var methodClass = new CodeTypeDeclaration(m.Name);
                    codeDomNamespace.Types.Add(methodClass);
                });

            var provider = new CSharpCodeProvider();
            var sb = new StringBuilder();
            using (var sw = new StringWriter(sb))
            {
                provider.GenerateCodeFromCompileUnit(compileUnit, sw, new CodeGeneratorOptions {});
            }
            var s = sb.ToString();
            //remove generated comments
            return s.Substring(s.IndexOf("namespace", System.StringComparison.Ordinal));
        }

       

       
    }
}
