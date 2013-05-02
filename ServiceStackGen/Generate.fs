module ServiceStackGen.Generate

open System
open System.Reflection
open System.CodeDom

open Utils
open CommandOptions
open GenerationOptions
open CodeModel
open TypeParser
open CodeGen

type RequestResponseType = { RequestType: Type; ResponseType: Type }
type private RequestInfo = { TypeName: string; Parameters: ParameterInfo array }
type private ResponseInfo = { TypeName: string; ReturnType: Type option }
type private RequestResponseInfo = { Request: RequestInfo; Response: ResponseInfo }

let private serviceStackInterfaceNs = "ServiceStack.ServiceInterface"
let private serviceMemberName = "_service"
let private responseTypeResultPropertyName = "Result"

let private addNamespaces opts (compileUnit: CodeCompileUnit) =
    let codeDomNamespace = new CodeNamespace(opts.TargetNamespace)
    let globalNamespace = new CodeNamespace()
    globalNamespace.Imports.Add(new CodeNamespaceImport(serviceStackInterfaceNs))

    compileUnit.Namespaces.Add(globalNamespace) |> ignore
    compileUnit.Namespaces.Add(codeDomNamespace) |> ignore
    globalNamespace.Comments.Clear()
    codeDomNamespace

let decorate (typeDecl: CodeTypeDeclaration) =
    typeDecl.CustomAttributes.Add(new CodeAttributeDeclaration(typeof<System.Runtime.Serialization.DataContractAttribute>.FullName)) |> ignore
    typeDecl.Members
        |> Seq.cast
        |> Seq.choose(fun m -> match box m with
                                | :? CodeMemberProperty as p -> Some(p)
                                | _ -> None)
        |> Seq.cast<CodeMemberProperty>
        |> Seq.iter(fun p -> p.CustomAttributes.Add(new CodeAttributeDeclaration(typeof<System.Runtime.Serialization.DataMemberAttribute>.FullName)) |> ignore)
    typeDecl

let serviceFromOptions (serviceType: Type) (targetNamespace: string) =
    { MemberName = "_service"; TargetTypeName = targetNamespace; Type = serviceType; Methods = [] }

let GenerateUnit (opts: Options) (genOptions: GenerationOptions) =
    let compileUnit = new CodeCompileUnit()

    //add namespaces
    let ns = addNamespaces opts compileUnit

    let serviceModel = parseService genOptions
    let decorated = decorateSerialisation serviceModel
    genServiceTypes decorated |> Seq.iter (fun typeDecl -> ns.Types.Add(typeDecl) |> ignore)

    compileUnit