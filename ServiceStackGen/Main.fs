module ServiceStackGen.Main
open System
open System.Reflection

open GenOutput
open CommandOptions

[<EntryPoint>]
let main argv = 
    match argv with
    | [| arg1; arg2; arg3; arg4 |] ->
        let assm =  try Some(Assembly.LoadFrom(arg4))
                    with
                    | ex -> printfn "%s" ex.Message; None

        match assm with
        | Some(assembly) ->
            match parseOptions assembly [arg1; arg2; arg3] with
            | Success(options) -> 
                generateAll options
                0
            | Fail(errs) ->
                Seq.iter (fun err -> printfn "%s" err) errs |> ignore
                //System.Console.ReadLine() |> ignore
                -1
        | None -> -1
    | _ ->
        printfn "Usage: ServiceStackGen /srcns:SourceNamespace /tns:TargetNamespace /dir:OutputDir assembly"
        printfn "ServiceStackGen  is a utility which generates a ServiceStack wrapper class for any service"
        printfn "under the specified source namespace. The generated class takes it's namespace from target"
        printfn "namespace and the source files are placed in the supplied output pulder."
        printfn "  /srcns    - Namespace of the source services"
        printfn "  /tns      - Namespace of the target classes"
        printfn "  /dir      - Output folder to put generated files"
        printfn "  assembly  - The source assembly containing the source services"
       // System.Console.ReadLine() |> ignore
        -1
