namespace TypedConfig

open FSharp.Core
open FSharp.Reflection
open System
open System.Collections.Generic

type ConfigError =
    | MissingVariable of name: string
    | InvalidValue of name: string * value: string * expected: string
    | UnsupportedType of type': string

[<RequireQualifiedAccess>]
module internal Reader =
    let tryGet (varName: string) =
        System.Environment.GetEnvironmentVariable varName |> Option.ofObj

[<RequireQualifiedAccess>]
module internal Parser =
    type private ParserStrategy = string -> string -> Result<obj, ConfigError>

    let private parseWith expectedType tryParse name (value: string) =
        match tryParse value with
        | true, parsed -> Ok (box parsed)
        | _ -> Error (InvalidValue (name, value, expectedType))

    let private parseString _name value = Ok (box value)
    let private parseInt = parseWith "int" System.Int32.TryParse
    let private parseBool = parseWith "bool" System.Boolean.TryParse

    let private strategies : IDictionary<Type, ParserStrategy> =
        dict [
            (typeof<string>, parseString)
            (typeof<int>, parseInt)
            (typeof<bool>, parseBool)
        ]

    let parse (t: Type) name (value: string) =
        match strategies.TryGetValue t with
        | true, parser -> parser name value
        | false, _ -> Error (UnsupportedType t.Name)

[<RequireQualifiedAccess>]
module internal Builder =
    let private buildValueResult (field: Reflection.PropertyInfo) =
        let name = field.Name

        match Reader.tryGet name with
        | None -> Error (MissingVariable name)
        | Some value -> Parser.parse field.PropertyType name value

    let private buildRecord<'T> (values: obj array) : 'T =
        FSharpValue.MakeRecord (typeof<'T>, values) :?> 'T

    let build<'T> (fields: Reflection.PropertyInfo array) : Result<'T, ConfigError list> =
        let folder (valuesRev, errorsRev) field =
            match buildValueResult field with
            | Ok value -> (value :: valuesRev, errorsRev)
            | Error err -> (valuesRev, err :: errorsRev)

        let toResult (valuesRev, errorsRev) =
            match errorsRev with
            | [] -> valuesRev |> List.rev |> List.toArray |> Ok
            | _ -> errorsRev |> List.rev |> Error

        fields
        |> Array.fold folder ([], [])
        |> toResult
        |> Result.map buildRecord<'T>

[<RequireQualifiedAccess>]
module Api =
    let load<'T> () : Result<'T, ConfigError list> =
        let t = typeof<'T>
        let fields = FSharpType.GetRecordFields t

        Builder.build<'T> fields
