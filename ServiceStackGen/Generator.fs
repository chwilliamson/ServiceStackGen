namespace ServiceStackGen

open System
open System.Reflection
open System.CodeDom
open System.CodeDom.Compiler

open CommandOptions
open GenerationOptions
open Generate

module Generator =
    //outputs the generated code unit to a string
    let private Dump(codeUnit: CodeCompileUnit) =
        let provider = new Microsoft.CSharp.CSharpCodeProvider()
        use sw = new System.IO.StringWriter()
        provider.GenerateCodeFromCompileUnit(codeUnit, sw, new CodeGeneratorOptions())
        sw.GetStringBuilder().ToString()

    let Generate (opts: Options) (genOptions: GenerationOptions) =
        let codeUnit = GenerateUnit opts genOptions
        Dump(codeUnit)

    let GenerateAssembly (opts: Options) (genOptions: GenerationOptions) =
        let codeUnit = GenerateUnit opts genOptions
        let compilerParams = new CompilerParameters([| "ServiceStack.dll"; "ServiceStack.ServiceInterface.dll"; "System.Runtime.Serialization.dll"; "ServiceStack.Interfaces.dll"; genOptions.ServiceType.Assembly.ManifestModule.Name |])
        let compiler = new Microsoft.CSharp.CSharpCodeProvider()

        let result = compiler.CompileAssemblyFromDom(compilerParams, codeUnit)
        if result.Errors.Count > 0 then failwith "Failed to compile assembly"
        result.CompiledAssembly