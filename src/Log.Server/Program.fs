open System
open System.Threading
open FSharp.Control
open Logs
open Logs.Server

let waitResponse work =
    let rec loop () =
        Console.WriteLine("Enter one of the following entry to change request speed:")
        Console.WriteLine("    [interval requests] - Enter two separated number to generate a number of requests for the given interval")
        Console.WriteLine("    [q] - Exit the application")
        let response = Console.ReadLine()
        match response with
        | "q" -> ()
        | x -> loop()
    loop()

let defaultPath = "/temp/access.log"
let defaultInterval = 100
let defaultRequests = 1

[<EntryPoint>]
let main argv =
    printfn "HTTP Access log simulator"

    let work () = async {
        do! File.writeContinuously defaultPath (LogFactory.Logs defaultInterval defaultRequests) }

    let source = new CancellationTokenSource()
    Async.Start(work(), source.Token)

    waitResponse (fun _ -> source.Cancel())
    0
