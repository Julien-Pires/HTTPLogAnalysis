namespace Logs

type UpdatePolicy =
    | Tick of int

type StatisticComputation = {
    Name : string
    Computation : Request seq -> Statistic list
    RequestsFilter : RequestCache -> Request seq
    Update : UpdatePolicy }

type StatisticsAgent(cache : RequestCache, computations : StatisticComputation list) =
    let source = ObservableSource<StatisticResult list>()
    let computations =
        computations
        |> List.map (fun c ->
            match c.Update with
            | Tick x -> (Timer(x), c))

    let computeStatistics =
        let rec loop () = async {
            do! Async.Sleep 1000
            let statistics = [
                for (timer, computation) in computations do
                    timer.Update()
                    if timer.IsCompleted then
                        timer.Reset()
                        let requests = computation.RequestsFilter cache
                        yield {
                            Name = computation.Name
                            Result = computation.Computation requests }]
            source.OnNext statistics
            return! loop() }
        loop()

    do
        computeStatistics |> Async.Start

    member __.AsObservable =
        source.AsObservable

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