module ServiceStackGen.Utils
open System

let private transformFirst (f: char -> char) (str: string) =
    if str.Length = 0 then str
    else let first = str.[0] |> f |> string in first + str.Substring(1)

let andp p1 p2 = (fun a -> p1 a && p2 a)
let allp pl = List.fold andp (fun _ -> true) pl

let camelCase = transformFirst Char.ToLowerInvariant
let pascalCase = transformFirst Char.ToUpperInvariant
let toList (a, b) = [a;b]
let mapKeys m = m |> Map.toList |> List.map fst |> Set.ofList

type Either<'a, 'b> = Left of 'a | Right of 'b
let mapLeft f = function
| Left(l) -> Left(f l)
| Right(r) -> Right(r)

let mapRight f = function
| Left(l) -> Left(l)
| Right(r) -> Right(f r)

let either lf rf = function
| Left(l) -> lf l
| Right(r) -> rf r

let bind f = function
| Left(l) -> Left(l)
| Right(r) -> f r

let collect (eithers: Either<'a, 'b> list) =
    let combine e (acc: Either<'a list, 'b list>) =
        match (e, acc) with
        | (Left(l), Left(ls)) -> Left(l::ls)
        | (Left(l), Right(_)) -> Left([l])
        | (Right(r), Left(ls) as l) -> Left(ls)
        | (Right(r), Right(rs)) -> Right(r::rs)
    List.foldBack combine eithers (Right([]))
