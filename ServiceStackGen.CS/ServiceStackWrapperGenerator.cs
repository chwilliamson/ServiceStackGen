using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CSharp;

namespace ServiceStackGen
{
    /// <summary>
    /// Creates the conventional service stack wrapper classes
    /// </summary>
    public class ServiceStackWrapperGenerator
    {
        public string Generate(Type type)
        {
            return Generate(new GenerationOptions(type));
        }

        public Assembly GenerateAssembly(Type serviceType)
        {
            var compileUnit = GenerateUnit(new GenerationOptions(serviceType));
            string serviceTypeAssemblyName = serviceType.Assembly.ManifestModule.Name;

            var options = new CompilerParameters(new[] { "ServiceStack.dll", "ServiceStack.ServiceInterface.dll", "ServiceStack.Interfaces.dll", serviceTypeAssemblyName });
            var compiler = new CSharpCodeProvider();

            var result = compiler.CompileAssemblyFromDom(options, compileUnit);

            if (result.Errors.Count > 0)
            {
                throw new Exception("Failed to compile assembly :(");
            }

            return result.CompiledAssembly;
        }

        public string Generate(GenerationOptions options)
        {
            var compileUnit = this.GenerateUnit(options);

            var provider = new CSharpCodeProvider();
            using (var sw = new StringWriter())
            {
                provider.GenerateCodeFromCompileUnit(compileUnit, sw, new CodeGeneratorOptions {});
                string codeText = sw.GetStringBuilder().ToString();
                return SugarUp(codeText.Substring(codeText.IndexOf("using", System.StringComparison.Ordinal)));
            }
        }

        public CodeCompileUnit GenerateUnit(GenerationOptions generationOptions)
        {
            string serviceInterfaceNamespace = "ServiceStack.ServiceInterface";
            var type = generationOptions.Target;

            var codeDomNamespace = new CodeNamespace("ServiceStackGen.Tests.Examples");
            var compileUnit = new CodeCompileUnit();

            var globalNamespace = new CodeNamespace();
            globalNamespace.Imports.Add(new CodeNamespaceImport(serviceInterfaceNamespace));

            compileUnit.Namespaces.Add(globalNamespace); 
            compileUnit.Namespaces.Add(codeDomNamespace);
            globalNamespace.Comments.Clear();

            var @class = new CodeTypeDeclaration(generationOptions.TypeName);
            @class.BaseTypes.Add(serviceInterfaceNamespace + ".Service");
            codeDomNamespace.Types.Add(@class);

            @class.Members.Add(new CodeMemberField(type.FullName, "_" + LowerCamelCase(type.Name)));

            //add constructor
            var ctor = new CodeConstructor();
            ctor.Parameters.Add(new CodeParameterDeclarationExpression(type.FullName,LowerCamelCase(type.Name)));
            ctor.Attributes = MemberAttributes.Public;
            
            @class.Members.Add(ctor);

            
            var thisDotField = new CodeTypeReferenceExpression(
                "_" + LowerCamelCase(type.Name));
            var assignment = new CodeAssignStatement(thisDotField, new CodeArgumentReferenceExpression(LowerCamelCase(type.Name)));
            ctor.Statements.Add(assignment);

            //generate methods which are mapped to their own wrapping types
            type.GetMethods(BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance)
                .Where(m =>
                    {
                        if (generationOptions.MethodNameRegex != null)
                        {
                            return Regex.IsMatch(m.Name, generationOptions.MethodNameRegex);
                        }
                        return true;
                    })

                .ToList()
                .ForEach(m =>
                {
                    var request = new CodeTypeDeclaration(m.Name);
                    codeDomNamespace.Types.Add(request);

                    
                    var response = new CodeTypeDeclaration(m.Name + "Result");
                    
                    codeDomNamespace.Types.Add(response);

                    //create any method
                    var anyMethod = new CodeMemberMethod
                        {
                            Name = "Any",
                            Attributes = MemberAttributes.Public
                               
                        };
                    //add parameters
                    anyMethod.Parameters.Add(new CodeParameterDeclarationExpression(m.Name, LowerCamelCase(request.Name)));
                    //add return
                    anyMethod.ReturnType = new CodeTypeReference(response.Name);
                    //add methodBody
                    var proxy = new CodeMethodInvokeExpression(new CodeTypeReferenceExpression("_" + LowerCamelCase(type.Name)),m.Name);
                    m.GetParameters().ToList().ForEach(p =>
                        {
                            //request
                            request.Members.Add(new CodeMemberProperty { Name = UpperCamelCase(p.Name), Type = new CodeTypeReference(TypeName(p.ParameterType.FullName)), Attributes = MemberAttributes.Abstract | MemberAttributes.Public, HasGet = true, HasSet = true});
                            //proxy assignment
                            var proxyAssign = new CodePropertyReferenceExpression(
                                new CodeTypeReferenceExpression(LowerCamelCase(request.Name))
                                ,UpperCamelCase(p.Name));
                            proxy.Parameters.Add(proxyAssign);

                        });
                   
                    if (m.ReturnType != typeof (void))
                    {
                        response.Members.Add(new CodeMemberProperty
                            {
                                Name = "Result",
                                Type = new CodeTypeReference(TypeName(m.ReturnType.FullName)),
                                Attributes = MemberAttributes.Abstract | MemberAttributes.Public,
                                HasGet = true,
                                HasSet = true
                            });

                        var creation = new CodeObjectCreateExpression(TypeName(m.Name + "Result"));

                        var variableCreation = new CodeVariableDeclarationStatement("var",
                                                                                    LowerCamelCase(
                                                                                        TypeName(m.Name + "Result")),
                                                                                    creation);

                        anyMethod.Statements.Add(variableCreation);

                        //assign to proxy
                        var reference =
                            new CodePropertyReferenceExpression(
                                new CodeVariableReferenceExpression(LowerCamelCase(m.Name + "Result")), "Result");

                        anyMethod.Statements.Add(new CodeAssignStatement(reference, proxy));

                        anyMethod.Statements.Add(new CodeMethodReturnStatement { 
                            Expression =  new CodeVariableReferenceExpression(
                                LowerCamelCase(m.Name + "Result")) });

                    }
                    else
                    {
                        anyMethod.Statements.Add(proxy);
                        // return null
                        anyMethod.Statements.Add( new CodeMethodReturnStatement { Expression = new CodePrimitiveExpression(null) });
                     
                    }
                    @class.Members.Add(anyMethod);
                });

            return compileUnit;

        }

        private string SugarUp(string s)
        {
            return s.Replace("abstract","virtual");
        }

        private string TypeName(string fullName)
        {
            return fullName;
        }

        private string LowerCamelCase(string str)
        {
            return str.Substring(0, 1).ToLowerInvariant() + str.Substring(1);
        }

        private string UpperCamelCase(string str)
        {
            return str.Substring(0, 1).ToUpperInvariant() + str.Substring(1);
        }

       

       
    }
}
