module ServiceStackGen.GenOutput
open System
open System.IO

open Utils
open Generator
open CommandOptions
open GenerationOptions

let generateAndWrite path (opts: Options) (genOptions: GenerationOptions) =
    let src = generate opts genOptions
    File.WriteAllText(path, src)

let generateAndWriteType (targetDir: string) (opts: Options) (t: Type) =
    let serviceTypeName = if t.Name.StartsWith("I") then t.Name.Substring(1) else t.Name
    let outputPath = Path.Combine(targetDir, serviceTypeName + ".cs")
    generateAndWrite outputPath opts { ServiceType = t; TargetTypeName = serviceTypeName }

let isCompilerGenerated (t: Type) =
    Attribute.IsDefined(t, typeof<System.Runtime.CompilerServices.CompilerGeneratedAttribute>)

let isInterface (t: Type) = t.IsInterface

let inNamespace ns (t: Type) = t.Namespace = ns

let (genRestrictions: (Type -> bool) list) = [isCompilerGenerated >> not; isInterface]
let private shouldGenerate ns = allp ((inNamespace ns) :: genRestrictions)

let generateAll (opts: Options) =
    let types = opts.SourceAssembly.GetTypes() |> Array.filter (shouldGenerate opts.SourceNamespace)
    let targetDir = opts.OutputDir

    if(not (Directory.Exists(targetDir))) then
        Directory.CreateDirectory(targetDir) |> ignore

    types |> Seq.iter (generateAndWriteType targetDir opts) |> ignore