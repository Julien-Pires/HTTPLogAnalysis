namespace Logs

open System

type CacheAction =
    | Add of Request
    | Get of AsyncReplyChannel<Request list>

type RequestCache() =
    let source = ObservableSource<Request>()

    let agent = Agent.Start(fun inbox -> 
        let rec loop state = async {
            let! msg = inbox.Receive()
            match msg with
            | Add x ->
                source.OnNext x
                return! loop (x::state)
            | Get x ->
                x.Reply state
                return! loop state }
        loop [])

    member __.Add request = agent.Post (Add request)

    member __.Get = agent.PostAndReply Get

    member __.AsObservable = source.AsObservable

module RequestCache =
    let getRequestsByFrame timeSpan cache =
        let datetime = DateTime.Now.AddSeconds(-timeSpan)
        cache |> Seq.filter(fun c -> c.Date >= datetime)
