open System
open FSharp.Control
open Logs
open FParsec

let defaultPath = "/temp/access.log"

[<EntryPoint>]
let main _ =
    let repository = StatisticsRepository()

    let statistics = StatisticsAgent([
        { Name = "most_section_hit"; Computation = RankingComputation() }])

    let sub =
        statistics.AsObservable
        |> Observable.subscribe repository.Update
    
    File.readContinuously defaultPath
    |> AsyncSeq.map LogParser.parse
    |> AsyncSeq.choose (function | Success(x, _, _) -> Some x | _ -> None)
    |> AsyncSeq.iter statistics.Receive
    |> Async.Start

    Console.ReadLine() |> ignore
    sub.Dispose()
    0 // return an integer exit code
