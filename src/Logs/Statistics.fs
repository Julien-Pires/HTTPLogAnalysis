namespace Logs

open FSharpx.Control

type UpdatePolicy =
    | Tick of int

type StatisticComputation = {
    Name : string
    Computation : Request seq -> Statistic list
    RequestsFilter : RequestCache -> Request seq
    Update : UpdatePolicy }

type StatisticsAgent(cache : RequestCache, computations : StatisticComputation list, refreshRate) =
    let source = ObservableSource<StatisticResult list>()
    let computations =
        computations
        |> List.map (fun c ->
            match c.Update with
            | Tick x -> (Counter(x), c))

    let refreshStatistics =
        let rec loop () = async {
            do! Async.Sleep refreshRate

            let statistics =
                computations
                |> List.choose (fun (timer, computation) ->
                    timer.Update()
                    match timer.IsCompleted with
                    | true ->
                        let { RequestsFilter = filter; Computation = compute } = computation
                        {   Name = computation.Name
                            Result = compute <| filter cache } |> Some
                    | false -> None)
            computations
            |> Seq.filter (fun (timer, _) -> timer.IsCompleted)
            |> Seq.iter (fun (timer, _) -> timer.Reset())

            source.OnNext statistics

            return! loop() }
        loop()

    do
        refreshStatistics |> Async.Start

    member __.AsObservable with get() = source.AsObservable

module Statistics =
    let rank key requests =
        requests
        |> Seq.groupBy key
        |> Seq.map (fun (key, requests) -> {
            Values = Map.ofList <| [
            ("Name", key)
            ("Count", (Seq.length requests) :> obj)] })
        |> Seq.sortByDescending (fun c -> c.Values.["Count"] :?> int)
        |> Seq.toList

    let count requests = [{
        Values = Map.ofList <| [("Count", (Seq.length requests) :> obj)] }]