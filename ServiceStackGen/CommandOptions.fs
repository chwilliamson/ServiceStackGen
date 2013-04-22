module ServiceStackGen.CommandOptions

open System.Reflection
open System.Xml.Linq
open System.Text.RegularExpressions

open Utils

type Options = 
    {
        SourceNamespace : string;
        TargetNamespace : string;
        OutputDir : string;
        SourceAssembly : Assembly
    }

let loadOptionsFromFile (doc: XDocument) =
    doc.Root.Descendants() |> Seq.map (fun e -> (e.Name.LocalName, e.Value)) |> Map.ofSeq

let tryLoadOptionFileMap (path: string) =
    try
        XDocument.Load(path) |> loadOptionsFromFile |> Right
    with
    | ex -> 
        let errMessage = sprintf "Failed to load options file '%s': %s" path ex.Message
        Left(errMessage)

let optionRegex = new Regex("^/(\w+):(.*)$")
let optionNames = Set.ofList ["srcns"; "tns"; "dir"; "asm"]

let (|Opt|Malformed|) (s: string) =
    let m = optionRegex.Match(s)
    if m.Success then Opt(m.Groups.[1].Value, m.Groups.[2].Value)
    else Malformed

let parseOption optStr =
    match optStr with
    | Opt(opt, value) -> Right((opt, value))
    | Malformed -> Left(sprintf "Invalid option format for %s" optStr)

let tryLoadAssembly path =
    try Assembly.LoadFrom(path) |> Right
    with
    | ex -> Left([ex.Message])

let private getOptionValues k1 k2 k3 m f =
    f (Map.find k1 m) (Map.find k2 m) (Map.find k3 m)

let private createOpts (assm: Assembly) optMap =
    getOptionValues "srcns" "tns" "dir" optMap (fun srcns tns dir ->
        { SourceNamespace = srcns; TargetNamespace = tns; OutputDir = dir; SourceAssembly = assm }
    )

let private checkUnknownOptions optMap =
    let unknownOpts = Set.difference (mapKeys optMap) optionNames
    if unknownOpts.IsEmpty then Right(optMap) else Left(["Unknown option(s): " + String.concat ", " unknownOpts])

let private checkMissingOptions optMap =
    let options = mapKeys optMap
    let missing = Set.difference optionNames options
    if missing.IsEmpty then Right(optMap) else Left(["Missing required option(s): " + String.concat ", " missing])

let loadOptMap optMap =
    optMap 
        |> checkUnknownOptions
        |> bind checkMissingOptions
        |> bind (fun m ->
            let assmPath = Map.find "asm" m
            tryLoadAssembly assmPath |> bind (fun assm ->
                let o = createOpts assm m
                Right(createOpts assm m)))

let loadOpts opt1 opt2 opt3 opt4 =
    let optEithers = [opt1; opt2; opt3; opt4] |> List.map parseOption
    let optList = collect optEithers
    optList 
        |> bind (fun pairs -> Map.ofList pairs |> Right)
        |> bind loadOptMap

let loadOptFile path =
    tryLoadOptionFileMap path
    |> mapLeft (fun err -> [err])
    |> bind loadOptMap
