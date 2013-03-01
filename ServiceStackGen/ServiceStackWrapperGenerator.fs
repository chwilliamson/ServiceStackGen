namespace ServiceStackGen

open System
open System.Reflection
open System.CodeDom
open System.CodeDom.Compiler

open Generate

type ServiceStackWrapperGenerator() =
    member this.makeVirtual (codeText: string) =
        codeText.Replace("abstract", "virtual")

    member this.Generate(opts: GenerationOptions) =
        let codeUnit = GenerateUnit(opts)
        let provider = new Microsoft.CSharp.CSharpCodeProvider()
        use sw = new System.IO.StringWriter()
        provider.GenerateCodeFromCompileUnit(codeUnit, sw, new CodeGeneratorOptions())
        let codeText = sw.GetStringBuilder().ToString()
        codeText.Substring(codeText.IndexOf("using", System.StringComparison.Ordinal)) |> this.makeVirtual

    member this.Generate(t: Type) =
        this.Generate(GenerationOptions.fromType(t))

    member this.GenerateAssembly(serviceType: Type) =
        let codeUnit = GenerateUnit(GenerationOptions.fromType(serviceType))
        let compilerParams = new CompilerParameters([| "ServiceStack.dll"; "ServiceStack.ServiceInterface.dll"; "ServiceStack.Interfaces.dll"; serviceType.Assembly.ManifestModule.Name |])
        let compiler = new Microsoft.CSharp.CSharpCodeProvider()

        let result = compiler.CompileAssemblyFromDom(compilerParams, codeUnit)
        if result.Errors.Count > 0 then failwith "Failed to compile assembly"
        result.CompiledAssembly