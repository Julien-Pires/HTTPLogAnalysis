namespace Logs.Console

open FParsec

type Parser<'a> = Parser<'a, unit>

/// <summary>
/// Contains methods to parse arguments passed to the application with format -x:value
/// </summary>
module ArgumentsParser =
    let private parseArgumentKey : Parser<_> =
        skipChar '-' >>. anyChar

    let private parseArgumentValue : Parser<_> =
        skipChar ':' >>. manyChars (noneOf [' '])

    let private parseArgument : Parser<_> =
        parseArgumentKey .>>. parseArgumentValue

    let parse args =
        args
        |> Seq.map (run parseArgument)
        |> Seq.choose (function
            | Success (x, _, _) -> Some x
            | _ -> None)
        |> Map.ofSeq