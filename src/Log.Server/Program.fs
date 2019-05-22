open System
open System.Threading
open FSharp.Control
open Logs
open Logs.Server

let defaultPath = "/temp/access.log"
let defaultInterval = 100
let defaultRequests = 1

[<EntryPoint>]
let main argv =
    let argsMap = ArgumentsParser.parse argv

    printfn "HTTP Access log simulator"

    let mutable sourceToken = new CancellationTokenSource()

    let path = match argsMap |> Map.tryFind 'f' with | Some x -> x | None -> defaultPath
    let generateLogs interval requests =
        let work () = async {
            let logsGenerator = LogFactory.Logs interval requests
            do! File.writeContinuously path logsGenerator }
        let source = new CancellationTokenSource()
        Async.Start(work(), source.Token)
        source

    let waitResponse () =
        let rec loop () =
            Console.WriteLine("Enter one of the following entry to change requests creation:")
            Console.WriteLine("    [interval requests] - Generate a number of requests for the given interval in ms (e.g: 100 10")
            Console.WriteLine("    [q] - Exit the application")
            let response = Console.ReadLine()
            match response with
            | "q" ->
                sourceToken.Cancel()
                ()
            | x -> 
                match x.Split(' ') with
                | [| interval; requests|] ->
                    match (Int32.TryParse interval, Int32.TryParse requests) with
                    | (true, x), (true, y) ->
                        sourceToken.Cancel()
                        sourceToken <- generateLogs x y
                        loop()
                    | _ -> loop()
                | _ -> loop()
        loop()

    waitResponse ()
    0
