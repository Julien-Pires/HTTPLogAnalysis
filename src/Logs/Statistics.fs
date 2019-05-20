namespace Logs

type UpdatePolicy =
    | Tick of int

type StatisticComputation = {
    Name : string
    Computation : Request list -> Statistic list
    RequestsFilter : RequestCache -> Request list
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
            let stats = [
                for (timer, computation) in computations do
                    timer.Update()
                    if timer.IsCompleted then
                        timer.Reset()
                        let requests = computation.RequestsFilter cache
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