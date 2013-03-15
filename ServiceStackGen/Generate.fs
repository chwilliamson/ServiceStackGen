﻿module ServiceStackGen.Generate

open System
open System.Reflection
open System.CodeDom

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

let private transformFirst (f: char -> char) (str: string) =
    if str.Length = 0 then str
    else let first = str.[0] |> f |> string in first + str.Substring(1)

let private camelCase = transformFirst Char.ToLowerInvariant
let private pascalCase = transformFirst Char.ToUpperInvariant

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
    let ctor = new CodeConstructor()
    ctor.Attributes <- MemberAttributes.Public

    let paramName = camelCase opts.source.Name 
    ctor.Parameters.Add(new CodeParameterDeclarationExpression(opts.source, paramName)) |> ignore

    //add a field to contain the constructor param and assign it in the constructor body
    let fieldDecl = new CodeMemberField(opts.source, serviceMemberName)
    typeDecl.Members.Add(fieldDecl) |> ignore

    let fieldRef = new CodeTypeReferenceExpression(serviceMemberName)
    let assign = new CodeAssignStatement(fieldRef, new CodeArgumentReferenceExpression(paramName))
    ctor.Statements.Add(assign) |> ignore

    typeDecl.Members.Add(ctor) |> ignore

let createPropertyDecl (name: string) (propertyType: Type) =
    let backingFieldName = "_" + camelCase name
    let backingField = new CodeMemberField(propertyType, backingFieldName)
    let thisRef = new CodeThisReferenceExpression()
    let fieldRef = new CodeFieldReferenceExpression(thisRef, backingFieldName)

    let propDecl = new CodeMemberProperty()
    propDecl.Name <- name
    propDecl.Type <- new CodeTypeReference(propertyType)
    propDecl.Attributes <- MemberAttributes.Public
    propDecl.HasGet <- true
    propDecl.HasSet <- true

    //create getter/setter bodies
    propDecl.GetStatements.Add(new CodeMethodReturnStatement(fieldRef)) |> ignore
    propDecl.SetStatements.Add(new CodeAssignStatement(fieldRef, new CodePropertySetValueReferenceExpression())) |> ignore

    (backingField, propDecl)

let genResponseType(methodInfo: MethodInfo) =
    let typeName = methodInfo.Name + "Result"
    let typeDecl = new CodeTypeDeclaration(typeName)
    if methodInfo.ReturnType <> typeof<System.Void> then
        let (field, resultProperty) = createPropertyDecl responseTypeResultPropertyName methodInfo.ReturnType
        typeDecl.Members.Add(field) |> ignore
        typeDecl.Members.Add(resultProperty) |> ignore
    typeDecl

let getIRequestDecl (reqTypeName: string) =
    let ifaceType = typedefof<ServiceStack.ServiceHost.IReturn<_>>
    let typeRef = new CodeTypeReference(ifaceType)
    typeRef.TypeArguments.Add(reqTypeName) |> ignore
    typeRef

let private genRequestType { Request = { TypeName = typeName; Parameters = parameters }; Response = { TypeName = responseTypeName } } =
    let typeDecl = new CodeTypeDeclaration(typeName)
    let ireturnInterfaceDecl = getIRequestDecl responseTypeName
    typeDecl.BaseTypes.Add(ireturnInterfaceDecl) |> ignore
    parameters |> Array.iter(fun param ->
        let (field, paramProp) = createPropertyDecl (pascalCase param.Name) param.ParameterType
        typeDecl.Members.Add(field) |> ignore
        typeDecl.Members.Add(paramProp) |> ignore
    )
    typeDecl

let genAnyMethod (methodInfo: MethodInfo) (request: CodeTypeDeclaration) (response: CodeTypeDeclaration) =
    let methodDecl = new CodeMemberMethod()
    methodDecl.Name <- "Any"
    methodDecl.Attributes <- MemberAttributes.Public

    let requestParamName = "request"
    methodDecl.Parameters.Add(new CodeParameterDeclarationExpression(request.Name, requestParamName)) |> ignore
    methodDecl.ReturnType <- new CodeTypeReference(response.Name)

    let requestParamRef = new CodeTypeReferenceExpression(requestParamName)
    let serviceFieldRef = new CodeTypeReferenceExpression(serviceMemberName)

    //create local variable for the response
    let responseTypeRef = new CodeTypeReference(response.Name)
    let responseDecl = new CodeVariableDeclarationStatement(responseTypeRef, "@return", new CodeObjectCreateExpression(responseTypeRef))
    let responseVarRef = new CodeVariableReferenceExpression("@return")

    methodDecl.Statements.Add(responseDecl) |> ignore

    //create invocation expression
    let paramExprs = methodInfo.GetParameters() |> Array.map (fun p -> new CodePropertyReferenceExpression(requestParamRef, pascalCase p.Name) :> CodeExpression)
    let invocationExpr = new CodeMethodInvokeExpression(serviceFieldRef, methodInfo.Name, paramExprs)

    //if the method is void then just invoke it. If it has a return value, assign it to the result property of the response
    let invokeStm  = if methodInfo.ReturnType = typeof<System.Void> then
                        new CodeExpressionStatement(invocationExpr) :> CodeStatement
                     else
                        let propRefExpr = new CodePropertyReferenceExpression(responseVarRef, responseTypeResultPropertyName)
                        new CodeAssignStatement(propRefExpr, invocationExpr) :> CodeStatement

    methodDecl.Statements.Add(invokeStm) |> ignore
    methodDecl.Statements.Add(new CodeMethodReturnStatement(responseVarRef)) |> ignore

    methodDecl

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

let GenerateUnit(opts: GenerationOptions) =
    let compileUnit = new CodeCompileUnit()

    //add namespaces
    let ns = addNamespaces opts compileUnit

    //declare service type
    let serviceTypeDecl = new CodeTypeDeclaration(opts.targetTypeName)
    ns.Types.Add(serviceTypeDecl) |> ignore

    //add base types
    serviceTypeDecl.BaseTypes.Add(typeof<ServiceStack.ServiceInterface.Service>)

    //add constructor
    addConstructor opts serviceTypeDecl |> ignore

    //get service methods and generate request and response types
    let methods = opts.source.GetMethods(BindingFlags.Public ||| BindingFlags.Instance ||| BindingFlags.DeclaredOnly)
    methods |> Array.iter(fun m ->
        let requestInfo = { TypeName = m.Name; Parameters = m.GetParameters() }
        let responseInfo = { TypeName = m.Name + "Result"; ReturnType = if m.ReturnType = typeof<System.Void> then None else Some(m.ReturnType) }
        let rrInfo = { Request = requestInfo; Response = responseInfo }
        let requestTypeDecl = genRequestType rrInfo |> decorate
        let responseTypeDecl = genResponseType m |> decorate
        let anyMethod = genAnyMethod m requestTypeDecl responseTypeDecl

        ns.Types.Add(requestTypeDecl) |> ignore
        ns.Types.Add(responseTypeDecl) |> ignore
        serviceTypeDecl.Members.Add(anyMethod) |> ignore
    )

    compileUnit