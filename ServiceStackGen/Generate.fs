module ServiceStackGen.Generate

open System
open System.Reflection
open System.CodeDom

type GenerationOptions =
    {
        source: Type;
        targetTypeName: string;
        targetNamespace: string
    }
    static member fromType(t: Type) = { source = t; targetTypeName = t.Name + "Expected"; targetNamespace = "ServiceStackGen.Tests.Examples" }

let private serviceStackInterfaceNs = "ServiceStack.ServiceInterface"

let private camelCase (str: string) = (str.[0] |> (Char.ToLowerInvariant >> string)) + str.Substring(1)

let private addNamespaces opts (compileUnit: CodeCompileUnit) =
    let codeDomNamespace = new CodeNamespace(opts.targetNamespace)
    let globalNamespace = new CodeNamespace()
    globalNamespace.Imports.Add(new CodeNamespaceImport(serviceStackInterfaceNs))

    compileUnit.Namespaces.Add(globalNamespace) |> ignore
    compileUnit.Namespaces.Add(codeDomNamespace) |> ignore
    globalNamespace.Comments.Clear()
    codeDomNamespace

let private addConstructor opts (typeDecl: CodeTypeDeclaration) =
    //create public constructor which takes an argument of the source type as a parameter
    let constructor = new CodeConstructor()
    constructor.Attributes = MemberAttributes.Public

    let paramName = camelCase opts.source.Name 
    constructor.Parameters.Add(new CodeParameterDeclarationExpression(opts.source, paramName))

    //add a field to contain the constructor param and assign it in the constructor body
    let fieldName = "_" + paramName;
    let fieldDecl = new CodeMemberField(opts.source, fieldName)
    typeDecl.Members.Add(fieldDecl)

    let fieldRef = new CodeTypeReferenceExpression(fieldName)
    let assign = new CodeAssignStatement(fieldRef, new CodeArgumentReferenceExpression(paramName))
    constructor.Statements.Add(assign)

let GenerateUnit(opts: GenerationOptions) =
    let compileUnit = new CodeCompileUnit()

    //add namespaces
    let ns = addNamespaces opts compileUnit

    //declare service type
    let serviceTypeDecl = new CodeTypeDeclaration(opts.targetTypeName)
    ns.Types.Add(serviceTypeDecl)

    //add base types
    serviceTypeDecl.BaseTypes.Add(typeof<ServiceStack.ServiceInterface.Service>)

    //add constructor
    addConstructor opts serviceTypeDecl |> ignore


    compileUnit