module ServiceStackGen.CommandOptions
open System.Reflection
open System.Text.RegularExpressions

type Options = 
    {
        SourceNamespace : string;
        TargetNamespace : string;
        OutputDir : string;
        SourceAssembly : Assembly
    }

type ParseResult = 
    | Success of Options
    | Fail of string list

let optionRegex = new Regex("^/(\w+):(.*)$")
let optionNames = Set.ofList ["srcns"; "tns"; "dir"]

let (|Opt|Unknown|Malformed|) (s: string) =
    let m = optionRegex.Match(s)
    if m.Success then 
        let optName = m.Groups.[1].Value
        if Set.contains optName optionNames then Opt(optName, m.Groups.[2].Value) else Unknown(optName)
    else Malformed

let tryParse optMap errList optStr =
    match optStr with
    | Opt(opt, value) -> (Map.add opt value optMap, errList)
    | Unknown(opt) -> (optMap, sprintf "Unknown option %s" opt :: errList)
    | Malformed -> (optMap, sprintf "Invalid option format for %s" optStr :: errList)

let tryParseOptions = (Map.empty, []) |> List.fold (fun (m, errs) opt -> tryParse m errs opt)

let parseOptions (assm: Assembly) opts =
    let optMap, parseErrors = tryParseOptions opts
    if List.isEmpty parseErrors then
        let optKeys = optMap |> Map.toList |> List.map fst |> Set.ofList
        let missing = Set.difference optionNames optKeys
        if missing.IsEmpty then Success({ SourceNamespace = Map.find "srcns" optMap; TargetNamespace = Map.find "tns" optMap; OutputDir = Map.find "dir" optMap; SourceAssembly = assm })
        else Fail(["Duplicate option definition(s) for %s" + String.concat "," missing])
    else Fail(parseErrors)