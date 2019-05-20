open System
open FSharp.Control
open FParsec
open Logs
open Logs.Console

[<EntryPoint>]
let main _ =
    let requestsCache = RequestCache()
    let statisticsAgent = StatisticsAgent(requestsCache, Configuration.statistics)
    let alertsMonitoring = AlertMonitoring(Configuration.alerts)
    let statsRepository = Repository<StatisticResult>()
    let mutable alertsRepository = []

    statisticsAgent.AsObservable
    |> Observable.subscribe (fun c -> 
        c
        |> Seq.map (fun c -> (c.Name, c))
        |> statsRepository.Add) |> ignore

    statisticsAgent.AsObservable
    |> Observable.subscribe (fun c -> 
        alertsMonitoring.Update c
        |> Seq.iter (fun c -> alertsRepository <- c::alertsRepository)) |> ignore

    let displayRefresh =
        let rec loop () = async {
            do! Async.Sleep 1000
            let stats =
                Configuration.statistics
                |> Seq.choose (fun c -> statsRepository.Get c.Name)
            Output.display Configuration.display stats alertsRepository
            return! loop() }
        loop()

    File.readContinuously Configuration.defaultPath
    |> AsyncSeq.map LogParser.parse
    |> AsyncSeq.choose (function | Success(x, _, _) -> Some x | _ -> None)
    |> AsyncSeq.iter requestsCache.Add
    |> Async.Start

    displayRefresh 
    |> Async.Start

    Console.ReadLine() |> ignore
    0 // return an integer exit code
