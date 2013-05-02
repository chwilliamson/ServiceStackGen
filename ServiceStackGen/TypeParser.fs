module ServiceStackGen.TypeParser
open System
open System.Reflection

open CodeModel
open GenerationOptions
open Utils

let private toRequestProperty (param : ParameterInfo) =
    { Name = pascalCase param.Name; Attributes = []; Type = param.ParameterType }

let parseDTOs (methodInfo : MethodInfo) =
    let requestTypeName = methodInfo.Name
    let responseTypeName = methodInfo.Name + "Result"
    
    let responseType = if methodInfo.ReturnType = typeof<System.Void> then Empty(responseTypeName, []) 
                       else Value(responseTypeName, { Name = "Result"; Attributes = []; Type = methodInfo.ReturnType }, [])
    let requestProperties = Array.map toRequestProperty (methodInfo.GetParameters()) |> List.ofArray
    Request(requestTypeName, requestProperties, [], responseType)

let toAnyMethod (mi : MethodInfo) =
    { Request = parseDTOs mi; ServiceMethodName = mi.Name; InvocationProperties = mi.GetParameters() |> Array.map (fun p -> pascalCase p.Name) }

let parseService { ServiceType = serviceType; TargetTypeName = typeName } =
    let methods = serviceType.GetMethods(BindingFlags.Public ||| BindingFlags.Instance ||| BindingFlags.DeclaredOnly)
    let serviceAnyMethods = Array.map toAnyMethod methods |> List.ofArray
    { MemberName = "_service"; Type = serviceType; TargetTypeName = typeName; Methods = serviceAnyMethods }
