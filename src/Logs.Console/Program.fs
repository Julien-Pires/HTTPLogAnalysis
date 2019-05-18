open System
open FSharp.Control
open Logs
open FParsec

let defaultPath = "/temp/access.log"

[<EntryPoint>]
let main argv =
    let statistics = StatisticsAgent([
        RankingStatistics()])

    File.readContinuously defaultPath
    |> AsyncSeq.map LogParser.parse
    |> AsyncSeq.choose (function | Success(x, _, _) -> Some x | _ -> None)
    |> AsyncSeq.iter statistics.Receive
    |> Async.Start

    Console.ReadLine() |> ignore
    0 // return an integer exit code
