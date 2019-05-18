open System
open FSharp.Control
open FParsec
open Logs
open Logs.Console

let defaultPath = "/temp/access.log"

let statistics = [
    {   Name = "most_section_hit"
        Computation = RankingComputation()
        Refresh = Rate 10000 }]

[<EntryPoint>]
let main _ =
    let repository = StatisticsRepository()

    let statisticsAgent = StatisticsAgent(statistics)

    let sub =
        statisticsAgent.AsObservable
        |> Observable.subscribe repository.Update

    let displayRefresh =
        let rec loop () = async {
            do! Async.Sleep 1000
            Console.Clear()
            statistics
            |> Seq.map (fun c -> c.Name)
            |> Seq.choose (fun c -> repository.Get c)
            |> Seq.iter (fun c -> Console.print c.Result)

            return! loop() }
        loop()

    File.readContinuously defaultPath
    |> AsyncSeq.map LogParser.parse
    |> AsyncSeq.choose (function | Success(x, _, _) -> Some x | _ -> None)
    |> AsyncSeq.iter statisticsAgent.Receive
    |> Async.Start

    displayRefresh |> Async.Start

    Console.ReadLine() |> ignore
    sub.Dispose()
    0 // return an integer exit code
