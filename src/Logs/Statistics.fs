namespace Logs

type UpdatePolicy =
    | Tick of int

type StatisticComputation = {
    Name : string
    Computation : Request seq -> Statistic list
    RequestsFilter : Request List -> Request seq
    Update : UpdatePolicy }

type StatisticsAgent(cache : RequestCache, computations : StatisticComputation list) =
    let source = ObservableSource<StatisticResult list>()
    let computations =
        computations
        |> List.map (fun c ->
            match c.Update with
            | Tick x -> (Timer(x), c))

    let timer =
        let rec loop () = async {
            do! Async.Sleep 1000
            let requests = cache.Get
            let stats = [
                for (timer, computation) in computations do
                    timer.Update()
                    if timer.IsCompleted then
                        timer.Reset()
                        let requests = requests |> computation.RequestsFilter
                        yield {
                            Name = computation.Name
                            Result = computation.Computation requests }]
            source.OnNext stats
            return! loop() }
        loop()

    do
        timer |> Async.Start

    member __.AsObservable =
        source.AsObservable