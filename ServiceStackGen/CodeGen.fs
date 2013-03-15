module ServiceStackGen.CodeGen
open Utils
open CodeModel
open System.CodeDom

let private responseTypeResultPropertyName = "Result"
let private genServiceMembers { Type = serviceType; MemberName = memberName } : CodeTypeMember =
    new CodeMemberField(serviceType, memberName) :> CodeTypeMember

let private genConstructor { Type = serviceType; MemberName = memberName } : CodeTypeMember =
    //create public constructor which takes an argument of the source type as a parameter
    let ctor = new CodeConstructor()
    ctor.Attributes <- MemberAttributes.Public

    let paramName = "service"
    ctor.Parameters.Add(new CodeParameterDeclarationExpression(serviceType, paramName)) |> ignore

    //assign service member field in the constructor body
    let fieldRef = new CodeTypeReferenceExpression(memberName)
    let assign = new CodeAssignStatement(fieldRef, new CodeArgumentReferenceExpression(paramName))
    ctor.Statements.Add(assign) |> ignore
    ctor :> CodeTypeMember

let private requestResponseTypeNames = function
| Request(reqName, _, _, Empty(respName, _)) -> (reqName, respName)
| Request(reqName, _, _, Value(respName, _, _)) -> (reqName, respName)

let private getResponseType = function
| Request(_, _, _, r) -> r

let genAnyMethod (anyMethod : AnyMethod) (service : Service) =
    let methodDecl = new CodeMemberMethod()
    let (requestTypeName, responseTypeName) = requestResponseTypeNames anyMethod.Request
    methodDecl.Name <- "Any"
    methodDecl.Attributes <- MemberAttributes.Public
    let responseTypeRef = new CodeTypeReference(responseTypeName)
    let responseLocalName = "@return"

    let requestParamName = "request"
    methodDecl.Parameters.Add(new CodeParameterDeclarationExpression(requestTypeName, requestParamName)) |> ignore
    methodDecl.ReturnType <- responseTypeRef

    let requestParamRef = new CodeTypeReferenceExpression(requestParamName)
    let serviceFieldRef = new CodeTypeReferenceExpression(service.MemberName)

    //create local variable for the response
    let responseDecl = new CodeVariableDeclarationStatement(responseTypeRef, responseLocalName, new CodeObjectCreateExpression(responseTypeRef))
    let responseVarRef = new CodeVariableReferenceExpression(responseLocalName)

    methodDecl.Statements.Add(responseDecl) |> ignore

    //create invocation expression
    let paramExprs = anyMethod.InvocationProperties |> Array.map (fun prop -> new CodePropertyReferenceExpression(requestParamRef, prop) :> CodeExpression)
    let invocationExpr = new CodeMethodInvokeExpression(serviceFieldRef, anyMethod.ServiceMethodName, paramExprs)

    //if the method is void then just invoke it. If it has a return value, assign it to the result property of the response
    let invokeStm  = match (getResponseType anyMethod.Request) with
                        | Empty(_) -> new CodeExpressionStatement(invocationExpr) :> CodeStatement
                        | Value(_, resultProp, _) ->
                            let propRefExpr = new CodePropertyReferenceExpression(responseVarRef, resultProp.Name)
                            new CodeAssignStatement(propRefExpr, invocationExpr) :> CodeStatement

    methodDecl.Statements.Add(invokeStm) |> ignore
    methodDecl.Statements.Add(new CodeMethodReturnStatement(responseVarRef)) |> ignore

    methodDecl :> CodeTypeMember

let private decorateAttributes<'T when 'T :> CodeTypeMember> (attributeTypes : System.Type list) (decl : 'T)  =
    attributeTypes |> List.map (fun t -> new CodeAttributeDeclaration(t.FullName))
         |> List.iter (decl.CustomAttributes.Add >> ignore)
    decl

let private genPropertyMembers (prop : Property) =
    let backingFieldName = "_" + camelCase prop.Name 
    let backingField = new CodeMemberField(prop.Type, backingFieldName)
    let thisRef = new CodeThisReferenceExpression()
    let fieldRef = new CodeFieldReferenceExpression(thisRef, backingFieldName)

    let propDecl = new CodeMemberProperty()
    propDecl.Name <- prop.Name
    propDecl.Type <- new CodeTypeReference(prop.Type)
    propDecl.Attributes <- MemberAttributes.Public
    propDecl.HasGet <- true
    propDecl.HasSet <- true

    //create getter/setter bodies
    propDecl.GetStatements.Add(new CodeMethodReturnStatement(fieldRef)) |> ignore
    propDecl.SetStatements.Add(new CodeAssignStatement(fieldRef, new CodePropertySetValueReferenceExpression())) |> ignore

    let decorated = decorateAttributes  prop.Attributes propDecl

    [backingField :> CodeTypeMember; decorated :> CodeTypeMember]

let private getResponseTypeName = function Empty(name, _) -> name | Value(name, _, _) -> name

let private genMembers (service : Service) =
    let genFuns = [genConstructor ; genServiceMembers]
    let genAnys = List.map genAnyMethod service.Methods

    List.map ((|>) service) (genFuns @ genAnys)

let private addMembers members (typeDecl : CodeTypeDeclaration) =
    Seq.iter (typeDecl.Members.Add >> ignore) members
    typeDecl

let private getIRequestDecl (reqTypeName: string) =
    let ifaceType = typedefof<ServiceStack.ServiceHost.IReturn<_>>
    let typeRef = new CodeTypeReference(ifaceType)
    typeRef.TypeArguments.Add(reqTypeName) |> ignore
    typeRef

let genResponseType = function
| Empty(name, attrs) -> new CodeTypeDeclaration(name) |> decorateAttributes attrs
| Value(name, resultProp, attrs) -> new CodeTypeDeclaration(name) |> (addMembers (genPropertyMembers resultProp)) |> decorateAttributes attrs

let genRequestType = function
| Request(name, properties, attrs, response) ->
    let typeDecl = new CodeTypeDeclaration(name)
    let ireqType = response |> getResponseTypeName |> getIRequestDecl 
    typeDecl.BaseTypes.Add(ireqType) |> ignore
    let propertyMembers = List.collect genPropertyMembers properties
    addMembers propertyMembers typeDecl |> decorateAttributes attrs

let genService (service : Service) =
    let serviceTypeDecl = new CodeTypeDeclaration(service.TargetTypeName)
    serviceTypeDecl.BaseTypes.Add(typeof<ServiceStack.ServiceInterface.Service>)
    genMembers service |> Seq.iter (fun m -> serviceTypeDecl.Members.Add(m) |> ignore)

    serviceTypeDecl

let genMethodDTOs (m : AnyMethod) =
    let (req, resp) = getMethodDTOs m
    [genRequestType req; genResponseType resp]

let genServiceTypes (service : Service) =
    let serviceTypeDecl = genService service
    let dtoTypes = service.Methods |> List.collect genMethodDTOs
    serviceTypeDecl :: dtoTypes

