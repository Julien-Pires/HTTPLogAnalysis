namespace Logs

open FSharp.Control

type StatisticRecord<'a> = {
    Name : string 
    Value : 'a }

type Statistics<'a> =
    | Single of StatisticRecord<'a>
    | Multiple of StatisticRecord<'a> list

[<AbstractClass>]
type IStatisticsComputation() =
    abstract member Compute : CacheContent -> unit

type RankingStatistics() =
    inherit IStatisticsComputation()

    override __.Compute cache =
        let requests = 
            cache 
            |> RequestCache.getRequests 10.0
            |> Seq.groupBy (fun c -> c.Sections.Head)
            |> Seq.map (fun (section, requests) -> (section, requests, requests |> Seq.length))
            |> Seq.sortByDescending (fun (_, _, count) -> count)
            |> Seq.map (fun (section, _, count) -> { Name = section; Value = count })
            |> Seq.toList
            |> fun c -> printfn "%A" c
        ()
        
type StatisticsAgent(computations : IStatisticsComputation list) =
    let cache = RequestCache()
    let computations = computations

    let rec timer = 
        let rec loop () = async {
            do! Async.Sleep 1000
            let cache = cache.Get
            for i in computations do
                i.Compute(cache)
            return! loop() }
        loop()

    do
        timer |> Async.Start

    member __.Receive request =
        cache.Add request