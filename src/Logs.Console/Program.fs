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

let displayConf = [
    ("most_section_hit", {
        Title = Some "Section with most hit (last 10 sec)"
        Headers = ["Section"; "Total hit"]
        Columns = [(fun c -> c.Name); (fun c -> c.Value)]
        ColumnWidth = 20 }) ] |> Map.ofList

[<EntryPoint>]
let main _ =
    let console = ConsoleFormat()
    let repository = StatisticsRepository()
    let statisticsAgent = StatisticsAgent(statistics)

    let sub =
        statisticsAgent.AsObservable
        |> Observable.subscribe repository.Update

    let displayRefresh =
        let rec loop () = async {
            do! Async.Sleep 1000
            statistics
            |> Seq.map (fun c -> c.Name)
            |> Seq.choose (fun c -> repository.Get c)
            |> Seq.iter (fun c ->
                console.WriteTable c.Result displayConf.[c.Name])
            console.Output()
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
