open System
open FSharp.Control
open Logs
open Logs.Console

let requestsCache = RequestCache(60.0)
let statisticsAgent = StatisticsAgent(requestsCache, Configuration.statistics, 1000)
let alertsMonitoring = AlertMonitoring(Configuration.alerts)
let statsRepository = KeyedRepository<StatisticResult>()
let alertsRepository = Repository<AlertResponse>()

[<EntryPoint>]
let main args =
    let argsMap = ArgumentsParser.parse args
    let path =
        match argsMap.TryFind 'f' with 
        | Some x -> x 
        | None -> Configuration.defaultPath

    statisticsAgent.AsObservable
    |> Observable.subscribe (fun c ->
        c
        |> Seq.map (fun c -> (c.Name, c))
        |> Seq.iter statsRepository.Add) |> ignore

    statisticsAgent.AsObservable
    |> Observable.subscribe (
        alertsMonitoring.Update 
        >> (List.iter alertsRepository.Add)) |> ignore

    async {
        let! lines = File.readContinuously path
        lines
        |> AsyncSeq.choose LogParser.parse
        |> AsyncSeq.iter requestsCache.Add
        |> Async.Start
        return () } |> Async.Start

    async {
        while true do
            do! Async.Sleep 1000
            let stats =
                Configuration.statistics
                |> Seq.choose (fun c -> statsRepository.Get c.Name)
            Output.display Configuration.display stats alertsRepository.Get
        return () } |> Async.Start

    Console.ReadLine() |> ignore
    0 // return an integer exit code
