namespace HTTPAnalysis.Monitoring

open System

type CacheAction =
    | Add of Request
    | Get of AsyncReplyChannel<Request list>
    | Clear

type CacheStatus = {
    Requests : Request list 
    LastClear : int64 }

type RequestCache() =
    let agent = Agent.Start(fun inbox -> 
        let rec loop state = async {
            let! msg = inbox.Receive()
            match msg with
            | Add x -> return! loop { state with Requests = x::state.Requests }
            | Get x ->
                x.Reply state.Requests
                return! loop state
            | Clear -> return! loop state }
        loop { Requests = []; LastClear = DateTime.Now.Ticks })

    let Add request = agent.Post (Add request)
    let Get = agent.PostAndReply Get