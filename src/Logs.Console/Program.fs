open System
open FSharp.Control
open FParsec
open Logs
open Logs.Console

let defaultPath = "/temp/access.log"

let statistics = [
    {   Name = "most_section_hit"
        Computation = RankingComputation(fun c -> c.Sections.Head :> obj)
        RequestsFilter = RequestCache.getRequestsByNow 10.0
        Update = Tick 10000 }
    {   Name = "requests_per_second"
        Computation = CountComputation()
        RequestsFilter = (fun c -> RequestCache.getRequestsAt (DateTime.Now.AddSeconds(-1.0)) c)
        Update = Tick 1000 }]

let outputConfiguration = Map.ofList [
    ("most_section_hit",  Table {
        Title = Some "Section with most hit (last 10 sec)"
        Headers = ["Section"; "Total hit"]
        Columns = [(fun (c : Statistic) -> string c.Values.["Name"]); (fun c -> string c.Values.["Count"])]
        ColumnWidth = 20 })
    ("requests_per_second", Table {
        Title = Some "Number of requests (last 1 sec)"
        Headers = ["Total"]
        Columns = [(fun c -> string c.Values.["Count"])]
        ColumnWidth = 20 })]

[<EntryPoint>]
let main _ =
    let requestsCache = RequestCache()
    let statisticsAgent = StatisticsAgent(requestsCache, statistics)
    let repository = StatisticsRepository()

    statisticsAgent.AsObservable
    |> Observable.subscribe repository.Update |> ignore

    let displayRefresh =
        let rec loop () = async {
            do! Async.Sleep 1000
            let stats = 
                statistics
                |> Seq.choose (fun c -> repository.Get c.Name)
            Output.display outputConfiguration stats
            return! loop() }
        loop()

    File.readContinuously defaultPath
    |> AsyncSeq.map LogParser.parse
    |> AsyncSeq.choose (function | Success(x, _, _) -> Some x | _ -> None)
    |> AsyncSeq.iter requestsCache.Add
    |> Async.Start

    displayRefresh 
    |> Async.Start

    Console.ReadLine() |> ignore
    0 // return an integer exit code
