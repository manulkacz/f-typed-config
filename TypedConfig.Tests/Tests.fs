module TypedConfig.Tests

open System
open Expecto
open TypedConfig

type AppConfig = { APP_NAME: string; PORT: int; DEBUG: bool }
type InvalidConfig = { PORT: int; DEBUG: bool }
type UnsupportedConfig = { STARTED_AT: DateTime }

module private Env =
    let private gate = obj ()

    let withVars vars test =
        lock gate (fun () ->
            let previous =
                vars
                |> List.map (fun (name, _) -> name, Environment.GetEnvironmentVariable name)

            try
                vars
                |> List.iter (fun (name, value) -> Environment.SetEnvironmentVariable (name, value))

                test ()
            finally
                previous
                |> List.iter (fun (name, value) -> Environment.SetEnvironmentVariable (name, value)))

    let assertConfig (expected: AppConfig) (actual: Result<AppConfig, ConfigError list>) =
        match actual with
        | Ok config -> Expect.equal config expected "Expected config to match"
        | Error errors -> failwithf "Expected Ok, got Error %A" errors

    let assertErrors<'T> (expected: ConfigError list) (actual: Result<'T, ConfigError list>) =
        match actual with
        | Ok value -> failwithf "Expected Error, got Ok %A" value
        | Error errors -> Expect.equal errors expected "Expected errors to match"

[<Tests>]
let tests =
    testList "TypedConfig" [
        testList "Api" [
                testCase "load builds a strongly typed record from environment variables"
                <| fun _ ->
                    Env.withVars [ "APP_NAME", "typed-config"; "PORT", "5432"; "DEBUG", "true" ] (fun () ->
                        let result = Api.load<AppConfig> ()

                        Env.assertConfig { APP_NAME = "typed-config"; PORT = 5432; DEBUG = true } result)

                testCase "load reports all missing variables"
                <| fun _ ->
                    Env.withVars [ "APP_NAME", null; "PORT", null; "DEBUG", null ] (fun () ->
                        let result = Api.load<AppConfig> ()

                        Env.assertErrors
                            [ MissingVariable "APP_NAME"; MissingVariable "PORT"; MissingVariable "DEBUG" ]
                            result)

                testCase "load reports invalid int and bool values"
                <| fun _ ->
                    Env.withVars [ "PORT", "not-a-port"; "DEBUG", "maybe" ] (fun () ->
                        let result = Api.load<InvalidConfig> ()

                        Env.assertErrors
                            [
                                InvalidValue ("PORT", "not-a-port", "int")
                                InvalidValue ("DEBUG", "maybe", "bool")
                            ]
                            result)

                testCase "load accumulates missing and invalid value errors"
                <| fun _ ->
                    Env.withVars [ "PORT", "not-a-port"; "DEBUG", null ] (fun () ->
                        let result = Api.load<InvalidConfig> ()

                        Env.assertErrors [ InvalidValue ("PORT", "not-a-port", "int"); MissingVariable "DEBUG" ] result)

                testCase "load reports unsupported field type"
                <| fun _ ->
                    Env.withVars [ "STARTED_AT", "2026-01-01T00:00:00Z" ] (fun () ->
                        let result = Api.load<UnsupportedConfig> ()

                        Env.assertErrors [ UnsupportedType "DateTime" ] result)
            ]

        testList "Parser" [
                testCase "parse returns boxed string values unchanged"
                <| fun _ ->
                    let result = Parser.parse typeof<string> "NAME" "typed-config"

                    Expect.equal result (Ok (box "typed-config")) "Expected string value to be boxed unchanged"

                testCase "parse returns invalid value for malformed int"
                <| fun _ ->
                    let result = Parser.parse typeof<int> "PORT" "abc"

                    Expect.equal result (Error (InvalidValue ("PORT", "abc", "int"))) "Expected invalid int error"

                testCase "parse returns invalid value for malformed bool"
                <| fun _ ->
                    let result = Parser.parse typeof<bool> "DEBUG" "yes"

                    Expect.equal result (Error (InvalidValue ("DEBUG", "yes", "bool"))) "Expected invalid bool error"

                testCase "parse returns unsupported type error for non-supported types"
                <| fun _ ->
                    let result = Parser.parse typeof<DateTime> "STARTED_AT" "2026-01-01T00:00:00Z"

                    Expect.equal result (Error (UnsupportedType "DateTime")) "Expected unsupported type error"
            ]
    ]
