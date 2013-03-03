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
    let propDecl = new CodeMemberProperty()
    propDecl.Name <- name
    propDecl.Type <- new CodeTypeReference(propertyType)
    propDecl.Attributes <- MemberAttributes.Public
    propDecl.HasGet <- true
    propDecl.HasSet <- true
    propDecl

let genResponseType(methodInfo: MethodInfo) =
    let typeName = methodInfo.Name + "Result"
    let typeDecl = new CodeTypeDeclaration(typeName)
    if methodInfo.ReturnType <> typeof<System.Void> then
        let resultProperty = createPropertyDecl responseTypeResultPropertyName methodInfo.ReturnType
        typeDecl.Members.Add(resultProperty) |> ignore
    typeDecl

let genRequestType(methodInfo: MethodInfo) =
    let typeDecl = new CodeTypeDeclaration(methodInfo.Name)
    methodInfo.GetParameters() |> Array.iter(fun param ->
        let paramProp = createPropertyDecl (pascalCase methodInfo.Name) param.ParameterType
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
        let requestTypeDecl = genRequestType m
        let responseTypeDecl = genResponseType m
        let anyMethod = genAnyMethod m requestTypeDecl responseTypeDecl

        ns.Types.Add(requestTypeDecl) |> ignore
        ns.Types.Add(responseTypeDecl) |> ignore
        serviceTypeDecl.Members.Add(anyMethod) |> ignore
    )

    compileUnit