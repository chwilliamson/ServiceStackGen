module ServiceStackGen.Utils
open System

let private transformFirst (f: char -> char) (str: string) =
    if str.Length = 0 then str
    else let first = str.[0] |> f |> string in first + str.Substring(1)

let camelCase = transformFirst Char.ToLowerInvariant
let pascalCase = transformFirst Char.ToUpperInvariant
let toList (a, b) = [a;b]