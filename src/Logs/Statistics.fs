namespace Logs

type ComputationRefreshPolicy =
    | Rate of int

type StatisticComputation = {
    Name : string
    Computation : IComputation 
    Refresh : ComputationRefreshPolicy }

type StatisticResult = {
    Name : string
    Result : ComputationItem list }

type StatisticsAgent(computations : StatisticComputation list) =
    let source = ObservableSource<StatisticResult list>()
    let cache = RequestCache()
    let computations =
        computations
        |> List.map (fun c -> 
            match c.Refresh with
            | Rate x -> (Counter(x), c))

    let timer =
        let rec loop () = async {
            do! Async.Sleep 1000
            let cache = cache.Get
            let stats = [
                for (refresh, computation) in computations do
                    refresh.Reduce 1000
                    if refresh.Value <= 0 then
                        refresh.Reset()
                        yield {
                            Name = computation.Name
                            Result = computation.Computation.Compute(cache) }]
            source.OnNext stats
            return! loop() }
        loop()

    do
        timer |> Async.Start

    member __.Receive request =
        cache.Add request

    member __.AsObservable =
        source.AsObservable

type RepositoryOperation =
    | Get of (string * AsyncReplyChannel<StatisticResult option>)
    | Update of StatisticResult list

type StatisticsRepository() =
    let data = ref (Map.empty : Map<string, StatisticResult>)

    member __.Get name =
        !data |> Map.tryFind name

    member __.Update results = 
        data := 
            results 
            |> List.fold (fun acc c -> acc |> Map.add c.Name c) !data