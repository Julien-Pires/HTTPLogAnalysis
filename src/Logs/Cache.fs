namespace Logs

open System

type CacheAction =
    | Add of Request
    | Get of AsyncReplyChannel<Request list>

type CacheContent = Request list

type CacheStatus = {
    Requests : CacheContent
    LastClear : int64 }

type RequestCache() =
    let agent = Agent.Start(fun inbox -> 
        let rec loop state = async {
            let! msg = inbox.Receive()
            match msg with
            | Add x -> return! loop { state with Requests = x::state.Requests }
            | Get x ->
                x.Reply state.Requests
                return! loop state }
        loop { Requests = []; LastClear = DateTime.Now.Ticks })

    member __.Add request = agent.Post (Add request)
    member __.Get = agent.PostAndReply Get

module RequestCache = 
    let getRequests timeSpan (cache : CacheContent) =
        let datetime = DateTime.Now.AddSeconds(-timeSpan)
        cache |> Seq.filter(fun c -> c.Date >= datetime)
