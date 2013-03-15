module ServiceStackGen.CodeModel
open System
open System.Reflection

type Property = { Name : string; Attributes : Type list; Type : Type }

type ResponseType = 
    | Empty of String * Type list
    | Value of String * Property * Type list

type RequestType =
    | Request of String * Property list * Type list * ResponseType

type DTOType = Req of RequestType | Resp of ResponseType
type DTO = { Type : DTOType; Attributes : Type list }

type AnyMethod = { Request : RequestType; ServiceMethodName : String; InvocationProperties : String array }
type Service =
    {
        MemberName : String;
        Type : Type;
        TargetTypeName : String;
        Methods : AnyMethod list
    }

let addAttribute f ({ Attributes = attrs } as p : Property) =
    { p with Attributes = (f p) :: attrs }

let addResponsePropertyAttributes f = function
| Empty(_, _) as e -> e
| Value(name, p, attrs) -> Value(name, addAttribute f p, attrs)

let addRequestPropertyAttributes f = function
| Request(name, props, attrs, resp) -> Request(name, List.map (addAttribute f) props, attrs, addResponsePropertyAttributes f resp)

let addResponseAttribute attrType = function
| Empty(name, attrs) -> Empty(name, attrType :: attrs)
| Value(name, p, attrs) -> Value(name, p, attrType :: attrs)

let addDTOAttribute attrType = function
| Request(name, props, attrs, resp) -> Request(name, props, attrType :: attrs, addResponseAttribute attrType resp)

let addDataMemberAttr ({ Attributes = attrs } as p : Property) =
    { p with Attributes = typeof<System.Runtime.Serialization.DataMemberAttribute> :: attrs }

let addDataContractAttr ({ Attributes = attrs } as dto : DTO) =
    { dto with Attributes = typeof<System.Runtime.Serialization.DataContractAttribute> :: attrs }

let getMethodDTOs (m : AnyMethod) =
    match m.Request with
    | Request(_, _, _, resp) as req -> (req, resp)

let mapDTOs f (service : Service) =
    { service with Methods = service.Methods |> List.map (fun m -> { m with Request = f m.Request })}
    
let decorateSerialisation (service : Service) =
    service |> mapDTOs ((addDTOAttribute typeof<System.Runtime.Serialization.DataContractAttribute>) >> addRequestPropertyAttributes (fun _ -> typeof<System.Runtime.Serialization.DataMemberAttribute>))