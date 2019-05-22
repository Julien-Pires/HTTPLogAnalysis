open System
open FSharp.Control
open Logs
open Logs.Console

let requestsCache = RequestCache(60.0)
let statisticsAgent = StatisticsAgent(requestsCache, Configuration.statistics)
let alertsMonitoring = AlertMonitoring(Configuration.alerts)
let statsRepository = Repository<StatisticResult>()
let mutable alertsRepository = []

[<EntryPoint>]
let main args =
    let argsMap = ArgumentsParser.parse args

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

    let path = match argsMap.TryFind 'f' with | Some x -> x | None -> Configuration.defaultPath
    File.readContinuously path
    |> AsyncSeq.choose LogParser.parse
    |> AsyncSeq.iter requestsCache.Add
    |> Async.Start

    displayRefresh 
    |> Async.Start

    Console.ReadLine() |> ignore
    0 // return an integer exit code
