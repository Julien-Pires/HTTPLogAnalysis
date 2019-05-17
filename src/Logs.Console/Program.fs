open System
open FSharp.Control
open Logs

let defaultPath = "/temp/access.log"

[<EntryPoint>]
let main argv =
    File.readContinuously defaultPath
    |> AsyncSeq.iter (fun c -> Console.WriteLine(LogParser.parse c))
    |> Async.Start

    Console.ReadLine() |> ignore
    0 // return an integer exit code
