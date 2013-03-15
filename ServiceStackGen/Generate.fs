module ServiceStackGen.Generate

open System
open System.Reflection
open System.CodeDom
open Utils
open CodeModel
open TypeParser
open CodeGen

type RequestResponseType = { RequestType: Type; ResponseType: Type }
type private RequestInfo = { TypeName: string; Parameters: ParameterInfo array }
type private ResponseInfo = { TypeName: string; ReturnType: Type option }
type private RequestResponseInfo = { Request: RequestInfo; Response: ResponseInfo }

type GenerationOptions =
    {
        source: Type;
        targetTypeName: string;
        targetNamespace: string
    }
    static member fromType(t: Type) = { source = t; targetTypeName = t.Name + "Expected"; targetNamespace = "ServiceStackGen.Tests.Examples" }

let private serviceStackInterfaceNs = "ServiceStack.ServiceInterface"
let private serviceMemberName = "_service"
let private responseTypeResultPropertyName = "Result"

let private addNamespaces opts (compileUnit: CodeCompileUnit) =
    let codeDomNamespace = new CodeNamespace(opts.targetNamespace)
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

let serviceFromOptions { source = serviceType; targetTypeName = name } =
    { MemberName = "_service"; TargetTypeName = name; Type = serviceType; Methods = [] }

let GenerateUnit(opts: GenerationOptions) =
    let compileUnit = new CodeCompileUnit()

    //add namespaces
    let ns = addNamespaces opts compileUnit
    let service = serviceFromOptions opts

    let serviceModel = parseService opts.source
    let decorated = decorateSerialisation serviceModel
    genServiceTypes decorated |> Seq.iter (fun typeDecl -> ns.Types.Add(typeDecl) |> ignore)

    //get service methods and generate request and response types
    //let methods = opts.source.GetMethods(BindingFlags.Public ||| BindingFlags.Instance ||| BindingFlags.DeclaredOnly)
    //TODO: Generate AnyMethod for each method on the service type
//    let anyMethods = Array.map (fun mi -> ())
//    methods |> Array.iter(fun m ->
//        let requestInfo = { TypeName = m.Name; Parameters = m.GetParameters() }
//        let responseInfo = { TypeName = m.Name + "Result"; ReturnType = if m.ReturnType = typeof<System.Void> then None else Some(m.ReturnType) }
//        let rrInfo = { Request = requestInfo; Response = responseInfo }
//        let requestTypeDecl = genRequestType rrInfo |> decorate
//        let responseTypeDecl = genResponseType m |> decorate
//        let anyMethod = genAnyMethod m requestTypeDecl responseTypeDecl
//
//        ns.Types.Add(requestTypeDecl) |> ignore
//        ns.Types.Add(responseTypeDecl) |> ignore
//        serviceTypeDecl.Members.Add(anyMethod) |> ignore
//    )

    compileUnit