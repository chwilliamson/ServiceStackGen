module ServiceStackGen.Main
open System
open System.IO
open System.Reflection

open Utils
open GenOutput
open CommandOptions

let usageLines =
    [   "Usage: ServiceStackGen /srcns:SourceNamespace /tns:TargetNamespace /dir:OutputDir /asm:assembly";
        "ServiceStackGen is a utility which generates a ServiceStack wrapper class for any service";
        "under the specified source namespace found in the input assembly. The generated class is generated in the given target";
        "namespace and the source files are placed in the specified output folder.";
        "  /srcns    - Namespace of the source services";
        "  /tns      - Namespace of the target classes";
        "  /dir      - Output folder to put generated files";
        "  /asm      - The source assembly containing the source services"
    ]

let printLines lines = lines |> Seq.iter (fun s -> printfn "%s" s)
let printUsage () = printLines usageLines

let loadOptions argv =
    match argv with
    | [| |] -> loadOptFile "options.xml"
    | [| path |] -> loadOptFile path
    | [| arg1; arg2; arg3; arg4 |] ->
        loadOpts arg1 arg2 arg3 arg4
    | _ -> Left(["Unexpected number of options"])

[<EntryPoint>]
let main argv = 
    match loadOptions argv with
    | Left(errs) ->
        printLines (errs @ (System.Environment.NewLine :: usageLines))
        -1
    | Right(opts) ->
        generateAll opts
        0
