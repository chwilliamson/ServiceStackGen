module ServiceStackGen.CodeModel
open System
open System.Reflection

type Property = { Name : string; Attributes : Type list; Type : Type }
type DTO = { Name : string; Attributes : Type list; Properties : Property list }
type Method = { Name : string; Parameters : ParameterInfo array; ReturnType : Type option }

let addDataMemberAttr ({ Attributes = attrs } as p : Property) =
    { p with Attributes = typeof<System.Runtime.Serialization.DataMemberAttribute> :: attrs }

let addDataContractAttr ({ Attributes = attrs } as dto : DTO) =
    { dto with Attributes = typeof<System.Runtime.Serialization.DataContractAttribute> :: attrs }

let decorateSerialisation ({ Properties = props } as dto) =
    { dto with Properties = List.map addDataMemberAttr props } |> addDataContractAttr