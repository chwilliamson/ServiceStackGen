module ServiceStackGen.TypeParser
open System
open System.Reflection

open CodeModel
open GenerationOptions
open Utils

let internal getRequestResponseTypeNames (serviceName: string) (methodName: string) =
    let requestTypeName = sprintf "%s_%s" serviceName methodName
    let responseTypeName = sprintf "%s_%sResult" serviceName methodName
    (requestTypeName, responseTypeName)

let private toRequestProperty (param : ParameterInfo) =
    { Name = pascalCase param.Name; Attributes = []; Type = param.ParameterType }

let parseDTOs (serviceName: string) (methodInfo : MethodInfo) =
    let (requestTypeName, responseTypeName) = getRequestResponseTypeNames serviceName methodInfo.Name
    
    let responseType = if methodInfo.ReturnType = typeof<System.Void> then Empty(responseTypeName, []) 
                       else Value(responseTypeName, { Name = "Result"; Attributes = []; Type = methodInfo.ReturnType }, [])
    let requestProperties = Array.map toRequestProperty (methodInfo.GetParameters()) |> List.ofArray
    Request(requestTypeName, requestProperties, [], responseType)

let toAnyMethod (serviceName: string) (mi : MethodInfo) =
    { Request = parseDTOs serviceName mi; ServiceMethodName = mi.Name; InvocationProperties = mi.GetParameters() |> Array.map (fun p -> pascalCase p.Name) }

let parseService { ServiceType = serviceType; TargetTypeName = typeName } =
    let methods = serviceType.GetMethods(BindingFlags.Public ||| BindingFlags.Instance ||| BindingFlags.DeclaredOnly)
    let serviceAnyMethods = Array.map (toAnyMethod typeName) methods |> List.ofArray
    { MemberName = "_service"; Type = serviceType; TargetTypeName = typeName; Methods = serviceAnyMethods }
