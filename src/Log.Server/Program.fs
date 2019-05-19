open System
open FSharp.Control
open Logs
open Logs.Server

let waitResponse () =
    let rec loop () =
        Console.WriteLine("Enter a number to define log speed, or enter 'q' to exit the program.")
        let response = Console.ReadLine()
        match response with
        | "q" -> ()
        | x -> loop()
    loop()

let defaultPath = "/temp/access.log"

[<EntryPoint>]
let main argv =
    printfn "HTTP Access log simulator"
    File.writeContinuously defaultPath (LogFactory.Logs())
    |> Async.Start

    waitResponse()
    0
