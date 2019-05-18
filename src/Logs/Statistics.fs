namespace Logs

type StatisticComputation = {
    Name : string
    Computation : IComputation }

type StatisticResult = {
    Name : string
    Result : ComputationResults }

type StatisticsAgent(computations : StatisticComputation list) =
    let source = ObservableSource<StatisticResult list>()
    let cache = RequestCache()
    let computations = computations

    let rec timer =
        let rec loop () = async {
            do! Async.Sleep 1000
            let cache = cache.Get
            let stats = [
                for i in computations do
                    yield {
                        Name = i.Name
                        Result = i.Computation.Compute(cache) }]
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
    let agent = Agent.Start(fun inbox -> 
        let rec loop (data : StatisticResult list) = async {
            let! msg = inbox.Receive()
            match msg with
            | Get (name, reply) ->
                data |> List.tryFind (fun c -> c.Name = name) |> reply.Reply
                return! loop data
            | Update x ->
                return! loop x
            return! loop data }
        loop [])

    member __.Get name = agent.PostAndReply (fun c -> Get(name, c))

    member __.Update results = agent.Post <| Update results