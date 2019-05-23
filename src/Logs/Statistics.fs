namespace Logs

open FSharpx.Control

/// <summary>Represents the update policy used for computing a statistic</summary>
type UpdatePolicy =
    | Tick of int

/// <summary>Represents a statistic to compute</summary>
type StatisticComputation = {
    Name : string
    Computation : Request seq -> Statistic list
    RequestsFilter : RequestCache -> Request seq
    Update : UpdatePolicy }

/// <summary>
/// Represents an agent that computes a set of statistics.
/// Each statistic are computed according to their own update policy.
/// The refreshRate parameter allow to customize at which speed the agent check for computing statistics.
///</summary>
type StatisticsAgent(cache : RequestCache, computations : StatisticComputation list, refreshRate) =
    let source = ObservableSource<StatisticResult list>()
    let computations =
        computations
        |> List.map (fun c ->
            match c.Update with
            | Tick x -> (Counter(x), c))

    /// <summary>Represents an asynchronous task that computes statistics perdiodically</summary>
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

    /// <summary>Returns this instance as an observable object</summary>
    member __.AsObservable with get() = source.AsObservable

/// <summary>Contains methods to compute statistics</summary>
module Statistics =
    /// <summary>Allows to order requests by descending order for the specified key</summary>
    let rank key requests =
        requests
        |> Seq.countBy key
        |> Seq.sortByDescending (fun (_, count) -> count)
        |> Seq.map (fun (key, count) -> {
            Values = Map.ofList <| [
            ("Name", key)
            ("Count", count :> obj)] })
        |> Seq.toList

    /// <summary>Counts all requests</summary>
    let count requests = [{
        Values = Map([("Count", (Seq.length requests) :> obj)]) }]

    /// <summary>Counts all requests that matchs with the specified filter</summary>
    let countWith filter requests = [
        let count = requests |> Seq.filter filter |> Seq.length
        yield { Values = Map([("Count", count :> obj)]) }]