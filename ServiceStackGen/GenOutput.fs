module ServiceStackGen.GenOutput
open System
open System.IO

open Utils
open Generator
open CommandOptions

let generateAndWrite path (serviceType: Type) =
    let src = generate serviceType
    File.WriteAllText(path, src)

let isCompilerGenerated (t: Type) =
    Attribute.IsDefined(t, typeof<System.Runtime.CompilerServices.CompilerGeneratedAttribute>)

let canInstantiate (t: Type) = not t.IsAbstract && t.IsPublic

let inNamespace ns (t: Type) = t.Namespace = ns

let (genRestrictions: (Type -> bool) list) = [isCompilerGenerated >> not; canInstantiate]
let private shouldGenerate ns = allp ((inNamespace ns) :: genRestrictions)

let generateAll (opts: Options) =
    let types = opts.SourceAssembly.GetTypes() |> Array.filter (shouldGenerate opts.SourceNamespace)
    let targetDir = opts.OutputDir

    if(not (Directory.Exists(targetDir))) then
        Directory.CreateDirectory(targetDir) |> ignore

    types |> Seq.iter (fun t -> generateAndWrite (Path.Combine(targetDir, t.Name + ".cs")) t) |> ignore