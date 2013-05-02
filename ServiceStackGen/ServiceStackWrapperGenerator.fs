namespace ServiceStackGen

open System
open System.Reflection
open System.CodeDom
open System.CodeDom.Compiler

open CommandOptions
open GenerationOptions
open Generate

type ServiceStackWrapperGenerator() =

    member this.Generate (opts: Options) (genOptions: GenerationOptions) =
        let codeUnit = GenerateUnit opts genOptions
        this.Dump(codeUnit)

    member private this.Dump(codeUnit: CodeCompileUnit) =
        let provider = new Microsoft.CSharp.CSharpCodeProvider()
        use sw = new System.IO.StringWriter()
        provider.GenerateCodeFromCompileUnit(codeUnit, sw, new CodeGeneratorOptions())
        sw.GetStringBuilder().ToString()

    member this.GenerateAssembly (opts: Options) (genOptions: GenerationOptions) =
        let codeUnit = GenerateUnit opts genOptions
        let codeText = this.Dump(codeUnit);
        let compilerParams = new CompilerParameters([| "ServiceStack.dll"; "ServiceStack.ServiceInterface.dll"; "System.Runtime.Serialization.dll"; "ServiceStack.Interfaces.dll"; genOptions.ServiceType.Assembly.ManifestModule.Name |])
        let compiler = new Microsoft.CSharp.CSharpCodeProvider()

        let result = compiler.CompileAssemblyFromDom(compilerParams, codeUnit)
        if result.Errors.Count > 0 then failwith "Failed to compile assembly"
        result.CompiledAssembly

module Generator =
    let generate (opts: Options) (genOptions: GenerationOptions) =
        let gen = new ServiceStackWrapperGenerator() in gen.Generate opts genOptions